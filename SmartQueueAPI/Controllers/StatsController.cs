using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Core.Data;
using SmartQueue.Core.DTOs.StatsDTOs;
using SmartQueue.Core.Models;

namespace SmartQueueAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Djelatnik")]
    public class StatsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatsController(AppDbContext context)
        {
            _context = context;
        }

        // ── GET /api/stats/overview 
        
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var allQueues = await _context.Queues
                .Include(q => q.Tickets)
                .Include(q => q.Counters)
                    .ThenInclude(c => c.AssignedUser)
                .ToListAsync();

            var todayTickets = allQueues
                .SelectMany(q => q.Tickets)
                .Where(t => t.CreatedAt >= today && t.CreatedAt < tomorrow)
                .ToList();

            var waitingNow = allQueues
                .SelectMany(q => q.Tickets)
                .Count(t => t.Status == TicketStatus.Waiting);

            var servedToday = todayTickets
                .Count(t => t.Status == TicketStatus.Done);

            var skippedToday = todayTickets
                .Count(t => t.Status == TicketStatus.Skipped);

            var avgWait = todayTickets
                .Where(t => t.ActualWaitMinutes.HasValue)
                .Select(t => (double)t.ActualWaitMinutes!.Value)
                .DefaultIfEmpty(0)
                .Average();

            var openCounters = allQueues
                .SelectMany(q => q.Counters)
                .Count(c => c.Status == CounterStatus.Open
                         || c.Status == CounterStatus.Busy);

            // Djelatnici count (users with Djelatnik role)
            var djelatniciCount = await _context.UserRoles
                .Join(_context.Roles,
                      ur => ur.RoleId,
                      r => r.Id,
                      (ur, r) => new { ur, r })
                .CountAsync(x => x.r.Name == "Djelatnik");

            var queueBreakdown = allQueues.Select(q =>
                BuildQueueSummary(q, today, tomorrow)).ToList();

            return Ok(new OverviewStatsDto
            {
                TotalQueues = allQueues.Count,
                ActiveQueues = allQueues.Count(q => q.Status == QueueStatus.Active),
                TotalTicketsToday = todayTickets.Count,
                TotalWaitingNow = waitingNow,
                TotalServedToday = servedToday,
                TotalSkippedToday = skippedToday,
                OverallAvgWaitToday = Math.Round(avgWait, 1),
                TotalOpenCounters = openCounters,
                TotalDjelatsnici = djelatniciCount,
                QueueBreakdown = queueBreakdown
            });
        }

        // ── GET /api/stats/queue/{id} 
        
        [HttpGet("queue/{id}")]
        public async Task<IActionResult> GetQueueStats(int id)
        {
            var queue = await _context.Queues
                .Include(q => q.Tickets)
                .Include(q => q.Counters)
                    .ThenInclude(c => c.AssignedUser)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (queue == null)
                return NotFound(new { message = $"Queue {id} not found." });

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            return Ok(BuildQueueSummary(queue, today, tomorrow));
        }

        // ── GET /api/stats/queue/{id}/peakhours 
        
        [HttpGet("queue/{id}/peakhours")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPeakHours(int id)
        {
            var queue = await _context.Queues
                .FirstOrDefaultAsync(q => q.Id == id);

            if (queue == null)
                return NotFound(new { message = $"Queue {id} not found." });

       
            var tickets = await _context.Tickets
                .Where(t => t.QueueId == id
                         && t.Status == TicketStatus.Done
                         && t.ActualWaitMinutes.HasValue)
                .ToListAsync();

     
            var byHour = tickets
                .GroupBy(t => t.CreatedAt.Hour)
                .Select(g => new PeakHourDto
                {
                    HourOfDay = g.Key,
                    HourLabel = $"{g.Key:D2}:00-{g.Key + 1:D2}:00",
                    TicketCount = g.Count(),
                    AvgWaitMinutes = Math.Round(g.Average(
                        t => (double)t.ActualWaitMinutes!.Value), 1)
                })
                .OrderBy(h => h.HourOfDay)
                .ToList();

         
            var dayNames = new[]
            {
                "Sunday","Monday","Tuesday","Wednesday",
                "Thursday","Friday","Saturday"
            };

            var byDay = tickets
                .GroupBy(t => (int)t.CreatedAt.DayOfWeek)
                .Select(g => new PeakDayDto
                {
                    DayOfWeek = g.Key,
                    DayName = dayNames[g.Key],
                    TicketCount = g.Count(),
                    AvgWaitMinutes = Math.Round(g.Average(
                        t => (double)t.ActualWaitMinutes!.Value), 1)
                })
                .OrderBy(d => d.DayOfWeek)
                .ToList();

            return Ok(new QueuePeakHoursDto
            {
                QueueId = queue.Id,
                QueueName = queue.Name,
                ByHour = byHour,
                ByDay = byDay,
                BusiestHour = byHour.OrderByDescending(h => h.TicketCount).FirstOrDefault(),
                BusiestDay = byDay.OrderByDescending(d => d.TicketCount).FirstOrDefault()
            });
        }

        // ── GET /api/stats/queue/{id}/counters ────────────────────────────────
       
        [HttpGet("queue/{id}/counters")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCounterStats(int id)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var counters = await _context.Counters
                .Include(c => c.AssignedUser)
                .Include(c => c.Tickets)
                .Where(c => c.QueueId == id)
                .ToListAsync();

            if (!counters.Any())
                return NotFound(new { message = $"No counters found for queue {id}." });

            var result = counters.Select(c =>
            {
                var todayTickets = c.Tickets
                    .Where(t => t.CreatedAt >= today
                             && t.CreatedAt < tomorrow
                             && t.Status == TicketStatus.Done)
                    .ToList();

                var avgService = todayTickets
                    .Where(t => t.CalledAt.HasValue && t.CompletedAt.HasValue)
                    .Select(t => (t.CompletedAt!.Value - t.CalledAt!.Value).TotalMinutes)
                    .DefaultIfEmpty(0)
                    .Average();

                return new CounterStatsDto
                {
                    CounterId = c.Id,
                    CounterName = c.Name,
                    Status = c.Status.ToString(),
                    AssignedUserName = c.AssignedUser != null
                        ? $"{c.AssignedUser.FirstName} {c.AssignedUser.LastName}"
                        : null,
                    TicketsServedToday = todayTickets.Count,
                    AvgServiceMinutesToday = Math.Round(avgService, 1)
                };
            }).ToList();

            return Ok(result);
        }

        // ── GET /api/stats/queue/{id}/trend ───────────────────────────────────
       
        [HttpGet("queue/{id}/trend")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTrend(int id, [FromQuery] int days = 30)
        {
            var from = DateTime.UtcNow.Date.AddDays(-days);

            var tickets = await _context.Tickets
                .Where(t => t.QueueId == id && t.CreatedAt >= from)
                .ToListAsync();

            var trend = tickets
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("dd/MM/yyyy"),
                    TotalTickets = g.Count(),
                    Completed = g.Count(t => t.Status == TicketStatus.Done),
                    Skipped = g.Count(t => t.Status == TicketStatus.Skipped),
                    AvgWaitMin = Math.Round(g
                        .Where(t => t.ActualWaitMinutes.HasValue)
                        .Select(t => (double)t.ActualWaitMinutes!.Value)
                        .DefaultIfEmpty(0)
                        .Average(), 1)
                })
                .OrderBy(d => d.Date)
                .ToList();

            return Ok(new
            {
                QueueId = id,
                Days = days,
                From = from.ToString("dd/MM/yyyy"),
                To = DateTime.UtcNow.Date.ToString("dd/MM/yyyy"),
                DailyData = trend
            });
        }

        // ── PRIVATE HELPER 
        private static QueueSummaryStatsDto BuildQueueSummary(
            Queue queue, DateTime today, DateTime tomorrow)
        {
            var todayTickets = queue.Tickets
                .Where(t => t.CreatedAt >= today && t.CreatedAt < tomorrow)
                .ToList();

            var completedToday = todayTickets
                .Where(t => t.Status == TicketStatus.Done)
                .ToList();

            var skippedToday = todayTickets
                .Count(t => t.Status == TicketStatus.Skipped);

            var avgWait = completedToday
                .Where(t => t.ActualWaitMinutes.HasValue)
                .Select(t => (double)t.ActualWaitMinutes!.Value)
                .DefaultIfEmpty(0)
                .Average();

            var avgService = completedToday
                .Where(t => t.CalledAt.HasValue && t.CompletedAt.HasValue)
                .Select(t => (t.CompletedAt!.Value - t.CalledAt!.Value).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average();

            var skipRate = todayTickets.Count > 0
                ? Math.Round((double)skippedToday / todayTickets.Count * 100, 1)
                : 0;

            return new QueueSummaryStatsDto
            {
                QueueId = queue.Id,
                QueueName = queue.Name,
                Status = queue.Status.ToString(),
                TotalTicketsToday = todayTickets.Count,
                TotalTicketsThisWeek = queue.Tickets.Count(t =>
                    t.CreatedAt >= today.AddDays(-(int)today.DayOfWeek)),
                TotalTicketsThisMonth = queue.Tickets.Count(t =>
                    t.CreatedAt.Month == today.Month
                    && t.CreatedAt.Year == today.Year),
                CurrentlyWaiting = queue.Tickets
                    .Count(t => t.Status == TicketStatus.Waiting),
                CurrentlyServing = queue.Tickets
                    .Count(t => t.Status == TicketStatus.Called
                             || t.Status == TicketStatus.InService),
                CompletedToday = completedToday.Count,
                SkippedToday = skippedToday,
                AvgWaitMinutesToday = Math.Round(avgWait, 1),
                AvgServiceMinutesToday = Math.Round(avgService, 1),
                SkipRateToday = skipRate,
                OpenCounters = queue.Counters.Count(c =>
                    c.Status == CounterStatus.Open
                    || c.Status == CounterStatus.Busy),
                TotalCounters = queue.Counters.Count
            };
        }
    }
}

/*
Why [Authorize(Roles = "Admin,Djelatnik")] at controller level:
 * All stats endpoints require at least Djelatnik — no public access to operational data.
 * Individual endpoints that are Admin-only add an extra [Authorize(Roles = "Admin")] on top.

Why BuildQueueSummary is a private static helper: 
* Both GetOverview and GetQueueStats need the same queue summary calculation.
* Extracting it to a private method avoids duplicating ~40 lines of logic.


Why DefaultIfEmpty(0) on averages: If there are no completed tickets yet,
 * .Average() on an empty collection throws an exception.
 * DefaultIfEmpty(0) returns 0 instead — safe for fresh queues with no data.
  
 
Why GetTrend accepts days parameter: Admins might want last 7 days, last 30 days or last 90 days.
 * Making it a query parameter with default 30 gives flexibility without needing multiple endpoints.
 
Why calculate AvgServiceMinutesToday separately from AvgWaitMinutesToday:
 * Wait time = CreatedAt → CalledAt (how long customer waited). 
 * Service time = CalledAt → CompletedAt (how long the actual service took).
 * Both are important but measure different things — wait time affects customer experience, 
 * service time affects staff efficiency.
*/