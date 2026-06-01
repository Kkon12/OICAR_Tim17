using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using SmartQueue.Core.DTOs.QueueDTOs;
using SmartQueue.Core.DTOs.StatsDTOs;
using SmartQueueApp.Controllers;
using SmartQueueApp.Models.ViewModels;
using SmartQueueApp.Services;

namespace SmartQueueApp.Tests.Unit
{
    public class AdminControllerTests
    {
        //  Helper: stvara AdminController sa mokanim IApiService and TempData
        
        //Bez toga , kontroler baca NullReferenceException u testovima.
       
        private (AdminController controller, Mock<IApiService> mockApi) CreateController()
        {
            var mockApi = new Mock<IApiService>();
            var controller = new AdminController(mockApi.Object);

            // TempData must be provided manually in unit tests — there is no
            //TempData mora bit ubacen rucno u unit testovima jer nema HTTP pipelina da ga auto injecta kao u pravom requestu
            controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            return (controller, mockApi);
        }

        //  TEST 1
        //Index mora vratiti ViewResult sa AdminDashboardView modelom
        //AKo APi call fail-a , view se treba renderati sa error porukom -> Nikad Crash sa Unhandeled exceptionom
       
        [Fact]
        public async Task Index_ReturnsViewResult_WithDashboardViewModel()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.GetOverviewStatsAsync())
                .ReturnsAsync(ApiResult<OverviewStatsDto>.Ok(new OverviewStatsDto
                {
                    TotalQueues = 3,
                    TotalTicketsToday = 42
                }));

            // Act
            var result = await controller.Index();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AdminDashboardViewModel>(view.Model);
            Assert.NotNull(model);
            Assert.Null(model.ErrorMessage);
        }

        // ── TEST 2
        //CreateQueue Post sa validnim podacima mora pozvati api i bit redirektan na Queues/Redove
        
        [Fact]
        public async Task CreateQueue_RedirectsToQueues_AfterSuccess()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.CreateQueueAsync(
                    It.IsAny<SmartQueue.Core.DTOs.QueueDTOs.CreateQueueDto>()))
                .ReturnsAsync(ApiResult<QueueResponseDto>.Ok(
                    new QueueResponseDto { Id = 1, Name = "Test" }));

            var model = new CreateQueueViewModel
            {
                Name = "Opca medicina",
                Description = "Test queue",
                DefaultServiceMinutes = 7
            };

            // Act
            var result = await controller.CreateQueue(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Queues", redirect.ActionName);
        }

        //  TEST 3
        // CreateQueue POST sa invalid ModelState mora odmah vratiti  View 
        // Bez zvanja  APIa. This is critical — sending incomplete or invalid
        //Jer slanje nepotpunih ili netocnih podatak API-u gusi network ,a moze i izavati error sa API strane
        
        // data to the API wastes a network call and may cause API-side errors.
        //  [Required] validacija pri CreateQueueViewModel ga treba uloviti prvi
        [Fact]
        public async Task CreateQueue_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            // Simulira validation error kao da je Name polje ostavljeno prazbo
            controller.ModelState.AddModelError("Name", "Name is required");

            var model = new CreateQueueViewModel();

            // Act
            var result = await controller.CreateQueue(model);

            // Assert -> vraca View ,API nije pozvan
            Assert.IsType<ViewResult>(result);
            mockApi.Verify(a => a.CreateQueueAsync(
                It.IsAny<SmartQueue.Core.DTOs.QueueDTOs.CreateQueueDto>()),
                Times.Never);
        }

        //  TEST 4
        // DeleteQueue mora redirectat na /Queues nakon uspjesnog deleta-a 
        
        [Fact]
        public async Task DeleteQueue_RedirectsToQueues_AfterSuccess()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.DeleteQueueAsync(1))
                .ReturnsAsync(ApiResult<bool>.Ok(true));

            // Act
            var result = await controller.DeleteQueue(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Queues", redirect.ActionName);
        }

        //  TEST 5
        // Statistics page mora vratiti  ViewResult sa non null AdminStatisticsViewModel.
        //ako je model null , Razor views ce baciti NullReferenceException
       
        [Fact]
        public async Task Statistics_ReturnsViewResult_WithNonNullModel()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.GetOverviewStatsAsync())
                .ReturnsAsync(ApiResult<OverviewStatsDto>.Ok(new OverviewStatsDto()));
            mockApi
                .Setup(a => a.GetQueuesAsync())
                .ReturnsAsync(ApiResult<List<QueueResponseDto>>.Ok(
                    new List<QueueResponseDto>()));

            // Act
            var result = await controller.Statistics();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AdminStatisticsViewModel>(view.Model);
            Assert.NotNull(model);
            // QueueStats mora biti prazna lista a , ne null 
            Assert.NotNull(model.QueueStats);
        }
    }
}