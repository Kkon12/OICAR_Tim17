using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Core.Data;
using SmartQueue.Core.DTOs.CounterDTOs;
using SmartQueue.Core.Models;
using System.Security.Claims;

namespace SmartQueueAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CounterController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CounterController(AppDbContext context)
        {
            _context = context;
        }

        // ── GET /api/counter/queue/{queueId} ──────────────────────────────────
        // Public — list all counters for a queue
        [HttpGet("queue/{queueId}")]
        public async Task<IActionResult> GetByQueue(int queueId)
        {
            var counters = await _context.Counters
                .Include(c => c.Queue)
                .Include(c => c.AssignedUser)
                .Include(c => c.Tickets)
                .Where(c => c.QueueId == queueId)
                .ToListAsync();

            var response = counters.Select(c => new CounterResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Status = c.Status.ToString(),
                QueueId = c.QueueId,
                QueueName = c.Queue.Name,
                AssignedUserId = c.AssignedUserId,
                AssignedUserName = c.AssignedUser != null
                    ? $"{c.AssignedUser.FirstName} {c.AssignedUser.LastName}"
                    : null,
                CurrentTicketNumber = c.Tickets
                    .Where(t => t.Status == TicketStatus.Called
                             || t.Status == TicketStatus.InService)
                    .OrderByDescending(t => t.CalledAt)
                    .FirstOrDefault()?.TicketNumber
            });

            return Ok(response);
        }

        // ── GET /api/counter/mine ─────────────────────────────────────────────
        // Djelatnik/Admin — returns the single counter assigned to the currently
        // authenticated user. Single DB query — replaces the N+1 loop that
        // previously called GetQueuesAsync() + GetCountersAsync() for every queue.
        // Returns 404 when no counter is assigned — MVC maps this to the
        // "No counter assigned" state, which is a valid and expected condition.
        [HttpGet("mine")]
        [Authorize(Roles = "Admin,Djelatnik")]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity not found in token." });

            var counter = await _context.Counters
                .Include(c => c.Queue)
                .Include(c => c.AssignedUser)
                .Include(c => c.Tickets)
                .FirstOrDefaultAsync(c => c.AssignedUserId == userId);

            if (counter == null)
                return NotFound(new { message = "No counter assigned to your account." });

            return Ok(new CounterResponseDto
            {
                Id = counter.Id,
                Name = counter.Name,
                Status = counter.Status.ToString(),
                QueueId = counter.QueueId,
                QueueName = counter.Queue.Name,
                AssignedUserId = counter.AssignedUserId,
                AssignedUserName = counter.AssignedUser != null
                    ? $"{counter.AssignedUser.FirstName} {counter.AssignedUser.LastName}"
                    : null,
                CurrentTicketNumber = counter.Tickets
                    .Where(t => t.Status == TicketStatus.Called
                             || t.Status == TicketStatus.InService)
                    .OrderByDescending(t => t.CalledAt)
                    .FirstOrDefault()?.TicketNumber
            });
        }

        // ── GET /api/counter/{id} ─────────────────────────────────────────────
        // Public — get single counter details
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var counter = await _context.Counters
                .Include(c => c.Queue)
                .Include(c => c.AssignedUser)
                .Include(c => c.Tickets)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (counter == null)
                return NotFound(new { message = $"Counter {id} not found." });

            return Ok(new CounterResponseDto
            {
                Id = counter.Id,
                Name = counter.Name,
                Status = counter.Status.ToString(),
                QueueId = counter.QueueId,
                QueueName = counter.Queue.Name,
                AssignedUserId = counter.AssignedUserId,
                AssignedUserName = counter.AssignedUser != null
                    ? $"{counter.AssignedUser.FirstName} {counter.AssignedUser.LastName}"
                    : null,
                CurrentTicketNumber = counter.Tickets
                    .Where(t => t.Status == TicketStatus.Called
                             || t.Status == TicketStatus.InService)
                    .OrderByDescending(t => t.CalledAt)
                    .FirstOrDefault()?.TicketNumber
            });
        }

        // ── POST /api/counter ─────────────────────────────────────────────────
        // Admin only — create new counter for a queue
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCounterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var queueExists = await _context.Queues.AnyAsync(q => q.Id == dto.QueueId);
            if (!queueExists)
                return NotFound(new { message = "Queue not found." });

            var counter = new Counter
            {
                Name = dto.Name,
                QueueId = dto.QueueId,
                AssignedUserId = dto.AssignedUserId,
                Status = CounterStatus.Closed
            };

            _context.Counters.Add(counter);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = counter.Id },
                new CounterResponseDto
                {
                    Id = counter.Id,
                    Name = counter.Name,
                    Status = counter.Status.ToString(),
                    QueueId = counter.QueueId,
                    AssignedUserId = counter.AssignedUserId
                });
        }

        // ── PUT /api/counter/{id} ─────────────────────────────────────────────
        // Admin only — update counter name or assigned user
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCounterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var counter = await _context.Counters.FindAsync(id);
            if (counter == null)
                return NotFound(new { message = $"Counter {id} not found." });

            counter.Name = dto.Name;
            counter.AssignedUserId = dto.AssignedUserId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Counter updated successfully." });
        }

        // ── PATCH /api/counter/{id}/open ──────────────────────────────────────
        // Djelatnik/Admin — open counter (start of shift)
        [HttpPatch("{id}/open")]
        [Authorize(Roles = "Admin,Djelatnik")]
        public async Task<IActionResult> OpenCounter(int id)
        {
            var counter = await _context.Counters.FindAsync(id);
            if (counter == null)
                return NotFound(new { message = $"Counter {id} not found." });

            if (counter.Status == CounterStatus.Open)
                return BadRequest(new { message = "Counter is already open." });

            counter.Status = CounterStatus.Open;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{counter.Name} is now Open." });
        }

        // ── PATCH /api/counter/{id}/close ─────────────────────────────────────
        // Djelatnik/Admin — close counter (end of shift)
        [HttpPatch("{id}/close")]
        [Authorize(Roles = "Admin,Djelatnik")]
        public async Task<IActionResult> CloseCounter(int id)
        {
            var counter = await _context.Counters.FindAsync(id);
            if (counter == null)
                return NotFound(new { message = $"Counter {id} not found." });

            // Check no ticket is currently being served
            var hasActiveTicket = await _context.Tickets
                .AnyAsync(t => t.CounterId == id
                            && (t.Status == TicketStatus.Called
                             || t.Status == TicketStatus.InService));

            if (hasActiveTicket)
                return BadRequest(new { message = "Cannot close counter — ticket is currently being served." });

            counter.Status = CounterStatus.Closed;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{counter.Name} is now Closed." });
        }

        // ── PATCH /api/counter/{id}/assign ────────────────────────────────────
        // Admin only — assign a Djelatnik to a counter
        [HttpPatch("{id}/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignUser(int id, [FromBody] AssignUserDto dto)
        {
            var counter = await _context.Counters.FindAsync(id);
            if (counter == null)
                return NotFound(new { message = $"Counter {id} not found." });

            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
                return NotFound(new { message = "User not found." });

            counter.AssignedUserId = dto.UserId;
            await _context.SaveChangesAsync();

            return Ok(new { message = "User assigned to counter successfully." });
        }

        // ── DELETE /api/counter/{id} ──────────────────────────────────────────
        // Admin only — delete a counter
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var counter = await _context.Counters.FindAsync(id);
            if (counter == null)
                return NotFound(new { message = $"Counter {id} not found." });

            var hasTickets = await _context.Tickets
                .AnyAsync(t => t.CounterId == id
                            && (t.Status == TicketStatus.Called
                             || t.Status == TicketStatus.InService));

            if (hasTickets)
                return BadRequest(new { message = "Cannot delete counter — ticket is currently being served." });

            _context.Counters.Remove(counter);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Counter deleted successfully." });
        }
    }
}

/*Why new counters start as Closed: A counter should never be active before a Djelatnik explicitly opens it
 * at the start of their shift.
 * This prevents estimates being calculated with phantom open counters.
Why check for active ticket before closing/deleting: Closing a counter mid-service
 * would leave a customer stranded at the counter with no resolution.
 * This guard prevents that data integrity issue.
Why Djelatnik can open/close but not create/delete:
 * Opening and closing is a daily shift operation — Djelatnik does it themselves.
 * Creating and deleting counters is infrastructure management — Admin only.
Why CurrentTicketNumber shows Called OR InService:
 * Both statuses mean a customer is physically at the counter.
 * The display board needs to show this number regardless of which sub-state the service is in.
Why GET /api/counter/mine reads userId from JWT claims:
 * The token already contains the user's GUID in the sub/NameIdentifier claim.
 * No userId parameter in the URL is needed — the token is the identity.
 * This also prevents one Djelatnik from querying another's counter by guessing an ID.*/