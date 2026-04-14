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
            // Only redirect if BOTH cookie auth AND a valid token exist.
            // Checking both prevents a loop where the cookie is present but
            // the JWT is already expired.
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

            // Store JWT + refresh token in HttpOnly cookies.
            // User ID is NOT stored in a cookie — it is decoded from the JWT
            // claims on demand via TokenService.GetUserId(). This prevents the
            // bug where storing auth.Email as userId caused counter lookups to
            // compare an email string against a GUID and never match.
            _tokenService.StoreTokens(
                auth.Token,
                auth.RefreshToken,
                auth.Role,
                auth.FirstName,
                auth.LastName);

            // Also sign into ASP.NET Core cookie auth so [Authorize] and
            // User.IsInRole() work in controllers and Razor views.
            // NameIdentifier is intentionally left as the Email here because
            // it is used only for display / identity purposes in the MVC layer.
            // Privileged lookups (counter assignment) use TokenService.GetUserId()
            // which reads the GUID directly from the JWT "sub" claim.
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

/*
 * CHANGES FROM PREVIOUS VERSION
 * ─────────────────────────────
 * 1. StoreTokens() call lost the userId argument — the signature no longer
 *    takes one. User ID is now decoded from JWT claims in TokenService.GetUserId().
 *
 * 2. Login GET guard also checks IsJwtExpired() to prevent a redirect loop
 *    where the ASP.NET Core cookie is still valid but the JWT has expired —
 *    in that case the user should be shown the login page, not redirected.
 *
 * WHY TWO AUTH LAYERS (StoreTokens + SignInAsync)
 * ────────────────────────────────────────────────
 * StoreTokens puts the JWT in an HttpOnly cookie for API call authentication.
 * SignInAsync creates the ASP.NET Core identity that powers [Authorize] and
 * User.IsInRole() in Razor views. Both are required — they serve different
 * purposes and neither alone is sufficient.
 *
 * WHY Url.IsLocalUrl(returnUrl)
 * ──────────────────────────────
 * Prevents open-redirect attacks. A crafted returnUrl=https://evil.com would
 * pass the null check but IsLocalUrl() rejects any absolute external URL.
 */
