using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRMS.Core.Entities.Master;
using HRMS.Core.Interfaces;
using HRMS.Core.Settings;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace HRMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly MasterDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IMfaService _mfaService;

    public AuthService(
        MasterDbContext context,
        IPasswordHasher passwordHasher,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger,
        IMfaService mfaService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _mfaService = mfaService;
    }

    public async Task<(string Token, string RefreshToken, DateTime ExpiresAt, AdminUser User)?> LoginAsync(string email, string password, string ipAddress)
    {
        // Find admin user by email
        var adminUser = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == email);

        if (adminUser == null)
        {
            _logger.LogWarning("Login attempt failed: User not found for email {Email}", email);
            return null; // User not found
        }

        if (!adminUser.IsActive)
        {
            _logger.LogWarning("Login attempt failed: User {Email} is deactivated", email);
            return null; // User is deactivated
        }

        // SECURITY FIX: Check if account is locked out
        if (adminUser.LockoutEnabled && adminUser.LockoutEnd.HasValue)
        {
            if (adminUser.LockoutEnd.Value > DateTime.UtcNow)
            {
                // Account is still locked
                _logger.LogWarning("Login attempt failed: Account {Email} is locked until {LockoutEnd}", email, adminUser.LockoutEnd.Value);
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

                _logger.LogWarning("Account {Email} locked due to {FailedAttempts} failed login attempts", email, adminUser.AccessFailedCount);
                throw new InvalidOperationException(
                    "Account has been locked due to multiple failed login attempts. " +
                    $"Please try again after {adminUser.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC or contact an administrator.");
            }

            await _context.SaveChangesAsync();
            _logger.LogWarning("Login attempt failed: Invalid password for {Email} (attempt {Count})", email, adminUser.AccessFailedCount);
            return null; // Invalid password
        }

        // SECURITY FIX: Reset failed login count on successful login
        adminUser.AccessFailedCount = 0;
        adminUser.LockoutEnd = null;

        // Update last login date
        adminUser.LastLoginDate = DateTime.UtcNow;

        // ============================================
        // PRODUCTION-GRADE: Generate access token AND refresh token
        // ============================================

        // Generate JWT access token (short-lived)
        var accessToken = GenerateJwtToken(adminUser.Id, adminUser.Email, adminUser.UserName);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        // Generate cryptographically secure refresh token (long-lived)
        var refreshToken = GenerateRefreshToken(ipAddress);
        refreshToken.AdminUserId = adminUser.Id;

        // Save refresh token to database
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {Email} logged in successfully from IP {IpAddress}", email, ipAddress);

        return (accessToken, refreshToken.Token, expiresAt, adminUser);
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
        _logger.LogInformation("Account {UserId} unlocked manually", userId);
        return true;
    }

    // ============================================
    // PRODUCTION-GRADE TOKEN REFRESH METHODS
    // Implements OWASP best practices
    // ============================================

    public async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        _logger.LogInformation("Token refresh requested from IP {IpAddress}", ipAddress);

        // Find and validate refresh token
        var token = await _context.RefreshTokens
            .Include(rt => rt.AdminUser)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null)
        {
            _logger.LogWarning("Refresh token not found");
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (!token.IsActive)
        {
            _logger.LogWarning("Inactive refresh token used: Expired={IsExpired}, Revoked={IsRevoked}", token.IsExpired, token.IsRevoked);
            throw new UnauthorizedAccessException("Refresh token is expired or revoked");
        }

        // SECURITY: Token Rotation
        // Generate new refresh token and revoke the old one
        var newRefreshToken = GenerateRefreshToken(ipAddress);
        newRefreshToken.AdminUserId = token.AdminUserId;

        // Revoke old token
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReplacedByToken = newRefreshToken.Token;
        token.ReasonRevoked = "Replaced by new token (rotation)";

        // Save new refresh token
        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        // Generate new access token
        var accessToken = GenerateJwtToken(
            token.AdminUser.Id,
            token.AdminUser.Email,
            token.AdminUser.UserName
        );

        _logger.LogInformation("Token refreshed successfully for user {UserId}", token.AdminUserId);

        return (accessToken, newRefreshToken.Token);
    }

    public async Task RevokeTokenAsync(string refreshToken, string ipAddress, string? reason = null)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null || !token.IsActive)
        {
            _logger.LogWarning("Attempted to revoke invalid or already revoked token from IP {IpAddress}", ipAddress);
            return; // Token doesn't exist or already revoked
        }

        // Revoke token
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = reason ?? "Revoked by user (logout)";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Refresh token revoked for user {UserId}, reason: {Reason}", token.AdminUserId, token.ReasonRevoked);
    }

    public async Task RevokeAllTokensAsync(Guid adminUserId, string ipAddress, string reason)
    {
        // Get all active refresh tokens for this user
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.AdminUserId == adminUserId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        if (!tokens.Any())
        {
            _logger.LogInformation("No active tokens found for user {UserId}", adminUserId);
            return;
        }

        // Revoke all active tokens
        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = reason;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked {Count} refresh tokens for user {UserId}, reason: {Reason}",
            tokens.Count, adminUserId, reason);
    }

    // ============================================
    // MULTI-FACTOR AUTHENTICATION (MFA) METHODS
    // ============================================

    /// <summary>
    /// Initiates MFA setup for a user (first login)
    /// Generates TOTP secret, QR code, and backup codes
    /// </summary>
    public async Task<(string Secret, string QrCodeBase64, List<string> BackupCodes)?> SetupMfaAsync(Guid adminUserId, string email)
    {
        try
        {
            var adminUser = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Id == adminUserId);
            if (adminUser == null)
            {
                _logger.LogWarning("MFA Setup failed: AdminUser {UserId} not found", adminUserId);
                return null;
            }

            // Generate TOTP secret
            var secret = _mfaService.GenerateTotpSecret();

            // Generate QR code for Google Authenticator
            var qrCodeBase64 = _mfaService.GenerateQrCode(email, secret, "MorisHR");

            // Generate 10 backup codes
            var backupCodes = _mfaService.GenerateBackupCodes(10);

            _logger.LogInformation("MFA setup initiated for user {Email}", email);

            return (secret, qrCodeBase64, backupCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up MFA for user {UserId}", adminUserId);
            return null;
        }
    }

    /// <summary>
    /// Completes MFA setup after user scans QR code and verifies TOTP
    /// Saves encrypted secret and hashed backup codes to database
    /// </summary>
    public async Task<bool> CompleteMfaSetupAsync(Guid adminUserId, string totpCode, string secret, List<string> backupCodes)
    {
        try
        {
            var adminUser = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Id == adminUserId);
            if (adminUser == null)
            {
                _logger.LogWarning("MFA Complete Setup failed: AdminUser {UserId} not found", adminUserId);
                return false;
            }

            // Validate TOTP code before enabling MFA
            if (!_mfaService.ValidateTotpCode(secret, totpCode))
            {
                _logger.LogWarning("MFA Complete Setup failed: Invalid TOTP code for user {UserId}", adminUserId);
                return false;
            }

            // Hash all backup codes before storing
            var hashedBackupCodes = backupCodes.Select(code => _mfaService.HashBackupCode(code)).ToList();
            var backupCodesJson = JsonSerializer.Serialize(hashedBackupCodes);

            // Save MFA configuration
            adminUser.TwoFactorSecret = secret;
            adminUser.IsTwoFactorEnabled = true;
            adminUser.BackupCodes = backupCodesJson;
            adminUser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("MFA enabled successfully for user {UserId}", adminUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing MFA setup for user {UserId}", adminUserId);
            return false;
        }
    }

    /// <summary>
    /// Validates TOTP code for login
    /// </summary>
    public async Task<bool> ValidateMfaAsync(Guid adminUserId, string totpCode)
    {
        try
        {
            var adminUser = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Id == adminUserId);
            if (adminUser == null || !adminUser.IsTwoFactorEnabled || string.IsNullOrWhiteSpace(adminUser.TwoFactorSecret))
            {
                _logger.LogWarning("MFA validation failed: User {UserId} not found or MFA not enabled", adminUserId);
                return false;
            }

            var isValid = _mfaService.ValidateTotpCode(adminUser.TwoFactorSecret, totpCode);

            if (isValid)
            {
                _logger.LogInformation("MFA validation successful for user {UserId}", adminUserId);
            }
            else
            {
                _logger.LogWarning("MFA validation failed for user {UserId}: Invalid TOTP code", adminUserId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating MFA for user {UserId}", adminUserId);
            return false;
        }
    }

    /// <summary>
    /// Validates backup code for login (phone lost scenario)
    /// Revokes the backup code after successful validation
    /// </summary>
    public async Task<bool> ValidateBackupCodeAsync(Guid adminUserId, string backupCode)
    {
        try
        {
            var adminUser = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Id == adminUserId);
            if (adminUser == null || !adminUser.IsTwoFactorEnabled || string.IsNullOrWhiteSpace(adminUser.BackupCodes))
            {
                _logger.LogWarning("Backup code validation failed: User {UserId} not found or MFA not enabled", adminUserId);
                return false;
            }

            // Validate backup code
            var isValid = _mfaService.ValidateBackupCode(backupCode, adminUser.BackupCodes);

            if (isValid)
            {
                // Revoke the used backup code (single-use)
                adminUser.BackupCodes = _mfaService.RevokeBackupCode(backupCode, adminUser.BackupCodes);
                adminUser.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Backup code validated and revoked for user {UserId}", adminUserId);
            }
            else
            {
                _logger.LogWarning("Backup code validation failed for user {UserId}: Invalid code", adminUserId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating backup code for user {UserId}", adminUserId);
            return false;
        }
    }

    /// <summary>
    /// Gets count of remaining backup codes for a user
    /// </summary>
    public async Task<int> GetRemainingBackupCodesAsync(Guid adminUserId)
    {
        try
        {
            var adminUser = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Id == adminUserId);
            if (adminUser == null || string.IsNullOrWhiteSpace(adminUser.BackupCodes))
            {
                return 0;
            }

            var backupCodeList = JsonSerializer.Deserialize<List<string>>(adminUser.BackupCodes);
            return backupCodeList?.Count ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting remaining backup codes for user {UserId}", adminUserId);
            return 0;
        }
    }

    /// <summary>
    /// Gets an admin user by ID for authentication purposes
    /// </summary>
    public async Task<AdminUser?> GetAdminUserAsync(Guid adminUserId)
    {
        return await _context.AdminUsers.FirstOrDefaultAsync(u => u.Id == adminUserId);
    }

    /// <summary>
    /// Generates JWT and refresh tokens for an authenticated user
    /// </summary>
    public (string Token, string RefreshToken, DateTime ExpiresAt) GenerateTokens(AdminUser admin, string ipAddress)
    {
        // Generate JWT access token
        var accessToken = GenerateJwtToken(admin.Id, admin.Email, admin.UserName);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        // Generate refresh token
        var refreshToken = GenerateRefreshToken(ipAddress);
        refreshToken.AdminUserId = admin.Id;

        // Save refresh token to database
        _context.RefreshTokens.Add(refreshToken);
        _context.SaveChanges(); // Synchronous save for this helper method

        _logger.LogInformation("Tokens generated for user {Email}", admin.Email);

        return (accessToken, refreshToken.Token, expiresAt);
    }

    // ============================================
    // PRIVATE HELPER METHODS
    // ============================================

    /// <summary>
    /// Generates a cryptographically secure random refresh token
    /// Uses RNGCryptoServiceProvider for maximum security
    /// </summary>
    private RefreshToken GenerateRefreshToken(string ipAddress)
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[64]; // 512 bits of entropy
        rng.GetBytes(randomBytes);

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }
}
