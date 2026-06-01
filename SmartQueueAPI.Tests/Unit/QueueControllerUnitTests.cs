using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartQueue.Core.Data;
using SmartQueue.Core.DTOs.QueueDTOs;
using SmartQueue.Core.Interfaces;
using SmartQueue.Core.Models;
using SmartQueueAPI.Controllers;

namespace SmartQueueAPI.Tests.Unit
{
    public class QueueControllerUnitTests
    {
        //  In-memoryDB create
        private AppDbContext CreateDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        // Stvaranje kontrolera sa mock servisom za test
        private QueueController CreateController(AppDbContext db)
        {
            var mockEstimation = new Mock<IEstimationService>();
            mockEstimation
                .Setup(e => e.GetQueueStatusAsync(It.IsAny<int>()))
                .ReturnsAsync(new QueueStatusDto());
            return new QueueController(db, mockEstimation.Object);
        }

        //  TEST 1
        // GET /api/queue vraca praznu listu ako nema redova
        //mob app i kiosk/salter mora izdrzati "prazno stanje" bez crashanja
        //vracanje null umjesto prazne liste izazvalo bi NullReferenceEx.
        
        [Fact]
        public async Task GetAll_ReturnsEmptyList_WhenNoQueuesExist()
        {
            // Arrange
            using var db = CreateDb(nameof(GetAll_ReturnsEmptyList_WhenNoQueuesExist));
            var controller = CreateController(db);

            // Act
            var result = await controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var queues = Assert.IsAssignableFrom<IEnumerable<QueueResponseDto>>(ok.Value);
            Assert.Empty(queues); // must be empty list, never null
        }

        //  TEST 2
        // POST /api/queue mora postaviti Status u Active po defaultu
        //admin ne specificira status pri kreiranju , po defaultu u pocetku mora biti Active
       
        
        [Fact]
        public async Task Create_SetsStatusToActive_ByDefault()
        {
            // Arrange
            using var db = CreateDb(nameof(Create_SetsStatusToActive_ByDefault));
            var controller = CreateController(db);

            var dto = new CreateQueueDto
            {
                Name = "Test Queue",
                Description = "Test",
                DefaultServiceMinutes = 5
            };

            // Act
            var result = await controller.Create(dto);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var response = Assert.IsType<QueueResponseDto>(created.Value);
            Assert.Equal("Active", response.Status);
        }

        //  TEST 3
        // DELETE /api/queue/{id} mora maknuti red iz DB (realno)
        //poslje brisanja , GetById mora vratiti 404 ,kj potvrduje da ga vise nema u bazi (odnosno tocno brisanje)
        // ovime testiramo cijeli Delete cycle
        [Fact]
        public async Task Delete_RemovesQueue_FromDatabase()
        {
            // Arrange
            using var db = CreateDb(nameof(Delete_RemovesQueue_FromDatabase));
            var queue = new Queue
            {
                Name = "To Delete",
                Description = "Will be removed",
                Status = QueueStatus.Active,
                DefaultServiceMinutes = 5,
                MinTicketsForStats = 20,
                CreatedAt = DateTime.UtcNow
            };
            db.Queues.Add(queue);
            await db.SaveChangesAsync();

            var controller = CreateController(db);

            // Act
            await controller.Delete(queue.Id);

            // Assert > queueu vise ne postoji u DB-i
            var getResult = await controller.GetById(queue.Id);
            Assert.IsType<NotFoundObjectResult>(getResult);
        }

        // ── TEST 4
        // PATCH /api/queue/{id}/status mora vratiti 400 za invalid status string
        //"Zatvoren" or "closed" (wrong case) mora biti  odbijen ->> Samo "Aktivni"!
        //Paused i closed su validni
        
        [Fact]
        public async Task UpdateStatus_Returns400_ForInvalidStatus()
        {
            // Arrange
            using var db = CreateDb(nameof(UpdateStatus_Returns400_ForInvalidStatus));
            var queue = new Queue
            {
                Name = "Test",
                Description = "Test",
                Status = QueueStatus.Active,
                DefaultServiceMinutes = 5,
                MinTicketsForStats = 20,
                CreatedAt = DateTime.UtcNow
            };
            db.Queues.Add(queue);
            await db.SaveChangesAsync();

            var controller = CreateController(db);

            // Act > salje invalid status string
            var result = await controller.UpdateStatus(queue.Id, "InvalidStatus");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        //  TEST 5
        //OpenCounters mora ukljucivati samo Open i Busy saltere
        //Zatovrenei "Closed" salteri NESMIJU biti brojani.
        //Jer ako su Zatvoerni salteri brojani , formula za procjenu nece tocno izracunati vrijeme cekanja (estimate time)
        //(Jer ce dijeliti sa krivim brojem saltera)
        
        [Fact]
        public async Task GetById_CountsOnlyOpenAndBusyCounters()
        {
            // Arrange
            using var db = CreateDb(nameof(GetById_CountsOnlyOpenAndBusyCounters));
            var queue = new Queue
            {
                Name = "Test",
                Description = "Test",
                Status = QueueStatus.Active,
                DefaultServiceMinutes = 5,
                MinTicketsForStats = 20,
                CreatedAt = DateTime.UtcNow
            };
            db.Queues.Add(queue);
            await db.SaveChangesAsync();

            
            //dodaj 1 Open + 1 Busy + 1 Closed counter
            db.Counters.Add(new Counter { Name = "S1", QueueId = queue.Id, Status = CounterStatus.Open });
            db.Counters.Add(new Counter { Name = "S2", QueueId = queue.Id, Status = CounterStatus.Busy });
            db.Counters.Add(new Counter { Name = "S3", QueueId = queue.Id, Status = CounterStatus.Closed });
            await db.SaveChangesAsync();

            var controller = CreateController(db);

            // Act
            var result = await controller.GetById(queue.Id);

            // Assert <—> Samo Open + Busy = 2, a ne  3
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<QueueResponseDto>(ok.Value);
            Assert.Equal(2, dto.OpenCounters);
        }
    }
}