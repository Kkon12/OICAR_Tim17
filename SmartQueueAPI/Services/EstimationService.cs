using Microsoft.EntityFrameworkCore;
using SmartQueue.Core.Data;
using SmartQueue.Core.DTOs.QueueDTOs;
using SmartQueue.Core.Interfaces;
using SmartQueue.Core.Models;

namespace SmartQueueAPI.Services
{
    public class EstimationService : IEstimationService
    {
        private readonly AppDbContext _context;

        public EstimationService(AppDbContext context)
        {
            _context = context;
        }

        // ── Calculate estimated wait at ticket creation ────────────────────────
        public async Task<int> CalculateEstimatedWaitAsync(int queueId, int position)
        {
            var avgServiceMinutes = await GetAverageServiceTimeAsync(queueId);
            var openCounters = await GetOpenCountersCountAsync(queueId);

            if (openCounters == 0) openCounters = 1;

            var estimated = (avgServiceMinutes * position) / openCounters;
            return (int)Math.Ceiling(estimated);
        }

        // ── Recalculate for updated position ──────────────────────────────────
        public async Task<int> RecalculateForPositionAsync(int queueId, int newPosition)
        {
            return await CalculateEstimatedWaitAsync(queueId, newPosition);
        }

        // ── Update stat snapshots after ticket COMPLETED ───────────────────────
        // Called from CompleteTicket (not CallTicket) because we need the full
        // service duration: CompletedAt - CalledAt. CalledAt is only set on call,
        // CompletedAt is only set on completion — both are needed for the duration.
        // Previously this was called on CallTicket with ActualWaitMinutes (queue
        // wait time, not service time) — wrong value for the wrong purpose.
        public async Task UpdateStatSnapshotsAsync(int queueId, double actualServiceMinutes)
        {
            var now = DateTime.UtcNow;
            var dayOfWeek = now.DayOfWeek;
            var hour = now.Hour;

            var snapshot = await _context.QueueStatSnapshots
                .FirstOrDefaultAsync(s =>
                    s.QueueId == queueId &&
                    s.DayOfWeek == dayOfWeek &&
                    s.HourOfDay == hour);

            if (snapshot == null)
            {
                // First ticket ever completed in this time slot
                _context.QueueStatSnapshots.Add(new QueueStatSnapshot
                {
                    QueueId = queueId,
                    DayOfWeek = dayOfWeek,
                    HourOfDay = hour,
                    AvgServiceMinutes = actualServiceMinutes,
                    SampleCount = 1,
                    LastUpdated = now
                });
            }
            else
            {
                // Rolling average — mathematically equivalent to averaging all values
                // but requires storing only count + current average, not every record.
                var totalMinutes = snapshot.AvgServiceMinutes * snapshot.SampleCount
                                   + actualServiceMinutes;
                snapshot.SampleCount++;
                snapshot.AvgServiceMinutes = totalMinutes / snapshot.SampleCount;
                snapshot.LastUpdated = now;
            }

            await _context.SaveChangesAsync();
        }

        // ── Get full queue status for SignalR ─────────────────────────────────
        public async Task<QueueStatusDto> GetQueueStatusAsync(int queueId)
        {
            var queue = await _context.Queues
                .Include(q => q.Tickets)
                .Include(q => q.Counters)
                .FirstOrDefaultAsync(q => q.Id == queueId);

            if (queue == null) return new QueueStatusDto();

            var waitingTickets = queue.Tickets
                .Where(t => t.Status == TicketStatus.Waiting)
                .OrderBy(t => t.CreatedAt)
                .ToList();

            var openCounters = queue.Counters
                .Count(c => c.Status == CounterStatus.Open
                          || c.Status == CounterStatus.Busy);

            var currentlyServing = queue.Tickets
                .Where(t => t.Status == TicketStatus.Called
                          || t.Status == TicketStatus.InService)
                .OrderByDescending(t => t.CalledAt)
                .FirstOrDefault()?.TicketNumber ?? 0;

            var avgService = await GetAverageServiceTimeAsync(queueId);
            if (openCounters == 0) openCounters = 1;

            var ticketPositions = waitingTickets
                .Select((ticket, index) => new TicketPositionDto
                {
                    TicketNumber = ticket.TicketNumber,
                    Position = index + 1,
                    EstimatedWaitMinutes = (int)Math.Ceiling(
                        (avgService * (index + 1)) / openCounters)
                })
                .ToList();

            return new QueueStatusDto
            {
                QueueId = queueId,
                QueueName = queue.Name,
                CurrentlyServingNumber = currentlyServing,
                TotalWaiting = waitingTickets.Count,
                OpenCounters = openCounters,
                AverageServiceMinutes = avgService,
                WaitingTickets = ticketPositions
            };
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private async Task<double> GetAverageServiceTimeAsync(int queueId)
        {
            var queue = await _context.Queues
                .FirstOrDefaultAsync(q => q.Id == queueId);

            if (queue == null) return 5.0;

            // Last 20 completed tickets — recent data is more representative
            // than old data. A slow morning should not distort afternoon estimates.
            var completedTickets = await _context.Tickets
                .Where(t => t.QueueId == queueId
                         && t.Status == TicketStatus.Done
                         && t.CalledAt != null
                         && t.CompletedAt != null)
                .OrderByDescending(t => t.CompletedAt)
                .Take(20)
                .ToListAsync();

            var count = completedTickets.Count;

            // ── Tier 1: Bayesian blend of real data + admin default ────────────
            // Day 1 (0 tickets): use admin default entirely.
            // Tickets 1-19: smoothly blend from default toward real data.
            // Ticket 20+: fully trust real data, ignore default.
            // This prevents a jarring jump at exactly ticket 20.
            double liveEstimate;
            if (count == 0)
            {
                liveEstimate = queue.DefaultServiceMinutes;
            }
            else
            {
                var realAvg = completedTickets
                    .Average(t => (t.CompletedAt!.Value - t.CalledAt!.Value).TotalMinutes);

                if (count >= queue.MinTicketsForStats)
                {
                    liveEstimate = realAvg;
                }
                else
                {
                    var weight = (double)count / queue.MinTicketsForStats;
                    liveEstimate = (queue.DefaultServiceMinutes * (1 - weight))
                                 + (realAvg * weight);
                }
            }

            // ── Tier 2: Time-aware snapshot lookup ────────────────────────────
            // Once enough live data exists (>= MinTicketsForStats) we trust it
            // fully and return immediately — no need to check snapshots.
            // When live data is sparse, check if there is a snapshot for the
            // current hour + day with enough samples (>= 10) to be meaningful.
            // Snapshots capture the fact that Monday 9am is different from
            // Friday 3pm — same queue, very different service patterns.
            if (count >= queue.MinTicketsForStats)
                return liveEstimate;

            var now = DateTime.UtcNow;
            var snapshot = await _context.QueueStatSnapshots
                .FirstOrDefaultAsync(s =>
                    s.QueueId == queueId &&
                    s.DayOfWeek == now.DayOfWeek &&
                    s.HourOfDay == now.Hour);

            if (snapshot != null && snapshot.SampleCount >= 10)
            {
                // Blend snapshot with whatever live data we have.
                // As live data grows toward MinTicketsForStats, liveWeight
                // increases and the snapshot's influence fades out.
                var liveWeight = (double)count / queue.MinTicketsForStats;
                return (snapshot.AvgServiceMinutes * (1 - liveWeight))
                     + (liveEstimate * liveWeight);
            }

            // No snapshot or not enough snapshot samples — use live/default blend
            return liveEstimate;
        }

        private async Task<int> GetOpenCountersCountAsync(int queueId)
        {
            return await _context.Counters
                .CountAsync(c => c.QueueId == queueId
                              && (c.Status == CounterStatus.Open
                               || c.Status == CounterStatus.Busy));
        }
    }
}

/*
 * CHANGES FROM PREVIOUS VERSION
 * ─────────────────────────────
 * 1. GetAverageServiceTimeAsync — added Tier 2 snapshot lookup.
 *    Snapshots were being written correctly but never read back.
 *    Now: if live data is sparse, the method checks QueueStatSnapshots
 *    for the current hour + day. Requires >= 10 samples in that slot.
 *    Blends snapshot with live data — snapshot influence fades as live
 *    data grows toward MinTicketsForStats.
 *
 * 2. UpdateStatSnapshotsAsync — should now be called from CompleteTicket,
 *    not CallTicket. The value passed must be actual service duration
 *    (CompletedAt - CalledAt), not wait time (CalledAt - CreatedAt).
 *    These are different things — confusing them made snapshot data wrong.
 *
 * WHY TIER 2 REQUIRES >= 10 SAMPLES
 * ────────────────────────────────────
 * A snapshot with 2 samples from 3 months ago is not reliable.
 * 10 samples gives a statistically meaningful average for a time slot
 * without requiring so many that early-stage queues never benefit from it.
 *
 * WHY LIVE DATA WINS WHEN COUNT >= MinTicketsForStats
 * ─────────────────────────────────────────────────────
 * Recent completed tickets reflect current conditions — staffing changes,
 * seasonal patterns, individual staff speed. A snapshot from last Tuesday
 * at 10am is less accurate than the last 20 real completions today.
 * Live data always wins when there is enough of it.
 */