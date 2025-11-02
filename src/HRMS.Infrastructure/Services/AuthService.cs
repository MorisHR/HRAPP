using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HRMS.Core.Entities.Master;
using HRMS.Core.Interfaces;
using HRMS.Core.Settings;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HRMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly MasterDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        MasterDbContext context,
        IPasswordHasher passwordHasher,
        IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<(string Token, DateTime ExpiresAt, AdminUser User)?> LoginAsync(string email, string password)
    {
        // Find admin user by email
        var adminUser = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == email);

        if (adminUser == null)
        {
            return null; // User not found
        }

        if (!adminUser.IsActive)
        {
            return null; // User is deactivated
        }

        // SECURITY FIX: Check if account is locked out
        if (adminUser.LockoutEnabled && adminUser.LockoutEnd.HasValue)
        {
            if (adminUser.LockoutEnd.Value > DateTime.UtcNow)
            {
                // Account is still locked
                throw new InvalidOperationException(
                    $"Account is locked until {adminUser.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC. " +
                    $"Please try again later or contact an administrator.");
            }
            else
            {
                // Lockout period has expired, reset
                adminUser.LockoutEnd = null;
                adminUser.AccessFailedCount = 0;
                await _context.SaveChangesAsync();
            }
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(password, adminUser.PasswordHash))
        {
            // SECURITY FIX: Increment failed login count
            adminUser.AccessFailedCount++;

            // Lock account after 5 failed attempts (15 minute lockout)
            if (adminUser.LockoutEnabled && adminUser.AccessFailedCount >= 5)
            {
                adminUser.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync();

                throw new InvalidOperationException(
                    "Account has been locked due to multiple failed login attempts. " +
                    $"Please try again after {adminUser.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC or contact an administrator.");
            }

            await _context.SaveChangesAsync();
            return null; // Invalid password
        }

        // SECURITY FIX: Reset failed login count on successful login
        adminUser.AccessFailedCount = 0;
        adminUser.LockoutEnd = null;

        // Update last login date
        adminUser.LastLoginDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = GenerateJwtToken(adminUser.Id, adminUser.Email, adminUser.UserName);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        return (token, expiresAt, adminUser);
    }

    public string GenerateJwtToken(Guid userId, string email, string userName)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.UniqueName, userName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Email, email),
            new Claim("role", "SuperAdmin") // Add role claim
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken &&
                jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UnlockAccountAsync(Guid userId)
    {
        var adminUser = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (adminUser == null)
        {
            return false;
        }

        // Reset lockout fields
        adminUser.LockoutEnd = null;
        adminUser.AccessFailedCount = 0;
        adminUser.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}
