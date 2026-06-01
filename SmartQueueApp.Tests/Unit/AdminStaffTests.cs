using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using SmartQueue.Core.DTOs.AuthDTOs;
using SmartQueueApp.Controllers;
using SmartQueueApp.Models.ViewModels;
using SmartQueueApp.Services;

namespace SmartQueueApp.Tests.Unit
{
    public class AdminStaffTests
    {
        // Helper: kreira AdminController sa mokanim IApiService-om i TempData
        
        private (AdminController controller, Mock<IApiService> mockApi) CreateController()
        {
            var mockApi = new Mock<IApiService>();
            var controller = new AdminController(mockApi.Object);

            controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            return (controller, mockApi);
        }

        //  TEST 1
        // GET /admin/staff mora vratiti ViewResult sa AdminStaffViewModelom
        // Lista Usera nesmije biti null , jer ce doci do crasha-a
        //Ako API faila , view mora se pokazati sa error porukom
        
        [Fact]
        public async Task Staff_ReturnsViewResult_WithUserList()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.GetUsersAsync())
                .ReturnsAsync(ApiResult<List<UserResponseDto>>.Ok(
                    new List<UserResponseDto>
                    {
                        new() { Id = "1", FirstName = "Ivan", LastName = "Horvat",
                                Email = "ivan@smartqueue.com", Role = "Djelatnik",
                                IsActive = true }
                    }));

            // Act
            var result = await controller.Staff();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AdminStaffViewModel>(view.Model);
            Assert.NotNull(model.Users);
            Assert.Single(model.Users); //  1 user seedan
        }

        //  TEST 2
        // POST /admin/staff/create  sa  invalid ModelState mora vratiti View Odma BEZ zvanja APIA
        // "Prazno" Ime ili ako nema email-a mora biti uhvacen od validatora prije bilo kakvog network call-a
        
        [Fact]
        public async Task CreateStaff_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            controller.ModelState.AddModelError("Email", "Email is required");

            var model = new CreateStaffViewModel();

            // Act
            var result = await controller.CreateStaff(model);

            // Assert
            Assert.IsType<ViewResult>(result);
            // API must NOT be called !!!! invalid data must never reach the server
            mockApi.Verify(a => a.RegisterStaffAsync(
                It.IsAny<RegisterStaffDto>()), Times.Never);
        }

        //  TEST 3
        // POST /admin/staff/create sa valid podacima mora redirektati na   Staff list.
        //nakon kreiranja Accounta Djelatnika, admin je vracen na Listu djelatnika kako bi se potvrdilo
        //da je novo stovreni akaunt se pojavio u listi odnosno tablici
        
        [Fact]
        public async Task CreateStaff_RedirectsToStaff_AfterSuccess()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.RegisterStaffAsync(It.IsAny<RegisterStaffDto>()))
                .ReturnsAsync(ApiResult<bool>.Ok(true));

            var model = new CreateStaffViewModel
            {
                FirstName = "Maja",
                LastName = "Kovac",
                Email = "maja@smartqueue.com",
                Password = "Djelatnik123!",
                Role = "Djelatnik"
            };

            // Act
            var result = await controller.CreateStaff(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Staff", redirect.ActionName);
        }

        //  TEST 4
        // POST /admin/staff/{id}/deactivate mora redirektat na listu djelatnika nakon success-a
        // Deaktivacija koristi Identity-jev LockoutEnd mehanizam  
        // djelatnik se ne može prijaviti, ali njegovi povijesni podaci su sačuvani (meko brisanje).
        // Admin se mora vratiti na popis osoblja kako bi potvrdio da se status promijenio..
        [Fact]
        public async Task DeactivateStaff_RedirectsToStaff_AfterSuccess()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.DeactivateUserAsync("user-123"))
                .ReturnsAsync(ApiResult<bool>.Ok(true));

            // Act
            var result = await controller.DeactivateStaff("user-123");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Staff", redirect.ActionName);
        }

        //  TEST 5
        // POST /admin/staff/{id}/activate mora preusmjeriti na popis osoblja nakon succesa.
        // Aktivacija briše LockoutEnd ,djelatnik se može ponovno prijaviti.
        // Preslikava DeactivateStaff ,a oba moraju preusmjeravati na Staff radi konzistentnosti.
        [Fact]
        public async Task ActivateStaff_RedirectsToStaff_AfterSuccess()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.ActivateUserAsync("user-123"))
                .ReturnsAsync(ApiResult<bool>.Ok(true));

            // Act
            var result = await controller.ActivateStaff("user-123");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Staff", redirect.ActionName);
        }
    }
}