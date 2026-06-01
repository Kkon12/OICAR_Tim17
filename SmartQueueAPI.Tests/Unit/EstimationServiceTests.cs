using Microsoft.EntityFrameworkCore;
using SmartQueue.Core.Data;
using SmartQueue.Core.Models;
using SmartQueueAPI.Services;

namespace SmartQueueAPI.Tests.Unit
{
    public class EstimationServiceTests
    {
        //  helper: stvara izolirani in-memory db
        private AppDbContext CreateDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        //  helper: seeda (novi) red i vraca njegov ID
        private async Task<int> SeedQueue(AppDbContext db,
            int defaultServiceMinutes = 5,
            int minTicketsForStats = 20)
        {
            var queue = new Queue
            {
                Name = "Test Queue",
                Description = "Test",
                Status = QueueStatus.Active,
                DefaultServiceMinutes = defaultServiceMinutes,
                MinTicketsForStats = minTicketsForStats,
                CreatedAt = DateTime.UtcNow
            };
            db.Queues.Add(queue);
            await db.SaveChangesAsync();
            return queue.Id;
        }

        //  Helper: dodaje otvoreni salter na/za red
        private async Task SeedCounter(AppDbContext db, int queueId)
        {
            db.Counters.Add(new Counter
            {
                Name = "Salter 1",
                QueueId = queueId,
                Status = CounterStatus.Open
            });
            await db.SaveChangesAsync();
        }

        
        //Helper -> dodaje izvrseni ticket sa znanim vremenom posluzivanja (aka service duration)
        private async Task SeedCompletedTicket(AppDbContext db,
            int queueId, int serviceMinutes, int ticketNumber)
        {
            var called = DateTime.UtcNow.AddMinutes(-serviceMinutes - 1);
            var completed = called.AddMinutes(serviceMinutes);
            db.Tickets.Add(new Ticket
            {
                TicketNumber = ticketNumber,
                QueueId = queueId,
                Status = TicketStatus.Done,
                CreatedAt = called.AddMinutes(-2),
                CalledAt = called,
                CompletedAt = completed,
                Position = 1,
                EstimatedWaitMinutes = serviceMinutes
            });
            await db.SaveChangesAsync();
        }

        //  TEST 1
        // Kada red ima 0 dovrsenih/posluzenih tiketa , servis mora vratiti (defaultni) DefaultServiceMinutes kao estimate/procjenu
        //DefaultServiceMinutes postavlja admin
        //DefaultServiceMinutes predstavlja TIER 1 Baysian starting condition
        //ondosno prvi dan novog reda (kada jos nema dostupnih realnih podataka9
        
        [Fact]
        public async Task Calculate_ReturnsDefault_WhenNoCompletedTickets()
        {
            // Arrange
            using var db = CreateDb(nameof(Calculate_ReturnsDefault_WhenNoCompletedTickets));
            var queueId = await SeedQueue(db, defaultServiceMinutes: 7);
            await SeedCounter(db, queueId);
            var service = new EstimationService(db);

            // Act
            var result = await service.CalculateEstimatedWaitAsync(queueId, position: 1);

            // Assert
            // ceil(7 * 1 / 1) = 7
            Assert.Equal(7, result);
        }

        // TEST 2
        //AKo su otvorena 2 saltera, vrijeme cekanja mora biti /2 (u usporedbi kad je otvoren samo jedan salter)
        //  formula: ceil((avgService * position) / openCounters)
        [Fact]
        public async Task Calculate_DividesWaitByOpenCounters()
        {
            // Arrange
            using var db = CreateDb(nameof(Calculate_DividesWaitByOpenCounters));
            var queueId = await SeedQueue(db, defaultServiceMinutes: 10);

            // dodaj 2 open counters
            db.Counters.Add(new Counter { Name = "S1", QueueId = queueId, Status = CounterStatus.Open });
            db.Counters.Add(new Counter { Name = "S2", QueueId = queueId, Status = CounterStatus.Open });
            await db.SaveChangesAsync();

            var service = new EstimationService(db);

            // Act
            var result = await service.CalculateEstimatedWaitAsync(queueId, position: 4);

            // Assert
            // ceil(10 * 4 / 2) = ceil(20) = 20
            Assert.Equal(20, result);
        }

        //  TEST 3
        // npr . tiket sa pozicijom 8 mora uvijek imati vecu / duzu procjenu cekanja nego tiket sa brojm 3
        //pojednostavljeno - ljudi koji su kasnije dosli u red cekaju duze
        [Fact]
        public async Task Calculate_ReturnsHigherEstimate_ForHigherPosition()
        {
            // Arrange
            using var db = CreateDb(nameof(Calculate_ReturnsHigherEstimate_ForHigherPosition));
            var queueId = await SeedQueue(db, defaultServiceMinutes: 5);
            await SeedCounter(db, queueId);
            var service = new EstimationService(db);

            // Act
            var resultAt3 = await service.CalculateEstimatedWaitAsync(queueId, position: 3);
            var resultAt8 = await service.CalculateEstimatedWaitAsync(queueId, position: 8);

            // Assert
            Assert.True(resultAt8 > resultAt3,
                $"Expected position 8 ({resultAt8}) > position 3 ({resultAt3})");
        }

        //  TEST 4
        // kada postoji vise od 20 (realnih) obradjenih tiketa (MinTicketsForStats(20))
        //servis mora poceti uzimati realne podatke vremena cekanja umjesto default (real avg servuce time)
        // Zapravo se testira prijelaz sa tier1 (default) na realne podatke
        [Fact]
        public async Task Calculate_UsesRealAverage_WhenMinTicketsReached()
        {
            // Arrange
            using var db = CreateDb(nameof(Calculate_UsesRealAverage_WhenMinTicketsReached));
            var queueId = await SeedQueue(db,
                defaultServiceMinutes: 10, // admin default = 10 min
                minTicketsForStats: 20);
            await SeedCounter(db, queueId);

            // Seed 20 tiketa sa 3 min actual service (svaki)
            for (int i = 1; i <= 20; i++)
                await SeedCompletedTicket(db, queueId, serviceMinutes: 3, ticketNumber: i);

            var service = new EstimationService(db);

            // Act
            var result = await service.CalculateEstimatedWaitAsync(queueId, position: 1);

            // Assert
            //sa realnim prosjecnim vremenom od 3 min , rezultat treba biti blizi 3 nego 10
            
            Assert.InRange(result, 1, 5);
        }

        //  TEST 5
        //Rezultat mora uvijek biti >=1 (nikad 0 il negativan) i ceil zaokruzen
        
        // ceil(0.5) = 1, a ne 0. Stiti da korsnik nevidi 0 minuta wait , ili -1 min wait
        [Fact]
        public async Task Calculate_AlwaysReturnsCeiledPositiveValue()
        {
            // Arrange
            using var db = CreateDb(nameof(Calculate_AlwaysReturnsCeiledPositiveValue));
            var queueId = await SeedQueue(db, defaultServiceMinutes: 1);

            // 2 otvorena saltera , uzrokuje podjelu: ceil(1 * 1 / 2) = ceil(0.5) = 1
            db.Counters.Add(new Counter { Name = "S1", QueueId = queueId, Status = CounterStatus.Open });
            db.Counters.Add(new Counter { Name = "S2", QueueId = queueId, Status = CounterStatus.Open });
            await db.SaveChangesAsync();

            var service = new EstimationService(db);

            // Act
            var result = await service.CalculateEstimatedWaitAsync(queueId, position: 1);

            // Assert
            Assert.True(result >= 1, $"Expected result >= 1 but got {result}");
        }
    }
}