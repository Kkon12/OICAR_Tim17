using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Core.Data;
using SmartQueue.Core.DTOs.QueueDTOs;
using SmartQueue.Core.Interfaces;
using SmartQueue.Core.Models;

namespace SmartQueueAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueueController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEstimationService _estimationService;

        public QueueController(
            AppDbContext context,
            IEstimationService estimationService)
        {
            _context = context;
            _estimationService = estimationService;
        }

        // ── GET /api/queue ────────────────────────────────────────────────────
        // Public — list all active queues
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var queues = await _context.Queues
                .Include(q => q.Tickets)
                .Include(q => q.Counters)
                .ToListAsync();

            var response = queues.Select(q => new QueueResponseDto
            {
                Id = q.Id,
                Name = q.Name,
                Description = q.Description,
                Status = q.Status.ToString(),
                DefaultServiceMinutes = q.DefaultServiceMinutes,
                TotalWaiting = q.Tickets.Count(t => t.Status == TicketStatus.Waiting),
                OpenCounters = q.Counters.Count(c => c.Status == CounterStatus.Open
                                                  || c.Status == CounterStatus.Busy),
                CreatedAt = q.CreatedAt
            });

            return Ok(response);
        }

        // ── GET /api/queue/{id} ───────────────────────────────────────────────
        // Public — get single queue details
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var queue = await _context.Queues
                .Include(q => q.Tickets)
                .Include(q => q.Counters)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (queue == null)
                return NotFound(new { message = $"Queue {id} not found." });

            return Ok(new QueueResponseDto
            {
                Id = queue.Id,
                Name = queue.Name,
                Description = queue.Description,
                Status = queue.Status.ToString(),
                DefaultServiceMinutes = queue.DefaultServiceMinutes,
                TotalWaiting = queue.Tickets.Count(t => t.Status == TicketStatus.Waiting),
                OpenCounters = queue.Counters.Count(c => c.Status == CounterStatus.Open
                                                      || c.Status == CounterStatus.Busy),
                CreatedAt = queue.CreatedAt
            });
        }

        // ── GET /api/queue/{id}/estimate ──────────────────────────────────────
        // Public — get full live queue status with estimates
        [HttpGet("{id}/estimate")]
        public async Task<IActionResult> GetEstimate(int id)
        {
            var exists = await _context.Queues.AnyAsync(q => q.Id == id);
            if (!exists)
                return NotFound(new { message = $"Queue {id} not found." });

            var status = await _estimationService.GetQueueStatusAsync(id);
            return Ok(status);
        }

        // ── POST /api/queue ───────────────────────────────────────────────────
        // Admin only — create new queue
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateQueueDto dto)
        {
            var queue = new Queue
            {
                Name = dto.Name,
                Description = dto.Description,
                DefaultServiceMinutes = dto.DefaultServiceMinutes,
                Status = QueueStatus.Active
            };

            _context.Queues.Add(queue);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = queue.Id },
                new QueueResponseDto
                {
                    Id = queue.Id,
                    Name = queue.Name,
                    Description = queue.Description,
                    Status = queue.Status.ToString(),
                    DefaultServiceMinutes = queue.DefaultServiceMinutes,
                    TotalWaiting = 0,
                    OpenCounters = 0,
                    CreatedAt = queue.CreatedAt
                });
        }

        // ── PUT /api/queue/{id} ───────────────────────────────────────────────
        // Admin only — update queue details
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateQueueDto dto)
        {
            var queue = await _context.Queues.FindAsync(id);
            if (queue == null)
                return NotFound(new { message = $"Queue {id} not found." });

            queue.Name = dto.Name;
            queue.Description = dto.Description;
            queue.DefaultServiceMinutes = dto.DefaultServiceMinutes;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Queue updated successfully." });
        }

        // ── PATCH /api/queue/{id}/status ──────────────────────────────────────
        // Admin only — change queue status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var queue = await _context.Queues.FindAsync(id);
            if (queue == null)
                return NotFound(new { message = $"Queue {id} not found." });

            if (!Enum.TryParse<QueueStatus>(status, true, out var newStatus))
                return BadRequest(new { message = "Invalid status. Use Active, Paused or Closed." });

            queue.Status = newStatus;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Queue status changed to {newStatus}." });
        }

        // ── DELETE /api/queue/{id} ────────────────────────────────────────────
        // Admin only — delete queue
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var queue = await _context.Queues.FindAsync(id);
            if (queue == null)
                return NotFound(new { message = $"Queue {id} not found." });

            _context.Queues.Remove(queue);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Queue deleted successfully." });
        }
    }
}

/*Why [Authorize(Roles = "Admin")] only on write operations: 
 * Reading queue info is public — kiosk tablets and mobile apps need to list queues without logging in.
 * Only creating, updating and deleting requires Admin role.
 --
Why CreatedAtAction on POST: Returns HTTP 201 Created with a Location header pointing to the new resource
— this is the correct REST standard for resource creation.
** --

Why PATCH for status instead of PUT: PUT replaces the entire resource.
-PATCH updates a single field — changing just the status without needing to send the full queue object.
Cleaner and more intentional.
--
Why Include(q => q.Tickets).Include(q => q.Counters):
Needed to calculate TotalWaiting and OpenCounters in the response. Without Include, EF Core won't load related data.*/