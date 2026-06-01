using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartQueue.Core.Data;
using SmartQueue.Core.Interfaces;
using SmartQueue.Core.Models;
using SmartQueueAPI.Controllers;
using SmartQueueAPI.Hubs;
using Microsoft.AspNetCore.Mvc;
using SmartQueue.Core.DTOs.TicketDTOs;

namespace SmartQueueAPI.Tests.Integration
{
    public class TicketControllerTests
    {
        // In-memory db za svaki test
        private AppDbContext CreateDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        // stvara kontroller sa mock dependecyima
        private TicketController CreateController(AppDbContext db,
            int estimatedWait = 5)
        {
            var mockEstimation = new Mock<IEstimationService>();
            mockEstimation
                .Setup(e => e.CalculateEstimatedWaitAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(estimatedWait);

            mockEstimation
                .Setup(e => e.GetQueueStatusAsync(It.IsAny<int>()))
                .ReturnsAsync(new SmartQueue.Core.DTOs.QueueDTOs.QueueStatusDto());

            var mockHub = new Mock<IHubContext<QueueHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockGroup = new Mock<IClientProxy>();
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockGroup.Object);

            return new TicketController(db, mockEstimation.Object, mockHub.Object);
        }

        // Seed-a aktivni red (mock naravno)
        private async Task<int> SeedActiveQueue(AppDbContext db)
        {
            var queue = new Queue
            {
                Name = "Opca medicina",
                Description = "Test queue",
                Status = QueueStatus.Active,
                DefaultServiceMinutes = 5,
                MinTicketsForStats = 20,
                CreatedAt = DateTime.UtcNow
            };
            db.Queues.Add(queue);
            await db.SaveChangesAsync();
            return queue.Id;
        }

        // TEST 1
        //"validni" aktivni red vraća 200 OK sa tocnim podacima ticketa
        
        [Fact]
        public async Task TakeTicket_Returns200_WithValidActiveQueue()
        {
            // Arrange
            using var db = CreateDb(nameof(TakeTicket_Returns200_WithValidActiveQueue));
            var queueId = await SeedActiveQueue(db);
            var controller = CreateController(db);

            // Act
            var result = await controller.TakeTicket(
                new TakeTicketDto { QueueId = queueId });

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<TicketResponseDto>(ok.Value);
            Assert.Equal(1, dto.TicketNumber);
            Assert.Equal("Waiting", dto.Status);
            Assert.Equal(queueId, dto.QueueId);
        }

        //  TEST 2
        // AKo red neposotoji izbaci 404 , a ne crash-a
        [Fact]
        public async Task TakeTicket_Returns404_WhenQueueDoesNotExist()
        {
            // Arrange
            using var db = CreateDb(nameof(TakeTicket_Returns404_WhenQueueDoesNotExist));
            var controller = CreateController(db);

            // Act
            var result = await controller.TakeTicket(
                new TakeTicketDto { QueueId = 999 });

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        //  TEST 3
        //Red koji je zatvoren mora vratiti 400 status , "business rule enforcement"
        
        [Fact]
        public async Task TakeTicket_Returns400_WhenQueueIsClosed()
        {
            // Arrange
            using var db = CreateDb(nameof(TakeTicket_Returns400_WhenQueueIsClosed));
            var queue = new Queue
            {
                Name = "Closed Queue",
                Description = "Closed",
                Status = QueueStatus.Closed,
                DefaultServiceMinutes = 5,
                MinTicketsForStats = 20,
                CreatedAt = DateTime.UtcNow
            };
            db.Queues.Add(queue);
            await db.SaveChangesAsync();

            var controller = CreateController(db);

            // Act
            var result = await controller.TakeTicket(
                new TakeTicketDto { QueueId = queue.Id });

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        //  TEST 4
        // Pozicija u redu mora bit count-of-waiting + 1
        // "ako 3 ticketa postoji , slijedeci mora dobiti broj 4"
        [Fact]
        public async Task TakeTicket_AssignsCorrectPosition_WhenOthersWaiting()
        {
            // Arrange
            using var db = CreateDb(nameof(TakeTicket_AssignsCorrectPosition_WhenOthersWaiting));
            var queueId = await SeedActiveQueue(db);

            // Seed-a 3 waiting tickets-a
            for (int i = 1; i <= 3; i++)
            {
                db.Tickets.Add(new Ticket
                {
                    TicketNumber = i,
                    QueueId = queueId,
                    Status = TicketStatus.Waiting,
                    Position = i,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();

            var controller = CreateController(db);

            // Act
            var result = await controller.TakeTicket(
                new TakeTicketDto { QueueId = queueId });

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<TicketResponseDto>(ok.Value);
            Assert.Equal(4, dto.Position);
        }

        // TEST 5
        // GET -dohvaćanje nepostojeceg ticketa mora vratit 404
        [Fact]
        public async Task GetById_Returns404_ForNonExistentTicket()
        {
            // Arrange
            using var db = CreateDb(nameof(GetById_Returns404_ForNonExistentTicket));
            var controller = CreateController(db);

            // Act
            var result = await controller.GetById(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}