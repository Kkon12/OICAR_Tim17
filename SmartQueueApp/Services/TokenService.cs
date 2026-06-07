using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SmartQueueApp.Services
{
    public class TokenService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Cookie names, centralizirani
        public const string JwtCookieName = "sq_jwt";
        public const string RefreshTokenCookieName = "sq_refresh";
        public const string UserRoleCookieName = "sq_role";
        public const string UserNameCookieName = "sq_name";

        public TokenService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // ── Store tokens after login 
        public void StoreTokens(string jwt, string refreshToken,
            string role, string firstName, string lastName)
        {
            var ctx = _httpContextAccessor.HttpContext!;
            var opts = new CookieOptions
            {
                HttpOnly = true,
                Secure = ctx.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            };

            ctx.Response.Cookies.Append(JwtCookieName, jwt, opts);
            ctx.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, opts);

            var uiOpts = new CookieOptions
            {
                HttpOnly = false,
                Secure = ctx.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            };
            ctx.Response.Cookies.Append(UserRoleCookieName, role, uiOpts);
            ctx.Response.Cookies.Append(UserNameCookieName, $"{firstName} {lastName}", uiOpts);
        }

        // ── Clear all tokens na logout
        public void ClearTokens()
        {
            var ctx = _httpContextAccessor.HttpContext!;
            ctx.Response.Cookies.Delete(JwtCookieName);
            ctx.Response.Cookies.Delete(RefreshTokenCookieName);
            ctx.Response.Cookies.Delete(UserRoleCookieName);
            ctx.Response.Cookies.Delete(UserNameCookieName);
        }

        // ── Retrieve current JWT 
        public string? GetJwt()
            => _httpContextAccessor.HttpContext?
               .Request.Cookies[JwtCookieName];

        // ── Retrieve refresh token 
        public string? GetRefreshToken()
            => _httpContextAccessor.HttpContext?
               .Request.Cookies[RefreshTokenCookieName];

        // ── Check if JWT is expired 
        public bool IsJwtExpired()
        {
            var jwt = GetJwt();
            if (string.IsNullOrEmpty(jwt)) return true;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);
          
                return token.ValidTo < DateTime.UtcNow.AddSeconds(30);
            }
            catch { return true; }
        }

        // ── Get user ID decoded directly from JWT claims ──────────────────────
        // AuthResponseDto does not carry a UserId field, so we decode it here.
        // The API sets the subject claim (ClaimTypes.NameIdentifier / "sub")
        // to the user's GUID when it issues the token.
        public string? GetUserId()
        {
            var jwt = GetJwt();
            if (string.IsNullOrEmpty(jwt)) return null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);

                // Try standard "sub" claim first, then the long-form NameIdentifier
                // claim that ASP.NET Identity uses — whichever the API sets.
                return token.Claims
                    .FirstOrDefault(c =>
                        c.Type == JwtRegisteredClaimNames.Sub ||
                        c.Type == ClaimTypes.NameIdentifier ||
                        c.Type == "nameid")                    // compact claim name
                    ?.Value;
            }
            catch { return null; }
        }

        // ── Get current user role ─────────────────────────────────────────────
        public string? GetRole()
            => _httpContextAccessor.HttpContext?
               .Request.Cookies[UserRoleCookieName];

        // ── Get current user display name ─────────────────────────────────────
        public string? GetUserName()
            => _httpContextAccessor.HttpContext?
               .Request.Cookies[UserNameCookieName];

        // ── Check if user is authenticated ────────────────────────────────────
        public bool IsAuthenticated()
            => !string.IsNullOrEmpty(GetJwt()) && !IsJwtExpired();
    }
}


