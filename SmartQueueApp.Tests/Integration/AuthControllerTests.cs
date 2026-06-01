using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using SmartQueue.Core.DTOs.AuthDTOs;
using SmartQueueApp.Controllers;
using SmartQueueApp.Models.ViewModels;
using SmartQueueApp.Services;

namespace SmartQueueApp.Tests.Integration
{
    public class AuthControllerTests
    {
        //  Helper: creates a real TokenService with a fake HttpContext
         //Helper -> kreaira realan TokenServis sa laznim HttpContext-om
         //TokenService metode nisu virtualne , pa ih Moq nemoze mockat
         //koristi se prava instanca
        
        private TokenService CreateTokenService(HttpContext httpContext)
        {
            var mockAccessor = new Mock<IHttpContextAccessor>();
            mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);
            return new TokenService(mockAccessor.Object);
        }

        //  Helper: creates AuthController with full context setup
        // Helper: Stvara AuthController sa punim context setup-om
        // 3 servisa moraju biti registrirani na HttpContext 

        // .RequestServices:
        //   1. IAuthenticationService , treba ga HttpContext.SignInAsync / SignOutAsync
        //   2. IUrlHelperFactory ,treba ga URL .IsLocalUrl() in the Login POST action
        // Bez njih kontroller vraca  InvalidOperationException u runtime-u.
        private (AuthController controller, Mock<IApiService> mockApi) CreateController()
        {
            var mockApi = new Mock<IApiService>();
            var httpContext = new DefaultHttpContext();

            //  Mock IAuthenticationService
            // SignInAsync je pozvan poslje uspjesnog logina kako bi se izdao cookie.
            // SignOutAsync je pozvan kod logouta da makne cookie
            
            var mockAuthService = new Mock<IAuthenticationService>();
            mockAuthService
                .Setup(a => a.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            mockAuthService
                .Setup(a => a.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            //  Mock IUrlHelperFactory
            // AuthController.Login POST zove Url.IsLocalUrl(model.ReturnUrl)
            // radi prevencije open-redirect napada, Url se rijesava preko IUrlHelperFactory
            // to prevent open-redirect attacks. Url is resolved via IUrlHelperFactory
            // Jer bez toga baca:
            // "No service for type IUrlHelperFactory has been registered."
            var mockUrlHelperFactory = new Mock<IUrlHelperFactory>();
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(u => u.IsLocalUrl(It.IsAny<string>()))
                .Returns(false); // simulira no returnUrl redirekt
            mockUrlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(mockUrlHelper.Object);

            //  Registrira oba  servisa u  (fake) IServiceProvider
            // 
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(mockAuthService.Object);
            serviceProvider
                .Setup(s => s.GetService(typeof(IUrlHelperFactory)))
                .Returns(mockUrlHelperFactory.Object);

            httpContext.RequestServices = serviceProvider.Object;

            // Kreira pravi tokenService
            var tokenService = CreateTokenService(httpContext);

            // Spoji kontroler sa svim dependecijima
            var controller = new AuthController(mockApi.Object, tokenService);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            // TempData se koristi od POST login ( da vrati poruku kod uspjesnog ulogiravanja=)
            controller.TempData = new TempDataDictionary(
                httpContext, Mock.Of<ITempDataProvider>());

            return (controller, mockApi);
        }

        //  TEST 1
        // GET /auth/login mora vratiti Login View kada user nije autentificiran
        //
        // TokenService.GetJwt() vraca null (nema cookiea) so the guard condition
        // tada kontroler vraca login formu , a ne redirecta dalje.
        [Fact]
        public void Login_Get_ReturnsLoginView_WhenNotAuthenticated()
        {
            // Arrange
            var (controller, _) = CreateController();

            // Act
            var result = controller.Login();

            // Assert — must show login form, not redirect
            var view = Assert.IsType<ViewResult>(result);
            Assert.IsType<LoginViewModel>(view.Model);
        }

        //  TEST 2
        // POST /auth/login ako je model state invalid (prazna polja)
        // mora vratiti Login view  BEZ zvanja APIa.
        //Prevenira slanje "praznih" na API i izbjegava nepotreban 400 error (na strani APIA)
        
        [Fact]
        public async Task Login_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            // Simulira trigeriranje slanja praznog polja za Imeal
            controller.ModelState.AddModelError("Email", "Email is required");

            var model = new LoginViewModel();

            // Act
            var result = await controller.Login(model);

            // Assert
            Assert.IsType<ViewResult>(result);
            // APi nesmije biti pozvan 
            mockApi.Verify(a => a.LoginAsync(It.IsAny<LoginDto>()), Times.Never);
        }

        //  TEST 3
        // POST /auth/login sa krivim kredencijalima mora ostati na LOgin viewu
        // a ne redirektirati dalje na dashboard
      
        [Fact]
        public async Task Login_Post_ReturnsViewWithError_WhenCredentialsAreWrong()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            // API vraca failure , (kada se unese krivi pass)"
            mockApi
                .Setup(a => a.LoginAsync(It.IsAny<LoginDto>()))
                .ReturnsAsync(ApiResult<AuthResponseDto>.Fail("Invalid credentials", 401));

            var model = new LoginViewModel
            {
                Email = "wrong@example.com",
                Password = "wrongpassword"
            };

            // Act
            var result = await controller.Login(model);

            // Assert, ostaje na LoginPage-u
            var view = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<LoginViewModel>(view.Model);
            Assert.NotNull(viewModel.ErrorMessage);
            Assert.Contains("Invalid", viewModel.ErrorMessage);
        }

        //  TEST 4
        // POST /auth/login with valid Admin credentials must redirect to Admin/Index.
        //sa validnim ADMIN uss i pass mora proslijediti na Admin/Index
        //Role based redirect , admin na admina , Djelatnik -> djelatnik)
        //krivi redirekt salje usera na kontroler za koji nije autoriziran sto triggera 403 Forbidden
       
        [Fact]
        public async Task Login_Post_RedirectsToAdminDashboard_WhenAdminLogsIn()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.LoginAsync(It.IsAny<LoginDto>()))
                .ReturnsAsync(ApiResult<AuthResponseDto>.Ok(new AuthResponseDto
                {
                    // Minimalna validna JWT struktura za TokenService.StoreTokens
                    //
                    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" +
                            ".eyJzdWIiOiJ0ZXN0IiwiZXhwIjo5OTk5OTk5OTk5fQ.test",
                    RefreshToken = "fake-refresh-token",
                    Email = "admin@smartqueue.com",
                    FirstName = "Super",
                    LastName = "Admin",
                    Role = "Admin"
                }));

            var model = new LoginViewModel
            {
                Email = "admin@smartqueue.com",
                Password = "Admin123!"
            };

            // Act
            var result = await controller.Login(model);

            // Assert , Ako je ROla admin -> redirecta na admin/index
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        //  TEST 5
        // POST /auth/logout mora proslijediti na login page . nakon logouta
        //Nakon logouta JWT cookie i refresh token su clearani od strane TokenServisa
       
        [Fact]
        public async Task Logout_RedirectsToLogin()
        {
            // Arrange
            var (controller, mockApi) = CreateController();
            mockApi
                .Setup(a => a.LogoutAsync(It.IsAny<string>()))
                .ReturnsAsync(ApiResult<bool>.Ok(true));

            // Act
            var result = await controller.Logout();

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
        }
    }
}