using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using SmartQueue.Core.DTOs.CounterDTOs;
using SmartQueue.Core.DTOs.TicketDTOs;
using SmartQueueApp.Controllers;
using SmartQueueApp.Models.ViewModels;
using SmartQueueApp.Services;

namespace SmartQueueApp.Tests.Unit
{
    public class DjelatnikControllerTests
    {
        //  Pomoćna metoda: kreira DjelatnikController s mockiranim ovisnostima
        // DjelatnikController ovisi o IApiService i TokenService.
        // TokenService nije mockabilan (ne-virtualne metode) pa kreiramo
        // stvarnu instancu podržanu lažnim HttpContext-om — isti obrazac kao u
        // AuthControllerTests.
        private (DjelatnikController controller, Mock<IApiService> mockApi)
            CreateController()
        {
            var mockApi = new Mock<IApiService>();
            var httpContext = new DefaultHttpContext();

            var mockAccessor = new Mock<IHttpContextAccessor>();
            mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);
            var tokenService = new TokenService(mockAccessor.Object);

            var controller = new DjelatnikController(mockApi.Object, tokenService);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            // TempData je required (inace exception)
            controller.TempData = new TempDataDictionary(
                httpContext, Mock.Of<ITempDataProvider>());

            return (controller, mockApi);
        }

        //  TEST 1
        // Index mora vratiti ViewResult s DjelatnikDashboardViewModel.
        // Kada je šalter dodijeljen, listići u čekanju moraju biti učitani i
        // ispravno razdvojeni na WaitingTickets i CurrentTicket.
        [Fact]
        public async Task Index_ReturnsView_WithAssignedCounter()
        {
            // Arrange
            var (controller, mockApi) = CreateController();

            // Simulira assignanje saltera djelatniku
            mockApi
                .Setup(a => a.GetMyCounterAsync())
                .ReturnsAsync(ApiResult<CounterResponseDto>.Ok(new CounterResponseDto
                {
                    Id = 1,
                    Name = "Salter 1",
                    QueueId = 1,
                    Status = "Open"
                }));

            // 
            // simulira 2 waiting tiketa i 1 pozvani tiket u redu
            mockApi
                .Setup(a => a.GetQueueTicketsAsync(1))
                .ReturnsAsync(ApiResult<List<TicketResponseDto>>.Ok(
                    new List<TicketResponseDto>
                    {
                        new() { Id = 1, TicketNumber = 10, Status = "Waiting" },
                        new() { Id = 2, TicketNumber = 11, Status = "Waiting" },
                        new() { Id = 3, TicketNumber = 9,  Status = "Called"  }
                    }));

            // Act
            var result = await controller.Index();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DjelatnikDashboardViewModel>(view.Model);
            Assert.NotNull(model.MyCounter);
            // Samo Waiting tickets idu u   WaitingTickets list
            Assert.Equal(2, model.WaitingTickets.Count);
            // Pozvani ticket postaje  CurrentTicket
            Assert.NotNull(model.CurrentTicket);
            Assert.Equal("Called", model.CurrentTicket.Status);
        }

        //  TEST 2
        // Index mora vratiti ViewResult bez greške kada nije dodijeljen šalter.
        // 404 od GetMyCounterAsync je valjano stanje — djelatniku jednostavno
        // još nije dodijeljen šalter. Mora prikazati prazno stanje, ne poruku greške.
        [Fact]
        public async Task Index_ReturnsView_WithNoError_WhenNoCounterAssigned()
        {
            // Arrange
            var (controller, mockApi) = CreateController();

            // 404 = no counter assigned ->validno i ocekivano stanje
            mockApi
                .Setup(a => a.GetMyCounterAsync())
                .ReturnsAsync(ApiResult<CounterResponseDto>.Fail("Not found", 404));

            // Act
            var result = await controller.Index();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DjelatnikDashboardViewModel>(view.Model);
            // ErrorMessage mora biti null
            Assert.Null(model.ErrorMessage);
            Assert.Null(model.MyCounter);
        }

        //  TEST 3
        // CallTicket mora preusmjeriti natrag na Index nakon uspješnog pozivanja ticketa.
        // Djelatnik ostaje na svojoj nadzornoj ploči — bez navigacije izvan stranice.
        [Fact]
        public async Task CallTicket_RedirectsToIndex_AfterSuccess()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.CallTicketAsync(
                    It.IsAny<int>(),
                    It.IsAny<UpdateTicketStatusDto>()))
                .ReturnsAsync(ApiResult<bool>.Ok(true));

            // Act
            var result = await controller.CallTicket(ticketId: 1, counterId: 1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        //  TEST 4
        // CompleteTicket mora preusmjeriti na Index nakon uspjeha.
        // Dovršavanje listića ažurira red — osvježavanje Indexa prikazuje sljedećeg korisnika.
        [Fact]
        public async Task CompleteTicket_RedirectsToIndex_AfterSuccess()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.CompleteTicketAsync(It.IsAny<int>()))
                .ReturnsAsync(ApiResult<bool>.Ok(true));

            // Act
            var result = await controller.CompleteTicket(ticketId: 1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        //  TEST 5
        // SkipTicket mora preusmjeriti na Index nakon uspjeha.
        // Preskakanje pomiče red naprijed ,a Djelatnik vidi sljedeći ticket u čekanju.
        [Fact]
        public async Task SkipTicket_RedirectsToIndex_AfterSuccess()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.SkipTicketAsync(It.IsAny<int>()))
                .ReturnsAsync(ApiResult<bool>.Ok(true));

            // Act
            var result = await controller.SkipTicket(ticketId: 1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }
    }
}