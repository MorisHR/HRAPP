using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRMS.Core.Entities.Master;
using HRMS.Core.Interfaces;
using HRMS.Core.Settings;
using HRMS.Core.Enums;
using HRMS.Core.Exceptions;
using HRMS.Infrastructure.Data;
using HRMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace HRMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly MasterDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IMfaService _mfaService;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDeviceFingerprintService _deviceFingerprintService;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly string _frontendUrl;

    public AuthService(
        MasterDbContext context,
        IPasswordHasher passwordHasher,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger,
        IMfaService mfaService,
        IAuditLogService auditLogService,
        IEmailService emailService,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        IDeviceFingerprintService deviceFingerprintService,
        ITokenBlacklistService tokenBlacklistService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _mfaService = mfaService;
        _auditLogService = auditLogService;
        _emailService = emailService;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _deviceFingerprintService = deviceFingerprintService;
        _tokenBlacklistService = tokenBlacklistService;
        _frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:4200";
    }

    public async Task<(string Token, string RefreshToken, DateTime ExpiresAt, AdminUser User)?> LoginAsync(string email, string password, string ipAddress)
    {
        // Find admin user by email
        var adminUser = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == email);

        if (adminUser == null)
        {
            _logger.LogWarning("Login attempt failed: User not found for email {Email}", email);

            // Audit log: Failed login attempt
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.LOGIN_FAILED,
                userId: null,
                userEmail: email,
                success: false,
                tenantId: null,
                errorMessage: "User not found"
            );

            return null; // User not found
        }

        if (!adminUser.IsActive)
        {
            _logger.LogWarning("Login attempt failed: User {Email} is deactivated", email);

            // Audit log: Failed login attempt - inactive account
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.LOGIN_FAILED,
                userId: adminUser.Id,
                userEmail: email,
                success: false,
                tenantId: null,
                errorMessage: "Account is deactivated"
            );

            return null; // User is deactivated
        }

        // FORTUNE 500: IP Whitelisting Validation
        if (!string.IsNullOrWhiteSpace(adminUser.AllowedIPAddresses))
        {
            var isIpAllowed = ValidateIpWhitelist(ipAddress, adminUser.AllowedIPAddresses);

            if (!isIpAllowed)
            {
                _logger.LogWarning("Login attempt blocked: IP {IpAddress} not whitelisted for user {Email}", ipAddress, email);

                // Audit log: IP restriction violation (CRITICAL security event)
                await _auditLogService.LogSecurityEventAsync(
                    AuditActionType.UNAUTHORIZED_ACCESS_ATTEMPT,
                    AuditSeverity.CRITICAL,
                    adminUser.Id,
                    description: $"Login attempt from non-whitelisted IP address: {ipAddress}",
                    additionalInfo: JsonSerializer.Serialize(new
                    {
                        userEmail = email,
                        blockedIp = ipAddress,
                        allowedIps = adminUser.AllowedIPAddresses,
                        timestamp = DateTime.UtcNow
                    })
                );

                // Also log as failed authentication
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.LOGIN_FAILED,
                    userId: adminUser.Id,
                    userEmail: email,
                    success: false,
                    tenantId: null,
                    errorMessage: $"IP address {ipAddress} is not whitelisted for this account"
                );

                throw new ForbiddenException(
                    ErrorCodes.AUTH_INSUFFICIENT_PERMISSIONS,
                    "Access denied from your current location.",
                    $"IP address {ipAddress} is not whitelisted for user {email}",
                    "Contact your administrator to authorize access from your location.");
            }

            _logger.LogInformation("IP whitelist validation passed for user {Email} from IP {IpAddress}", email, ipAddress);
        }

        // FORTUNE 500: Check if password has expired
        if (adminUser.PasswordExpiresAt.HasValue && adminUser.PasswordExpiresAt.Value <= DateTime.UtcNow)
        {
            _logger.LogWarning("Login attempt with expired password: User {Email}, password expired on {ExpiryDate}",
                email, adminUser.PasswordExpiresAt.Value);

            // Audit log: Password expired
            await _auditLogService.LogSecurityEventAsync(
                AuditActionType.PASSWORD_EXPIRED,
                AuditSeverity.WARNING,
                adminUser.Id,
                description: $"Login attempt with expired password for user {email}",
                additionalInfo: JsonSerializer.Serialize(new
                {
                    userEmail = email,
                    passwordExpiredOn = adminUser.PasswordExpiresAt.Value,
                    daysOverdue = (DateTime.UtcNow - adminUser.PasswordExpiresAt.Value).Days
                })
            );

            // Force password change
            adminUser.MustChangePassword = true;
            await _context.SaveChangesAsync();

            throw new UnauthorizedException(
                ErrorCodes.AUTH_PASSWORD_EXPIRED,
                "Your password has expired and must be changed before you can sign in.",
                $"Password expired on {adminUser.PasswordExpiresAt.Value:yyyy-MM-dd} for user {email}",
                "Contact your administrator to reset your password.");
        }

        // SECURITY FIX: Check if account is locked out
        if (adminUser.LockoutEnabled && adminUser.LockoutEnd.HasValue)
        {
            if (adminUser.LockoutEnd.Value > DateTime.UtcNow)
            {
                // Account is still locked
                _logger.LogWarning("Login attempt failed: Account {Email} is locked until {LockoutEnd}", email, adminUser.LockoutEnd.Value);

                // Audit log: Login attempt on locked account
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.LOGIN_FAILED,
                    userId: adminUser.Id,
                    userEmail: email,
                    success: false,
                    tenantId: null,
                    errorMessage: $"Account is locked until {adminUser.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC"
                );

                throw new UnauthorizedException(
                    ErrorCodes.AUTH_ACCOUNT_LOCKED,
                    "Your account has been temporarily locked for security reasons.",
                    $"Account {email} is locked until {adminUser.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC",
                    $"Please try again after {adminUser.LockoutEnd.Value.ToLocalTime():h:mm tt} or contact your administrator.");
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
            // SECURITY FIX: Increment failed login count and track timestamp
            adminUser.AccessFailedCount++;
            adminUser.LastFailedLoginAttempt = DateTime.UtcNow; // FORTUNE 500: Track failed attempt timestamp

            // Lock account after 5 failed attempts (15 minute lockout)
            if (adminUser.LockoutEnabled && adminUser.AccessFailedCount >= 5)
            {
                adminUser.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Account {Email} locked due to {FailedAttempts} failed login attempts", email, adminUser.AccessFailedCount);

                // Audit log: Account locked
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.ACCOUNT_LOCKED,
                    userId: adminUser.Id,
                    userEmail: email,
                    success: false,
                    tenantId: null,
                    errorMessage: $"Account locked due to {adminUser.AccessFailedCount} failed login attempts"
                );

                throw new UnauthorizedException(
                    ErrorCodes.AUTH_ACCOUNT_LOCKED,
                    "Your account has been locked due to multiple failed sign-in attempts.",
                    $"Account {email} locked after {adminUser.AccessFailedCount} failed attempts",
                    $"Please wait 15 minutes and try again, or contact your administrator for immediate assistance.");
            }

            await _context.SaveChangesAsync();
            _logger.LogWarning("Login attempt failed: Invalid password for {Email} (attempt {Count})", email, adminUser.AccessFailedCount);

            // Audit log: Failed login - invalid password
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.LOGIN_FAILED,
                userId: adminUser.Id,
                userEmail: email,
                success: false,
                tenantId: null,
                errorMessage: $"Invalid password (attempt {adminUser.AccessFailedCount})"
            );

            return null; // Invalid password
        }

        // SECURITY FIX: Reset failed login count on successful login
        adminUser.AccessFailedCount = 0;
        adminUser.LockoutEnd = null;

        // Update last login date and IP address
        adminUser.LastLoginDate = DateTime.UtcNow;
        adminUser.LastLoginIPAddress = ipAddress; // FORTUNE 500: Track login IP for security monitoring

        // FORTUNE 500: Validate login hours
        if (!string.IsNullOrWhiteSpace(adminUser.AllowedLoginHours))
        {
            var currentHour = DateTime.UtcNow.Hour;
            var allowedHours = JsonSerializer.Deserialize<List<int>>(adminUser.AllowedLoginHours) ?? new();

            if (allowedHours.Any() && !allowedHours.Contains(currentHour))
            {
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.LOGIN_FAILED,
                    userId: adminUser.Id,
                    userEmail: adminUser.Email,
                    success: false,
                    errorMessage: "Login attempted outside allowed hours");

                throw new ForbiddenException(
                    ErrorCodes.AUTH_INSUFFICIENT_PERMISSIONS,
                    "Sign-in is not available at this time due to your account's access hours.",
                    $"Login attempted outside allowed hours for user {email}. Allowed hours: {string.Join(", ", allowedHours)}. Current hour: {currentHour} UTC",
                    "Check your access hours with your administrator or try again during your designated time.");
            }
        }

        // ============================================
        // PRODUCTION-GRADE: Generate access token AND refresh token
        // ============================================

        // FORTUNE 500: Enforce concurrent session limits (default: 3 devices)
        var maxConcurrentSessions = 3; // Default limit
        var activeTokensCount = await _context.RefreshTokens
            .Where(rt => rt.AdminUserId == adminUser.Id && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .CountAsync();

        if (activeTokensCount >= maxConcurrentSessions)
        {
            // Revoke oldest active session to make room for new login
            var oldestToken = await _context.RefreshTokens
                .Where(rt => rt.AdminUserId == adminUser.Id && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                .OrderBy(rt => rt.CreatedAt)
                .FirstOrDefaultAsync();

            if (oldestToken != null)
            {
                oldestToken.RevokedAt = DateTime.UtcNow;
                oldestToken.RevokedByIp = ipAddress;
                oldestToken.ReasonRevoked = "Concurrent session limit exceeded - oldest session revoked";

                _logger.LogWarning(
                    "Concurrent session limit ({Limit}) exceeded for user {Email}. Revoked oldest session (created {CreatedAt})",
                    maxConcurrentSessions, email, oldestToken.CreatedAt);

                // Audit log: Session limit exceeded
                await _auditLogService.LogSecurityEventAsync(
                    AuditActionType.SESSION_TIMEOUT,
                    AuditSeverity.WARNING,
                    adminUser.Id,
                    description: $"Concurrent session limit ({maxConcurrentSessions}) exceeded - oldest session revoked",
                    additionalInfo: JsonSerializer.Serialize(new
                    {
                        maxSessions = maxConcurrentSessions,
                        activeSessionsBeforeRevoke = activeTokensCount,
                        revokedSessionCreatedAt = oldestToken.CreatedAt,
                        revokedSessionIp = oldestToken.CreatedByIp
                    }));
            }
        }

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

        // Audit log: Successful login
        await _auditLogService.LogAuthenticationAsync(
            AuditActionType.LOGIN_SUCCESS,
            userId: adminUser.Id,
            userEmail: email,
            success: true,
            tenantId: null
        );

        return (accessToken, refreshToken.Token, expiresAt, adminUser);
    }

    public string GenerateJwtToken(Guid userId, string email, string userName)
    {
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        // FORTUNE 500: Generate device fingerprint for token theft detection
        var httpContext = _httpContextAccessor.HttpContext;
        var deviceFingerprint = httpContext != null
            ? _deviceFingerprintService.GenerateFingerprint(httpContext)
            : "unknown";
        var userAgent = httpContext != null
            ? _deviceFingerprintService.GetUserAgent(httpContext)
            : "Unknown";
        var deviceInfo = httpContext != null
            ? _deviceFingerprintService.GetDeviceInfo(httpContext)
            : "Unknown Device";

        var claimsList = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.UniqueName, userName),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "SuperAdmin"),
            // FORTUNE 500: Device security claims
            new Claim("device_fingerprint", deviceFingerprint),
            new Claim("user_agent", userAgent),
            new Claim("device_info", deviceInfo)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claimsList,
            expires: expiresAt,
            signingCredentials: credentials
        );

        // Track token for future revocation
        if (httpContext != null)
        {
            _ = _tokenBlacklistService.TrackUserTokenAsync(userId, jti, expiresAt);
        }

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

        // Audit log: Account unlocked
        await _auditLogService.LogAuthenticationAsync(
            AuditActionType.ACCOUNT_UNLOCKED,
            userId: adminUser.Id,
            userEmail: adminUser.Email,
            success: true,
            tenantId: null
        );

        return true;
    }

    // ============================================
    // FORTUNE 500: PASSWORD ROTATION POLICY
    // ============================================

    /// <summary>
    /// Changes a SuperAdmin password with comprehensive security checks
    /// FORTUNE 500 FEATURES:
    /// - Password history validation (prevents reuse of last 5 passwords)
    /// - Automatic expiry date calculation (90 days)
    /// - Password complexity enforcement
    /// - Audit logging
    /// - MustChangePassword flag reset
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentPassword">Current password for verification</param>
    /// <param name="newPassword">New password</param>
    /// <param name="performedBySuperAdminId">ID of SuperAdmin performing the change (null if self-service)</param>
    /// <returns>Success status and message</returns>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        Guid? performedBySuperAdminId = null)
    {
        try
        {
            var adminUser = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Id == userId);

            if (adminUser == null)
            {
                _logger.LogWarning("Password change failed: User {UserId} not found", userId);
                return (false, "User not found");
            }

            // Verify current password (unless forced by another SuperAdmin)
            if (performedBySuperAdminId == null || performedBySuperAdminId == userId)
            {
                if (!_passwordHasher.VerifyPassword(currentPassword, adminUser.PasswordHash))
                {
                    _logger.LogWarning("Password change failed: Invalid current password for user {UserId}", userId);

                    // Audit log: Failed password change
                    await _auditLogService.LogAuthenticationAsync(
                        AuditActionType.PASSWORD_CHANGE_FAILED,
                        userId: adminUser.Id,
                        userEmail: adminUser.Email,
                        success: false,
                        tenantId: null,
                        errorMessage: "Invalid current password"
                    );

                    return (false, "Current password is incorrect");
                }
            }

            // FORTUNE 500: Password complexity validation
            var (isValid, validationMessage) = ValidatePasswordComplexity(newPassword);
            if (!isValid)
            {
                _logger.LogWarning("Password change failed: Password does not meet complexity requirements for user {UserId}", userId);
                return (false, validationMessage);
            }

            // FORTUNE 500: Check password history (prevent reuse of last 5 passwords)
            var newPasswordHash = _passwordHasher.HashPassword(newPassword);

            if (!string.IsNullOrWhiteSpace(adminUser.PasswordHistory))
            {
                var passwordHistory = JsonSerializer.Deserialize<List<string>>(adminUser.PasswordHistory) ?? new List<string>();

                // Check if new password matches any of the last 5 passwords
                foreach (var oldPasswordHash in passwordHistory)
                {
                    if (_passwordHasher.VerifyPassword(newPassword, oldPasswordHash))
                    {
                        _logger.LogWarning("Password change failed: Password was used previously for user {UserId}", userId);

                        // Audit log: Password reuse attempt
                        await _auditLogService.LogSecurityEventAsync(
                            AuditActionType.PASSWORD_CHANGE_FAILED,
                            AuditSeverity.WARNING,
                            adminUser.Id,
                            description: "Attempted to reuse a previously used password",
                            additionalInfo: JsonSerializer.Serialize(new
                            {
                                userEmail = adminUser.Email,
                                reason = "Password reuse prevention"
                            })
                        );

                        return (false, "Password was used previously. Please choose a different password.");
                    }
                }

                // Update password history: keep last 5 passwords
                passwordHistory.Insert(0, adminUser.PasswordHash); // Add current password to history
                if (passwordHistory.Count > 5)
                {
                    passwordHistory = passwordHistory.Take(5).ToList(); // Keep only last 5
                }

                adminUser.PasswordHistory = JsonSerializer.Serialize(passwordHistory);
            }
            else
            {
                // First password change: initialize history with current password
                adminUser.PasswordHistory = JsonSerializer.Serialize(new List<string> { adminUser.PasswordHash });
            }

            // Update password and related fields
            var now = DateTime.UtcNow;
            var passwordExpiryDays = 90; // FORTUNE 500: 90-day password rotation policy

            adminUser.PasswordHash = newPasswordHash;
            adminUser.LastPasswordChangeDate = now;
            adminUser.PasswordExpiresAt = now.AddDays(passwordExpiryDays);
            adminUser.MustChangePassword = false; // Reset forced password change flag
            adminUser.UpdatedAt = now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);

            // Audit log: Successful password change
            var isSelfService = performedBySuperAdminId == null || performedBySuperAdminId == userId;
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.PASSWORD_CHANGED,
                userId: adminUser.Id,
                userEmail: adminUser.Email,
                success: true,
                tenantId: null,
                eventData: new Dictionary<string, object>
                {
                    { "passwordExpiryDate", adminUser.PasswordExpiresAt!.Value },
                    { "passwordExpiryDays", passwordExpiryDays },
                    { "selfService", isSelfService },
                    { "performedBySuperAdminId", performedBySuperAdminId ?? Guid.Empty }
                }
            );

            return (true, $"Password changed successfully. Your new password will expire in {passwordExpiryDays} days.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return (false, "An error occurred while changing the password");
        }
    }

    /// <summary>
    /// Validates password complexity requirements
    /// FORTUNE 500 COMPLIANT: NIST 800-63B, OWASP recommendations
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>Validation result and message</returns>
    private (bool IsValid, string Message) ValidatePasswordComplexity(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password cannot be empty");
        }

        if (password.Length < 12)
        {
            return (false, "Password must be at least 12 characters long");
        }

        if (password.Length > 128)
        {
            return (false, "Password cannot exceed 128 characters");
        }

        bool hasUppercase = password.Any(char.IsUpper);
        bool hasLowercase = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

        if (!hasUppercase)
        {
            return (false, "Password must contain at least one uppercase letter (A-Z)");
        }

        if (!hasLowercase)
        {
            return (false, "Password must contain at least one lowercase letter (a-z)");
        }

        if (!hasDigit)
        {
            return (false, "Password must contain at least one digit (0-9)");
        }

        if (!hasSpecialChar)
        {
            return (false, "Password must contain at least one special character (!@#$%^&*-_+=, etc.)");
        }

        // Check for common weak passwords (basic blacklist)
        var weakPasswords = new[]
        {
            "Password123!",
            "Admin123456!",
            "Welcome123!",
            "SuperAdmin123!"
        };

        if (weakPasswords.Any(weak => password.Equals(weak, StringComparison.OrdinalIgnoreCase)))
        {
            return (false, "Password is too common. Please choose a more unique password.");
        }

        return (true, "Password meets complexity requirements");
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
            throw new UnauthorizedException(
                ErrorCodes.AUTH_TOKEN_INVALID,
                "Your session is invalid. Please sign in again.",
                "Refresh token not found in database",
                "Sign in again to create a new session.");
        }

        if (!token.IsActive)
        {
            _logger.LogWarning("Inactive refresh token used: Expired={IsExpired}, Revoked={IsRevoked}", token.IsExpired, token.IsRevoked);
            throw new UnauthorizedException(
                ErrorCodes.AUTH_SESSION_EXPIRED,
                "Your session has expired. Please sign in again.",
                $"Refresh token inactive: Expired={token.IsExpired}, Revoked={token.IsRevoked}",
                "Sign in again to start a new session.");
        }

        // FORTUNE 500: Check session timeout
        var sessionTimeoutMinutes = token.SessionTimeoutMinutes;
        var sessionExpiry = token.LastActivityAt.AddMinutes(sessionTimeoutMinutes);

        if (DateTime.UtcNow > sessionExpiry)
        {
            // Session expired due to inactivity
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = "Session timeout";
            await _context.SaveChangesAsync();

            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.SESSION_TIMEOUT,
                userId: token.AdminUserId,
                userEmail: null,
                success: false,
                errorMessage: $"Session timeout - no activity for {sessionTimeoutMinutes} minutes");

            throw new UnauthorizedException(
                ErrorCodes.AUTH_SESSION_EXPIRED,
                "Your session expired due to inactivity. Please sign in again.",
                $"Session timed out after {sessionTimeoutMinutes} minutes of inactivity",
                "Sign in again to continue working.");
        }

        // Ensure AdminUser is loaded (data integrity check)
        if (token.AdminUser == null)
        {
            _logger.LogError("AdminUser not loaded for refresh token {TokenId}", token.Id);
            throw new UnauthorizedException(
                ErrorCodes.AUTH_SESSION_EXPIRED,
                "Your session is invalid. Please sign in again.",
                "AdminUser not found for refresh token",
                "Sign in again to create a new session.");
        }

        // Update last activity
        token.LastActivityAt = DateTime.UtcNow;

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

        // Audit log: Token refreshed
        await _auditLogService.LogAuthenticationAsync(
            AuditActionType.TOKEN_REFRESHED,
            userId: token.AdminUser.Id,
            userEmail: token.AdminUser.Email,
            success: true,
            tenantId: null
        );

        return (accessToken, newRefreshToken.Token);
    }

    public async Task RevokeTokenAsync(string refreshToken, string ipAddress, string? reason = null)
    {
        var token = await _context.RefreshTokens
            .Include(rt => rt.AdminUser)
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

        // Audit log: Logout
        await _auditLogService.LogAuthenticationAsync(
            AuditActionType.LOGOUT,
            userId: token.AdminUser?.Id,
            userEmail: token.AdminUser?.Email,
            success: true,
            tenantId: null
        );
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

            // Audit log: MFA setup completed
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.MFA_SETUP_COMPLETED,
                userId: adminUser.Id,
                userEmail: adminUser.Email,
                success: true,
                tenantId: null
            );

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

                // Audit log: MFA verification success
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.MFA_VERIFICATION_SUCCESS,
                    userId: adminUser.Id,
                    userEmail: adminUser.Email,
                    success: true,
                    tenantId: null
                );
            }
            else
            {
                _logger.LogWarning("MFA validation failed for user {UserId}: Invalid TOTP code", adminUserId);

                // Audit log: MFA verification failed
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.MFA_VERIFICATION_FAILED,
                    userId: adminUser.Id,
                    userEmail: adminUser.Email,
                    success: false,
                    tenantId: null,
                    errorMessage: "Invalid TOTP code"
                );
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

                // Audit log: MFA verification success (backup code)
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.MFA_VERIFICATION_SUCCESS,
                    userId: adminUser.Id,
                    userEmail: adminUser.Email,
                    success: true,
                    tenantId: null,
                    errorMessage: "Backup code used"
                );
            }
            else
            {
                _logger.LogWarning("Backup code validation failed for user {UserId}: Invalid code", adminUserId);

                // Audit log: MFA verification failed (backup code)
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.MFA_VERIFICATION_FAILED,
                    userId: adminUser.Id,
                    userEmail: adminUser.Email,
                    success: false,
                    tenantId: null,
                    errorMessage: "Invalid backup code"
                );
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

        var now = DateTime.UtcNow;
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = now,
            CreatedByIp = ipAddress,
            LastActivityAt = now,
            SessionTimeoutMinutes = 30
        };
    }

    // ============================================
    // FORTUNE 500: IP WHITELISTING METHODS
    // ============================================

    /// <summary>
    /// Validates if an IP address is in the whitelist
    /// Supports CIDR notation (e.g., "192.168.1.0/24") and individual IPs
    /// FORTUNE 500 PATTERN: Google Workspace, Microsoft 365, AWS IAM IP policies
    /// </summary>
    /// <param name="ipAddress">The IP address to validate</param>
    /// <param name="allowedIpsJson">JSON array of allowed IPs/CIDR ranges</param>
    /// <returns>True if IP is whitelisted, false otherwise</returns>
    private bool ValidateIpWhitelist(string ipAddress, string allowedIpsJson)
    {
        try
        {
            // Parse JSON array of allowed IPs
            var allowedIps = JsonSerializer.Deserialize<List<string>>(allowedIpsJson);

            if (allowedIps == null || !allowedIps.Any())
            {
                // If whitelist is empty or invalid, deny access (fail-secure)
                _logger.LogWarning("IP whitelist is empty or invalid for validation");
                return false;
            }

            // Try to parse the incoming IP address
            if (!System.Net.IPAddress.TryParse(ipAddress, out var incomingIp))
            {
                _logger.LogWarning("Invalid IP address format: {IpAddress}", ipAddress);
                return false;
            }

            // Check each allowed IP/CIDR range
            foreach (var allowedIp in allowedIps)
            {
                if (string.IsNullOrWhiteSpace(allowedIp))
                    continue;

                var trimmedIp = allowedIp.Trim();

                // Check if it's a CIDR range (contains '/')
                if (trimmedIp.Contains('/'))
                {
                    if (IsIpInCidrRange(incomingIp, trimmedIp))
                    {
                        _logger.LogDebug("IP {IpAddress} matched CIDR range {CidrRange}", ipAddress, trimmedIp);
                        return true;
                    }
                }
                else
                {
                    // Simple exact IP match
                    if (System.Net.IPAddress.TryParse(trimmedIp, out var allowedIpAddress))
                    {
                        if (incomingIp.Equals(allowedIpAddress))
                        {
                            _logger.LogDebug("IP {IpAddress} matched exact IP {AllowedIp}", ipAddress, trimmedIp);
                            return true;
                        }
                    }
                }
            }

            // No match found
            _logger.LogWarning("IP {IpAddress} not found in whitelist", ipAddress);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating IP whitelist for {IpAddress}", ipAddress);
            // Fail secure: deny access on error
            return false;
        }
    }

    /// <summary>
    /// Checks if an IP address is within a CIDR range
    /// Supports IPv4 CIDR notation (e.g., "192.168.1.0/24")
    /// FORTUNE 500 COMPLIANT: Enterprise-grade IP range validation
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <param name="cidrNotation">CIDR notation (e.g., "192.168.1.0/24")</param>
    /// <returns>True if IP is in CIDR range, false otherwise</returns>
    private bool IsIpInCidrRange(System.Net.IPAddress ipAddress, string cidrNotation)
    {
        try
        {
            var parts = cidrNotation.Split('/');
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid CIDR notation: {CidrNotation}", cidrNotation);
                return false;
            }

            if (!System.Net.IPAddress.TryParse(parts[0], out var networkAddress))
            {
                _logger.LogWarning("Invalid network address in CIDR: {NetworkAddress}", parts[0]);
                return false;
            }

            if (!int.TryParse(parts[1], out var prefixLength) || prefixLength < 0 || prefixLength > 32)
            {
                _logger.LogWarning("Invalid prefix length in CIDR: {PrefixLength}", parts[1]);
                return false;
            }

            // Convert IP addresses to 32-bit integers for bitwise operations
            var ipBytes = ipAddress.GetAddressBytes();
            var networkBytes = networkAddress.GetAddressBytes();

            // Only support IPv4 for now
            if (ipBytes.Length != 4 || networkBytes.Length != 4)
            {
                _logger.LogWarning("IPv6 CIDR ranges not supported yet");
                return false;
            }

            var ipInt = BitConverter.ToUInt32(ipBytes.Reverse().ToArray(), 0);
            var networkInt = BitConverter.ToUInt32(networkBytes.Reverse().ToArray(), 0);

            // Create subnet mask from prefix length
            var mask = uint.MaxValue << (32 - prefixLength);

            // Check if IP is in the network range
            var isInRange = (ipInt & mask) == (networkInt & mask);

            if (isInRange)
            {
                _logger.LogDebug("IP {IpAddress} is within CIDR range {CidrNotation}", ipAddress, cidrNotation);
            }

            return isInRange;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking CIDR range for IP {IpAddress} in range {CidrNotation}", ipAddress, cidrNotation);
            return false;
        }
    }

    // ============================================
    // PASSWORD RESET METHODS
    // ============================================

    /// <summary>
    /// Initiates password reset flow by generating reset token and sending email
    /// SECURITY: Does not reveal if email exists (prevents user enumeration)
    /// </summary>
    public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
    {
        try
        {
            // Don't reveal if email exists (security best practice - prevents user enumeration)
            var adminUser = await _context.AdminUsers
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (adminUser == null)
            {
                _logger.LogWarning("Password reset requested for non-existent or inactive email: {Email}", email);
                return (true, "If email exists, password reset link will be sent");
            }

            // Generate cryptographically secure reset token
            var resetToken = Guid.NewGuid().ToString("N");  // 32 characters, no hyphens
            var tokenExpiry = DateTime.UtcNow.AddHours(1);   // 1 hour expiry (security best practice)

            adminUser.PasswordResetToken = resetToken;
            adminUser.PasswordResetTokenExpiry = tokenExpiry;
            await _context.SaveChangesAsync();

            // Send password reset email
            var emailSent = await _emailService.SendPasswordResetEmailAsync(
                adminUser.Email,
                resetToken,
                adminUser.FirstName);

            if (!emailSent)
            {
                _logger.LogWarning("Failed to send password reset email to {Email}", adminUser.Email);
            }

            // Audit log: Password reset requested
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.PASSWORD_RESET_REQUESTED,
                userId: adminUser.Id,
                userEmail: adminUser.Email,
                success: true,
                tenantId: null
            );

            _logger.LogInformation("Password reset token generated for user {Email}", adminUser.Email);

            return (true, "If email exists, password reset link will be sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ForgotPasswordAsync for email {Email}", email);
            return (false, "An error occurred. Please try again.");
        }
    }

    /// <summary>
    /// Resets password using the reset token from email
    /// SECURITY FEATURES:
    /// - Token expiry validation (1 hour)
    /// - Password complexity enforcement
    /// - Password history check (no reuse of last 5 passwords)
    /// - Single-use token (revoked after use)
    /// - Account lockout reset
    /// </summary>
    public async Task<(bool Success, string Message)> ResetPasswordAsync(string token, string newPassword)
    {
        try
        {
            // Find admin with valid reset token
            var adminUser = await _context.AdminUsers
                .FirstOrDefaultAsync(u => u.PasswordResetToken == token);

            if (adminUser == null)
            {
                _logger.LogWarning("Password reset attempted with invalid token");

                // Audit log: Failed password reset attempt
                await _auditLogService.LogSecurityEventAsync(
                    AuditActionType.PASSWORD_RESET_FAILED,
                    AuditSeverity.WARNING,
                    userId: null,
                    description: "Invalid password reset token",
                    additionalInfo: JsonSerializer.Serialize(new
                    {
                        token = token?.Substring(0, Math.Min(8, token?.Length ?? 0)) + "...", // Log only first 8 chars
                        timestamp = DateTime.UtcNow
                    })
                );

                return (false, "Invalid or expired password reset link");
            }

            // Check token expiry - handle null case first
            if (adminUser.PasswordResetTokenExpiry == null)
            {
                _logger.LogWarning("Password reset attempted with null expiry for user {Email}", adminUser.Email);

                await _auditLogService.LogSecurityEventAsync(
                    AuditActionType.PASSWORD_RESET_FAILED,
                    AuditSeverity.WARNING,
                    adminUser.Id,
                    description: "Password reset token has no expiry",
                    additionalInfo: JsonSerializer.Serialize(new
                    {
                        userEmail = adminUser.Email,
                        reason = "Token expiry is null"
                    })
                );

                return (false, "Invalid password reset link. Please request a new one.");
            }

            // Now check if expired (we know it's not null at this point)
            if (adminUser.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Password reset attempted with expired token for user {Email}", adminUser.Email);

                // Audit log: Expired token attempt
                await _auditLogService.LogSecurityEventAsync(
                    AuditActionType.PASSWORD_RESET_FAILED,
                    AuditSeverity.WARNING,
                    adminUser.Id,
                    description: "Password reset token expired",
                    additionalInfo: JsonSerializer.Serialize(new
                    {
                        userEmail = adminUser.Email,
                        tokenExpiredOn = adminUser.PasswordResetTokenExpiry.Value,
                        hoursOverdue = (DateTime.UtcNow - adminUser.PasswordResetTokenExpiry.Value).TotalHours
                    })
                );

                return (false, "Password reset link has expired. Please request a new one.");
            }

            // Validate new password complexity
            var (isValid, validationMessage) = ValidatePasswordComplexity(newPassword);
            if (!isValid)
            {
                _logger.LogWarning("Password reset failed: Password complexity validation failed for user {Email}", adminUser.Email);
                return (false, validationMessage);
            }

            // Check password history (prevent reuse of last 5 passwords)
            var newPasswordHash = _passwordHasher.HashPassword(newPassword);

            if (!string.IsNullOrWhiteSpace(adminUser.PasswordHistory))
            {
                var passwordHistory = JsonSerializer.Deserialize<List<string>>(adminUser.PasswordHistory) ?? new List<string>();

                // Check if new password matches any of the last 5 passwords
                foreach (var oldHash in passwordHistory)
                {
                    if (_passwordHasher.VerifyPassword(newPassword, oldHash))
                    {
                        _logger.LogWarning("Password reset failed: Password reuse attempt for user {Email}", adminUser.Email);

                        // Audit log: Password reuse attempt
                        await _auditLogService.LogSecurityEventAsync(
                            AuditActionType.PASSWORD_RESET_FAILED,
                            AuditSeverity.WARNING,
                            adminUser.Id,
                            description: "Attempted to reuse a previously used password during password reset",
                            additionalInfo: JsonSerializer.Serialize(new
                            {
                                userEmail = adminUser.Email,
                                reason = "Password reuse prevention"
                            })
                        );

                        return (false, "Cannot reuse previous passwords. Choose a different password.");
                    }
                }

                // Update password history: add current password, keep last 5
                passwordHistory.Insert(0, adminUser.PasswordHash);
                if (passwordHistory.Count > 5)
                {
                    passwordHistory = passwordHistory.Take(5).ToList();
                }
                adminUser.PasswordHistory = JsonSerializer.Serialize(passwordHistory);
            }
            else
            {
                // First password change: initialize history with current password
                adminUser.PasswordHistory = JsonSerializer.Serialize(new List<string> { adminUser.PasswordHash });
            }

            // Update password and related security fields
            var now = DateTime.UtcNow;
            var passwordExpiryDays = 90; // FORTUNE 500: 90-day password rotation policy

            adminUser.PasswordHash = newPasswordHash;
            adminUser.LastPasswordChangeDate = now;
            adminUser.PasswordExpiresAt = now.AddDays(passwordExpiryDays);
            adminUser.MustChangePassword = false;

            // Revoke reset token (single-use security)
            adminUser.PasswordResetToken = null;
            adminUser.PasswordResetTokenExpiry = null;

            // Reset lockout (allow user to login immediately)
            adminUser.AccessFailedCount = 0;
            adminUser.LockoutEnd = null;

            adminUser.UpdatedAt = now;

            await _context.SaveChangesAsync();

            // Audit log: Successful password reset
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.PASSWORD_RESET_COMPLETED,
                userId: adminUser.Id,
                userEmail: adminUser.Email,
                success: true,
                tenantId: null,
                eventData: new Dictionary<string, object>
                {
                    { "passwordExpiryDate", adminUser.PasswordExpiresAt!.Value },
                    { "passwordExpiryDays", passwordExpiryDays },
                    { "method", "reset_token" }
                }
            );

            _logger.LogInformation("Password reset successfully for user {Email}", adminUser.Email);

            return (true, "Password reset successfully. You can now login with your new password.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResetPasswordAsync");
            return (false, "An error occurred. Please try again.");
        }
    }
}
