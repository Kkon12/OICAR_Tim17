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

        // ── Kalkuliranje prosjecnog vremena (Core formula)
        public async Task<int> CalculateEstimatedWaitAsync(int queueId, int position)
        {
            var avgServiceMinutes = await GetAverageServiceTimeAsync(queueId);
            var openCounters = await GetOpenCountersCountAsync(queueId);

            if (openCounters == 0) openCounters = 1;

            var estimated = (avgServiceMinutes * position) / openCounters;
            return (int)Math.Ceiling(estimated);
        }

        // ── Rekalkuliraj kad se updejta pozicija 
        public async Task<int> RecalculateForPositionAsync(int queueId, int newPosition)
        {
            return await CalculateEstimatedWaitAsync(queueId, newPosition);
        }

      
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
                // prvi ticket
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
                // Rolling average 
               
                var totalMinutes = snapshot.AvgServiceMinutes * snapshot.SampleCount
                                   + actualServiceMinutes;
                snapshot.SampleCount++;
                snapshot.AvgServiceMinutes = totalMinutes / snapshot.SampleCount;
                snapshot.LastUpdated = now;
            }

            await _context.SaveChangesAsync();
        }

        // ── Queue status za SignalR ─────────────────────────────────
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

        // ──Helperi

        private async Task<double> GetAverageServiceTimeAsync(int queueId)
        {
            var queue = await _context.Queues
                .FirstOrDefaultAsync(q => q.Id == queueId);

            if (queue == null) return 5.0;

            // Zadnjih 20 završenih tiketa — nedavni podaci su reprezentativniji
            // od starih. Spora jutra ne bi smjela iskriviti procjene poslijepodneva.
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
            // Dan 1 (0 tiketa): koristi isključivo admin zadanu vrijednost.
            // Tiketi 1-19: postupno miješanje od zadane vrijednosti prema stvarnim podacima.
            // Tiket 20+: potpuno povjerenje u stvarne podatke, zanemarivanje zadane vrijednosti.
            // Ovo sprječava nagli skok točno na tiketu 20.
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
            // Kada postoji dovoljno živih podataka (>= MinTicketsForStats) onda njima i vjerujemo
            // i odmah vraćamo rezultat , nema potrebe provjeravati snimke stanja.
            // Kada su živi podaci rijetki (nema ih dosta), provjeravamo postoji li snimka stanja za
            // trenutni sat + dan s dovoljno uzoraka (>= 10) da bude relevantna.
            // Snimke stanja bilježe činjenicu da se ponedjeljak u 9h razlikuje od
            // petka u 15h — isti red čekanja, vrlo različiti obrasci usluge.
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
                // Miješanje snimke stanja s trenutno dostupnim živim podacima.
                // Kako živi podaci rastu prema MinTicketsForStats, liveWeight
                // se povećava i utjecaj snimke stanja postupno nestaje.
                var liveWeight = (double)count / queue.MinTicketsForStats;
                return (snapshot.AvgServiceMinutes * (1 - liveWeight))
                     + (liveEstimate * liveWeight);
            }

            // Nema snimke stanja ili nedovoljno uzoraka vec se koristi se miješanje živih/zadanih podataka.
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




////////notes dolje





































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