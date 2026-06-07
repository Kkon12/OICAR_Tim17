using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SmartQueue.Core.DTOs.AuthDTOs;
using SmartQueueApp.Models.ViewModels;
using SmartQueueApp.Services;
using System.Security.Claims;

namespace SmartQueueApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly IApiService _apiService;
        private readonly TokenService _tokenService;

        public AuthController(IApiService apiService, TokenService tokenService)
        {
            _apiService = apiService;
            _tokenService = tokenService;
        }

        // ── GET /auth/login ───────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
           
            if (User.Identity?.IsAuthenticated == true
                && !string.IsNullOrEmpty(_tokenService.GetJwt())
                && !_tokenService.IsJwtExpired())
                return RedirectToDashboard();

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // ── POST /auth/login ──────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _apiService.LoginAsync(new LoginDto
            {
                Email = model.Email,
                Password = model.Password
            });

            if (!result.Success)
            {
                model.ErrorMessage = result.ErrorMessage;
                return View(model);
            }

            var auth = result.Data!;

            _tokenService.StoreTokens(
                auth.Token,
                auth.RefreshToken,
                auth.Role,
                auth.FirstName,
                auth.LastName);


            var claims = new List<Claim>
            {
                new(ClaimTypes.Name,           auth.Email),
                new(ClaimTypes.Email,          auth.Email),
                new(ClaimTypes.Role,           auth.Role),
                new(ClaimTypes.NameIdentifier, auth.Email),
                new("FirstName",               auth.FirstName),
                new("LastName",                auth.LastName),
            };

            var identity = new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            TempData["Success"] = $"Welcome back, {auth.FirstName}!";

            if (!string.IsNullOrEmpty(model.ReturnUrl)
                && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToDashboard(auth.Role);
        }

        // ── POST /auth/logout ─────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = _tokenService.GetRefreshToken();
            if (!string.IsNullOrEmpty(refreshToken))
                await _apiService.LogoutAsync(refreshToken);

            _tokenService.ClearTokens();
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login");
        }

        // ── GET /auth/denied ──────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Denied()
            => View();

        // ── PRIVATE ───────────────────────────────────────────────────────────
        private IActionResult RedirectToDashboard(string? role = null)
        {
            role ??= _tokenService.GetRole();
            return role switch
            {
                "Admin" => RedirectToAction("Index", "Admin"),
                "Djelatnik" => RedirectToAction("Index", "Djelatnik"),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }
}

