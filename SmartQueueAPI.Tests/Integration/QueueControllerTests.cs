using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartQueue.Core.Data;
using SmartQueue.Core.DTOs.QueueDTOs;
using SmartQueue.Core.Interfaces;
using SmartQueue.Core.Models;
using SmartQueueAPI.Controllers;

namespace SmartQueueAPI.Tests.Integration
{
    public class QueueControllerTests
    {
        // Stvara se "izolirani" in-memory databaza za svaki test 
        private AppDbContext CreateDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        //  Kreira se konrtroler sa mock EstimationServisom
        private QueueController CreateController(AppDbContext db)
        {
            var mockEstimation = new Mock<IEstimationService>();
            mockEstimation
                .Setup(e => e.GetQueueStatusAsync(It.IsAny<int>()))
                .ReturnsAsync(new QueueStatusDto());
            return new QueueController(db, mockEstimation.Object);
        }

        // Seeda se red/qzueue i vraca se ID red.ID 
        private async Task<int> SeedQueue(AppDbContext db,
            string name = "Opca medicina",
            QueueStatus status = QueueStatus.Active)
        {
            var queue = new Queue
            {
                Name = name,
                Description = "Test",
                Status = status,
                DefaultServiceMinutes = 5,
                MinTicketsForStats = 20,
                CreatedAt = DateTime.UtcNow
            };
            db.Queues.Add(queue);
            await db.SaveChangesAsync();
            return queue.Id;
        }

        // TEST 1
        // GET /api/queue mora dohvatiti/vratiti sve redove sa tocnim podacima
        // TotalWaiting mora uzeti samo Waiting tickets , a ne sce tikete 
        // U slucaju da je ovo krivo kiosk/salter i mob app vracaju odnosno pokazuju netocne podatke o duzini reda
        [Fact]
        public async Task GetAll_ReturnsAllQueues_WithCorrectWaitingCount()
        {
            // aarrange
            using var db = CreateDb(nameof(GetAll_ReturnsAllQueues_WithCorrectWaitingCount));
            var queueId = await SeedQueue(db, "Opca medicina");

            //Add-a 3 ticketa , 2 waiting i 1 done - Total waiting mora biti 2 (ne 3)
            
            db.Tickets.Add(new Ticket { QueueId = queueId, Status = TicketStatus.Waiting, TicketNumber = 1, Position = 1, CreatedAt = DateTime.UtcNow });
            db.Tickets.Add(new Ticket { QueueId = queueId, Status = TicketStatus.Waiting, TicketNumber = 2, Position = 2, CreatedAt = DateTime.UtcNow });
            db.Tickets.Add(new Ticket { QueueId = queueId, Status = TicketStatus.Done, TicketNumber = 3, Position = 3, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var controller = CreateController(db);

            // act
            var result = await controller.GetAll();

            // assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var queues = Assert.IsAssignableFrom<IEnumerable<QueueResponseDto>>(ok.Value);
            var first = queues.First();
            Assert.Equal(2, first.TotalWaiting); //kraj-ticket must not be counted "!
        }

        // TEST 2
        // GET /api/queue/{id} -> za nepostojeci red mora vratiti 404,
        // mobapp nesmije crash.at nego izbacit 404
        [Fact]
        public async Task GetById_Returns404_ForNonExistentQueue()
        {
            // Arrange
            using var db = CreateDb(nameof(GetById_Returns404_ForNonExistentQueue));
            var controller = CreateController(db);

            // Act
            var result = await controller.GetById(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // TEST 3
        // GET /api/queue/{id} mora vratiti tocne i realne podatke za postojeci red (postojeci po ID_u)

        // Name, Status and DefaultServiceMinutes mmoraju se podudarati sa onim što je seedano
        [Fact]
        public async Task GetById_ReturnsCorrectQueue_WhenExists()
        {
            // Arrange
            using var db = CreateDb(nameof(GetById_ReturnsCorrectQueue_WhenExists));
            var queueId = await SeedQueue(db, "Blagajna");
            var controller = CreateController(db);

            // Act
            var result = await controller.GetById(queueId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<QueueResponseDto>(ok.Value);
            Assert.Equal("Blagajna", dto.Name);
            Assert.Equal("Active", dto.Status);
        }

        // TEST 4
        // POST /api/queue mora stvoriti novi red i vratiti 201 ---> Created
        //response mora sadržavati ID novog /stvorenog reda 
        //klijenti ga koriste za navigaciju do stranice queue details
        
        [Fact]
        public async Task Create_Returns201_WithNewQueue()
        {
            // Arrange
            using var db = CreateDb(nameof(Create_Returns201_WithNewQueue));
            var controller = CreateController(db);

            var dto = new CreateQueueDto
            {
                Name = "Nova usluga",
                Description = "Test opis",
                DefaultServiceMinutes = 8
            };

            // Act
            var result = await controller.Create(dto);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
            var response = Assert.IsType<QueueResponseDto>(created.Value);
            Assert.Equal("Nova usluga", response.Name);
            Assert.Equal("Active", response.Status); // novi red je "active" po default-u
        }

        // ── TEST 5
        // DELETE /api/queue/{id} kod deleta- mora vratiti 404 ako red ne postoji
        
        [Fact]
        public async Task Delete_Returns404_ForNonExistentQueue()
        {
            // Arrange
            using var db = CreateDb(nameof(Delete_Returns404_ForNonExistentQueue));
            var controller = CreateController(db);

            // Act
            var result = await controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}