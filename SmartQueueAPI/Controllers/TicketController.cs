using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Core.Data;
using SmartQueue.Core.DTOs.TicketDTOs;
using SmartQueue.Core.Interfaces;
using SmartQueue.Core.Models;
using SmartQueueAPI.Hubs;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace SmartQueueAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEstimationService _estimationService;
        private readonly IHubContext<QueueHub> _hubContext;

        public TicketController(
            AppDbContext context,
            IEstimationService estimationService,
            IHubContext<QueueHub> hubContext)
        {
            _context = context;
            _estimationService = estimationService;
            _hubContext = hubContext;
        }

        /* Why inject IHubContext<QueueHub>: Controllers cannot directly call
         * Hub methods — IHubContext is the correct way for server-side code to push messages to connected clients.*/

        // ── POST /api/ticket/take ─────────────────────────────────────────────
        // Public — anonymous kiosk or registered user takes a ticket.
        // [EnableRateLimiting] activates the "kiosk" sliding window policy defined
        // in Program.cs — max 10 tickets per minute per IP. Returns 429 if exceeded.
        // ModelState check activates [Range] validation on TakeTicketDto.QueueId.
        [HttpPost("take")]
        [EnableRateLimiting("kiosk")]
        public async Task<IActionResult> TakeTicket([FromBody] TakeTicketDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check queue exists and is active
            var queue = await _context.Queues
                .FirstOrDefaultAsync(q => q.Id == dto.QueueId);

            if (queue == null)
                return NotFound(new { message = "Queue not found." });

            if (queue.Status != QueueStatus.Active)
                return BadRequest(new { message = $"Queue is {queue.Status}. Cannot take ticket." });

            // Calculate position — how many waiting tickets ahead
            var position = await _context.Tickets
                .CountAsync(t => t.QueueId == dto.QueueId
                              && t.Status == TicketStatus.Waiting) + 1;

            // Generate next ticket number for this queue
            // Each queue has its own ticket number sequence — Opća medicina has 001, 002...
            // and Blagajna independently has 001, 002... They don't share a global counter.
            var lastTicketNumber = await _context.Tickets
                .Where(t => t.QueueId == dto.QueueId)
                .MaxAsync(t => (int?)t.TicketNumber) ?? 0;

            // Calculate estimated wait using Tier 1/2 estimation engine
            var estimatedWait = await _estimationService
                .CalculateEstimatedWaitAsync(dto.QueueId, position);

            var ticket = new Ticket
            {
                TicketNumber = lastTicketNumber + 1,
                QueueId = dto.QueueId,
                UserId = dto.UserId,
                Status = TicketStatus.Waiting,
                Position = position,
                EstimatedWaitMinutes = estimatedWait,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Notify all clients in this queue group
            await QueueHub.NotifyQueueUpdated(_hubContext, _estimationService, dto.QueueId);

            return Ok(new TicketResponseDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Status = ticket.Status.ToString(),
                QueueId = ticket.QueueId,
                QueueName = queue.Name,
                Position = position,
                EstimatedWaitMinutes = estimatedWait,
                CreatedAt = ticket.CreatedAt
            });
        }

        // ── GET /api/ticket/{id} ──────────────────────────────────────────────
        // Public — get ticket status (customer checks their ticket)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Queue)
                .Include(t => t.Counter)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound(new { message = "Ticket not found." });

            // Recalculate current position
            var currentPosition = await _context.Tickets
                .CountAsync(t => t.QueueId == ticket.QueueId
                              && t.Status == TicketStatus.Waiting
                              && t.CreatedAt < ticket.CreatedAt) + 1;

            var estimatedWait = ticket.Status == TicketStatus.Waiting
                ? await _estimationService.RecalculateForPositionAsync(
                    ticket.QueueId, currentPosition)
                : ticket.ActualWaitMinutes;

            return Ok(new TicketResponseDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Status = ticket.Status.ToString(),
                QueueId = ticket.QueueId,
                QueueName = ticket.Queue.Name,
                Position = currentPosition,
                EstimatedWaitMinutes = estimatedWait,
                ActualWaitMinutes = ticket.ActualWaitMinutes,
                CounterId = ticket.CounterId,
                CounterName = ticket.Counter?.Name,
                CreatedAt = ticket.CreatedAt,
                CalledAt = ticket.CalledAt,
                CompletedAt = ticket.CompletedAt
            });
        }

        // ── GET /api/ticket/queue/{queueId} ───────────────────────────────────
        // Djelatnik/Admin — see all waiting tickets for a queue
        [HttpGet("queue/{queueId}")]
        [Authorize(Roles = "Admin,Djelatnik")]
        public async Task<IActionResult> GetByQueue(int queueId)
        {
            var tickets = await _context.Tickets
                .Include(t => t.Queue)
                .Include(t => t.Counter)
                .Where(t => t.QueueId == queueId
                         && t.Status == TicketStatus.Waiting)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            var response = tickets.Select((ticket, index) =>
                new TicketResponseDto
                {
                    Id = ticket.Id,
                    TicketNumber = ticket.TicketNumber,
                    Status = ticket.Status.ToString(),
                    QueueId = ticket.QueueId,
                    QueueName = ticket.Queue.Name,
                    Position = index + 1,
                    EstimatedWaitMinutes = ticket.EstimatedWaitMinutes,
                    CounterId = ticket.CounterId,
                    CounterName = ticket.Counter?.Name,
                    CreatedAt = ticket.CreatedAt
                });

            return Ok(response);
        }

        // ── PATCH /api/ticket/{id}/call ───────────────────────────────────────
        // Djelatnik — call a ticket to their counter
        [HttpPatch("{id}/call")]
        [Authorize(Roles = "Admin,Djelatnik")]
        public async Task<IActionResult> CallTicket(int id,
            [FromBody] UpdateTicketStatusDto dto)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Queue)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound(new { message = "Ticket not found." });

            if (ticket.Status != TicketStatus.Waiting)
                return BadRequest(new { message = $"Ticket is already {ticket.Status}." });

            ticket.Status = TicketStatus.Called;
            ticket.CalledAt = DateTime.UtcNow;
            ticket.CounterId = dto.CounterId;

            // ActualWaitMinutes = time from ticket creation to being called.
            // This is the ground truth wait time — how long the customer
            // actually stood/sat waiting. Stored for ML training data later.
            ticket.ActualWaitMinutes = (int)(ticket.CalledAt.Value
                - ticket.CreatedAt).TotalMinutes;

            await _context.SaveChangesAsync();

            // NOTE: UpdateStatSnapshotsAsync is intentionally NOT called here.
            // It was moved to CompleteTicket because snapshots store SERVICE TIME
            // (CompletedAt - CalledAt = time at the counter), not WAIT TIME
            // (CalledAt - CreatedAt = time in the queue). These are different.
            // The formula uses service time — storing wait time here was wrong.

            // Notify all clients in this queue group
            await QueueHub.NotifyQueueUpdated(_hubContext, _estimationService, ticket.QueueId);

            return Ok(new { message = $"Ticket {ticket.TicketNumber} called." });
        }

        // ── PATCH /api/ticket/{id}/complete ───────────────────────────────────
        // Djelatnik — mark ticket as done after serving
        [HttpPatch("{id}/complete")]
        [Authorize(Roles = "Admin,Djelatnik")]
        public async Task<IActionResult> CompleteTicket(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Queue)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound(new { message = "Ticket not found." });

            if (ticket.Status != TicketStatus.Called
             && ticket.Status != TicketStatus.InService)
                return BadRequest(new { message = "Ticket must be Called or InService to complete." });

            ticket.Status = TicketStatus.Done;
            ticket.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Update Tier 2 stat snapshots with TRUE service duration.
            // Service time = CompletedAt - CalledAt (time spent at the counter).
            // This is called here — not on CallTicket — because CompletedAt is
            // only available now. This feeds the time-aware snapshot averages
            // that make Monday 9am estimates different from Friday 3pm.
            if (ticket.CalledAt.HasValue)
            {
                var serviceMinutes = (ticket.CompletedAt.Value
                    - ticket.CalledAt.Value).TotalMinutes;
                await _estimationService.UpdateStatSnapshotsAsync(
                    ticket.QueueId, serviceMinutes);
            }

            // Notify all clients in this queue group
            await QueueHub.NotifyQueueUpdated(_hubContext, _estimationService, ticket.QueueId);

            return Ok(new { message = $"Ticket {ticket.TicketNumber} completed." });
        }

        // ── PATCH /api/ticket/{id}/skip ───────────────────────────────────────
        // Djelatnik — skip a ticket if customer is not present
        [HttpPatch("{id}/skip")]
        [Authorize(Roles = "Admin,Djelatnik")]
        public async Task<IActionResult> SkipTicket(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Queue)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound(new { message = "Ticket not found." });

            // Skipped tickets do NOT update stat snapshots — the customer was
            // not served so there is no service time to record. Including skips
            // would corrupt the average with zero-duration phantom services.
            ticket.Status = TicketStatus.Skipped;
            await _context.SaveChangesAsync();

            // Notify all clients in this queue group
            await QueueHub.NotifyQueueUpdated(_hubContext, _estimationService, ticket.QueueId);

            return Ok(new { message = $"Ticket {ticket.TicketNumber} skipped." });
        }

        // ── GET /api/ticket/history ───────────────────────────────────────────
        // Admin/Djelatnik — paginated ticket history with filters
        [HttpGet("history")]
        [Authorize(Roles = "Admin,Djelatnik")]
        public async Task<IActionResult> GetHistory([FromQuery] TicketHistoryQueryDto query)
        {
            var q = _context.Tickets
                .Include(t => t.Queue)
                .Include(t => t.Counter)
                .AsQueryable();

            // ── Apply filters ─────────────────────────────────────────────────
            // AsQueryable() with chained .Where(): builds the SQL query dynamically
            // — filters are added only if provided. EF Core translates this into a
            // single optimised SQL query. No data loaded until .ToListAsync().
            if (query.QueueId.HasValue)
                q = q.Where(t => t.QueueId == query.QueueId.Value);

            if (!string.IsNullOrWhiteSpace(query.Status) &&
                Enum.TryParse<TicketStatus>(query.Status, true, out var status))
                q = q.Where(t => t.Status == status);

            if (query.DateFrom.HasValue)
                q = q.Where(t => t.CreatedAt >= query.DateFrom.Value);

            if (query.DateTo.HasValue)
                q = q.Where(t => t.CreatedAt <= query.DateTo.Value);

            if (!string.IsNullOrWhiteSpace(query.UserId))
                q = q.Where(t => t.UserId == query.UserId);

            // ── Count before pagination ───────────────────────────────────────
            // CountAsync() before Skip/Take gives the true total for pagination
            // metadata ("Page 3 of 47"). Counting after would always return max 20.
            var totalCount = await q.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            // ── Apply pagination ──────────────────────────────────────────────
            var tickets = await q
                .OrderByDescending(t => t.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var response = tickets.Select(t => new TicketResponseDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Status = t.Status.ToString(),
                QueueId = t.QueueId,
                QueueName = t.Queue.Name,
                Position = t.Position,
                EstimatedWaitMinutes = t.EstimatedWaitMinutes,
                ActualWaitMinutes = t.ActualWaitMinutes,
                CounterId = t.CounterId,
                CounterName = t.Counter?.Name,
                CreatedAt = t.CreatedAt,
                CalledAt = t.CalledAt,
                CompletedAt = t.CompletedAt
            }).ToList();

            return Ok(new TicketHistoryResponseDto
            {
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = totalPages,
                Tickets = response
            });
        }

        // ── GET /api/ticket/my ────────────────────────────────────────────────
        // Authenticated user — see their own ticket history.
        // Uses JWT identity to scope query — no userId parameter needed.
        // Prevents users from querying each other's history.
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyTickets()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tickets = await _context.Tickets
                .Include(t => t.Queue)
                .Include(t => t.Counter)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(50) // personal history doesn't need full pagination
                .ToListAsync();

            var response = tickets.Select(t => new TicketResponseDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Status = t.Status.ToString(),
                QueueId = t.QueueId,
                QueueName = t.Queue.Name,
                Position = t.Position,
                EstimatedWaitMinutes = t.EstimatedWaitMinutes,
                ActualWaitMinutes = t.ActualWaitMinutes,
                CounterId = t.CounterId,
                CounterName = t.Counter?.Name,
                CreatedAt = t.CreatedAt,
                CalledAt = t.CalledAt,
                CompletedAt = t.CompletedAt
            }).ToList();

            return Ok(response);
        }
    }
}

/*Why TakeTicket is public: Kiosk tablets and mobile app users take tickets without logging in.
 * Authentication is optional — if UserId is provided it gets linked, otherwise it's anonymous.
Why we calculate lastTicketNumber per queue:
 -- Each queue has its own ticket number sequence — Opća medicina has 001, 002... and Blagajna
    independently has 001, 002... They don't share a global counter.
Why ActualWaitMinutes is set on CallTicket:
 -- The actual wait is from CreatedAt (when ticket was taken) to CalledAt (when Djelatnik calls them).
 -- This is the ground truth queue wait data — stored for ML training later.
Why UpdateStatSnapshotsAsync moved to CompleteTicket:
 -- Snapshots store SERVICE TIME (time at the counter = CompletedAt - CalledAt).
 -- Previously it was called on CallTicket with ActualWaitMinutes (queue wait time).
 -- These are two different things — confusing them made snapshot averages wrong.
 -- Service time is only knowable after completion, so CompleteTicket is the right place.
Why skipped tickets don't update snapshots:
 -- No service occurred. Including skips would corrupt averages with zero-duration entries.
Why separate call, complete and skip endpoints:
 -- Each is a distinct business action with different logic — calling sets CalledAt and CounterId,
    completing sets CompletedAt and updates Tier 2 snapshots, skipping just marks absent.
 -- Keeping them separate makes each action clear and auditable.*/