using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartQueue.Core.Data;
using SmartQueue.Core.DTOs;
using SmartQueue.Core.DTOs.AuthDTOs;
using SmartQueue.Core.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SmartQueueAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
        }

        // ── POST /api/auth/register **
        // Public
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return BadRequest(new { message = "Email already in use." });

            var user = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                UserName = dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await EnsureRoleExists("Korisnik");
            await _userManager.AddToRoleAsync(user, "Korisnik");

            return Ok(new { message = "Registration successful." });
        }

        // ── POST /api/auth/register-staff 
       
        [HttpPost("register-staff")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterStaff([FromBody] RegisterStaffDto dto)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return BadRequest(new { message = "Email already in use." });

            var allowedRoles = new[] { "Djelatnik", "Admin" };
            if (!allowedRoles.Contains(dto.Role))
                return BadRequest(new { message = "Invalid role. Use 'Djelatnik' or 'Admin'." });

            var user = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                UserName = dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await EnsureRoleExists(dto.Role);
            await _userManager.AddToRoleAsync(user, dto.Role);

            return Ok(new { message = $"{dto.Role} account created successfully." });
        }

        // ── POST /api/auth/login 
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return Unauthorized(new { message = "Invalid email or password." });

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Korisnik";

            var token = GenerateJwtToken(user, role);
            var refreshToken = await GenerateRefreshToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token.Token,
                RefreshToken = refreshToken,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = role
            });
        }

        // ── POST /api/auth/refresh-token 
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            var principal = GetPrincipalFromExpiredToken(dto.Token);
            if (principal == null)
                return Unauthorized(new { message = "Invalid token." });

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken
                                       && r.UserId == userId);

            if (storedToken == null || !storedToken.IsActive)
                return Unauthorized(new { message = "Invalid or expired refresh token." });

            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Korisnik";

            storedToken.IsRevoked = true;

            var newToken = GenerateJwtToken(user, role);
            var newRefreshToken = await GenerateRefreshToken(user);

            await _context.SaveChangesAsync();

            return Ok(new AuthResponseDto
            {
                Token = newToken.Token,
                RefreshToken = newRefreshToken,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = role
            });
        }

        // ── POST /api/auth/logout 
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] string refreshToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == refreshToken
                                       && r.UserId == userId);

            if (token != null)
            {
                token.IsRevoked = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Logged out successfully." });
        }

        
        
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null)
                return NotFound(new { message = "User not found." });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? "Korisnik",
                IsActive = !user.LockoutEnd.HasValue
                            || user.LockoutEnd <= DateTimeOffset.UtcNow,
                CreatedAt = user.CreatedAt
            });
        }

        // ── GET /api/auth/users 
        
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = _userManager.Users.ToList();

            var result = new List<UserResponseDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = roles.FirstOrDefault() ?? "Korisnik",
                    IsActive = !user.LockoutEnd.HasValue
                                || user.LockoutEnd <= DateTimeOffset.UtcNow,
                    CreatedAt = user.CreatedAt
                });
            }

            return Ok(result);
        }

        // ── PATCH /api/auth/users/{id}/deactivate 
        // Soft delete -> samo admin
        [HttpPatch("users/{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            // Prevent Admin from deactivating themselves
            var requestingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == requestingUserId)
                return BadRequest(new { message = "Cannot deactivate your own account." });

            // LockoutEnd far in future = efektivno su deaktivirani
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            await _userManager.UpdateAsync(user);

            return Ok(new { message = $"User {user.Email} has been deactivated." });
        }

        // ── PATCH /api/auth/users/{id}/activate 
        // Reaktivacija
        [HttpPatch("users/{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            user.LockoutEnd = null;
            user.LockoutEnabled = false;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = $"User {user.Email} has been activated." });
        }

        // ──  HELPERSi
        private (string Token, DateTime ExpiresAt) GenerateJwtToken(
            ApplicationUser user, string role)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(
                double.Parse(_configuration["Jwt:ExpiresInMinutes"]!));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Role, role),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }

        private async Task<string> GenerateRefreshToken(ApplicationUser user)
        {
            var existingTokens = await _context.RefreshTokens
                .Where(r => r.UserId == user.Id && !r.IsRevoked)
                .ToListAsync();
            foreach (var t in existingTokens)
                t.IsRevoked = true;

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshToken = new RefreshToken
            {
                Token = token,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return token;
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(
                token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }

        private async Task EnsureRoleExists(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

/*Why GET /api/auth/me: Every authenticated user needs to know who they are — their role, name and ID.
 * The MVC frontend will call this on login to decide which views to show (Admin panel vs Djelatnik panel).
 * No need to decode the JWT client-side.
--
Why soft delete (deactivate) instead of hard delete: 
 * Deleting a Djelatnik would orphan all their historical ticket data — CounterId references would break.
 * Deactivation via LockoutEnd uses Identity's built-in lockout mechanism 
 * — the user simply cannot login but all their data is preserved.
--
Why LockoutEnd.AddYears(100): Identity's lockout mechanism uses a date 
 * — setting it 100 years in the future is effectively permanent 
 * deactivation while still using the built-in system correctly.
--
Why prevent self-deactivation: An Admin accidentally locking themselves out would
 * require direct database intervention to recover. This guard prevents that scenario entirely.*/