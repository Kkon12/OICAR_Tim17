using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SmartQueueApp.Services
{
    public class TokenService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Cookie names — centralized so never mistyped
        public const string JwtCookieName = "sq_jwt";
        public const string RefreshTokenCookieName = "sq_refresh";
        public const string UserRoleCookieName = "sq_role";
        public const string UserNameCookieName = "sq_name";

        public TokenService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // ── Store tokens after login ──────────────────────────────────────────
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

            // Role/name stored non-HttpOnly so Razor layout can read for UI hints.
            // These contain NO sensitive auth data — display information only.
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

        // ── Clear all tokens on logout ────────────────────────────────────────
        public void ClearTokens()
        {
            var ctx = _httpContextAccessor.HttpContext!;
            ctx.Response.Cookies.Delete(JwtCookieName);
            ctx.Response.Cookies.Delete(RefreshTokenCookieName);
            ctx.Response.Cookies.Delete(UserRoleCookieName);
            ctx.Response.Cookies.Delete(UserNameCookieName);
        }

        // ── Retrieve current JWT ──────────────────────────────────────────────
        public string? GetJwt()
            => _httpContextAccessor.HttpContext?
               .Request.Cookies[JwtCookieName];

        // ── Retrieve refresh token ────────────────────────────────────────────
        public string? GetRefreshToken()
            => _httpContextAccessor.HttpContext?
               .Request.Cookies[RefreshTokenCookieName];

        // ── Check if JWT is expired ───────────────────────────────────────────
        public bool IsJwtExpired()
        {
            var jwt = GetJwt();
            if (string.IsNullOrEmpty(jwt)) return true;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);
                // 30-second buffer — refresh before actual expiry to avoid
                // a race where the token is valid when checked but expires
                // during the in-flight API call.
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

/*
 * CHANGES FROM PREVIOUS VERSION
 * ─────────────────────────────
 * 1. Removed sq_uid cookie entirely.
 *    AuthResponseDto has no UserId field, so storing auth.Email in sq_uid
 *    caused DjelatnikController to compare an email string against a GUID
 *    (CounterResponseDto.AssignedUserId), which never matched — "No counter
 *    assigned" bug.
 *
 * 2. Added GetUserId() — decodes the user ID directly from the JWT claims.
 *    The API embeds the user's GUID in the "sub" / NameIdentifier claim when
 *    it signs the token. This is the authoritative source of truth and is
 *    always in sync with what CounterResponseDto.AssignedUserId contains.
 *
 * 3. StoreTokens() signature lost the userId parameter — callers updated.
 *
 * WHY COOKIES FOR ROLE/NAME BUT JWT FOR ID
 * ─────────────────────────────────────────
 * Role and name are display data only — the Razor layout needs them to render
 * the navbar without parsing a JWT on every request. They carry no privilege.
 * UserId IS sensitive (used to look up database records), so it must come from
 * the signed JWT that the API issued — not a plain cookie the browser could
 * trivially forge.
 *
 * WHY THREE CLAIM TYPE FALLBACKS IN GetUserId()
 * ───────────────────────────────────────────────
 * Different JWT libraries use different claim names for the subject:
 *   • "sub"                         — RFC 7519 standard
 *   • ClaimTypes.NameIdentifier     — ASP.NET long-form URI
 *   • "nameid"                      — ASP.NET compact form (common in JwtBearer)
 * Checking all three means the code works regardless of which the API uses.
 */
