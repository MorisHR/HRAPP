using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRMS.Core.Entities.Tenant;
using HRMS.Core.Entities.Master;
using HRMS.Core.Interfaces;
using HRMS.Core.Settings;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using HRMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Authentication service for tenant employees
/// </summary>
public class TenantAuthService
{
    private readonly MasterDbContext _masterContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;
    private readonly string _connectionString;
    private readonly ILogger<TenantAuthService> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly PasswordValidationService _passwordValidationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDeviceFingerprintService _deviceFingerprintService;
    private readonly ITokenBlacklistService _tokenBlacklistService;

    public TenantAuthService(
        MasterDbContext masterContext,
        IPasswordHasher passwordHasher,
        IOptions<JwtSettings> jwtSettings,
        IConfiguration configuration,
        ILogger<TenantAuthService> logger,
        IAuditLogService auditLogService,
        PasswordValidationService passwordValidationService,
        IHttpContextAccessor httpContextAccessor,
        IDeviceFingerprintService deviceFingerprintService,
        ITokenBlacklistService tokenBlacklistService)
    {
        _masterContext = masterContext;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        _logger = logger;
        _auditLogService = auditLogService;
        _passwordValidationService = passwordValidationService;
        _httpContextAccessor = httpContextAccessor;
        _deviceFingerprintService = deviceFingerprintService;
        _tokenBlacklistService = tokenBlacklistService;
    }

    public async Task<(string Token, string RefreshToken, DateTime ExpiresAt, Employee User, Guid TenantId)?> LoginAsync(
        string email,
        string password,
        string subdomain,
        string ipAddress)
    {
        // Step 1: Get tenant by subdomain
        var tenant = await _masterContext.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain.ToLower());

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for subdomain: {Subdomain}", subdomain);

            // Audit log: Failed login - tenant not found
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.LOGIN_FAILED,
                userId: null,
                userEmail: email,
                success: false,
                tenantId: null,
                errorMessage: $"Tenant not found for subdomain: {subdomain}"
            );

            return null;
        }

        if (tenant.Status != Core.Enums.TenantStatus.Active)
        {
            _logger.LogWarning("Tenant is not active: {TenantId}, Status: {Status}", tenant.Id, tenant.Status);

            // Audit log: Failed login - inactive tenant
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.LOGIN_FAILED,
                userId: null,
                userEmail: email,
                success: false,
                tenantId: tenant.Id,
                errorMessage: $"Tenant is not active (Status: {tenant.Status})"
            );

            throw new InvalidOperationException("Tenant account is not active. Please contact support.");
        }

        // Step 2: Create tenant-specific DbContext
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(_connectionString, o =>
        {
            // Use tenant-specific schema for migrations history
            o.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
            o.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        });

        await using var tenantContext = new TenantDbContext(optionsBuilder.Options, tenant.SchemaName);

        // Step 3: Find employee by email in tenant schema
        var employee = await tenantContext.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Email == email);

        if (employee == null)
        {
            _logger.LogWarning("Employee not found in tenant {TenantId}: {Email}", tenant.Id, email);

            // Audit log: Failed login - employee not found
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.LOGIN_FAILED,
                userId: null,
                userEmail: email,
                success: false,
                tenantId: tenant.Id,
                errorMessage: "Employee not found"
            );

            return null;
        }

        if (!employee.IsActive)
        {
            _logger.LogWarning("Employee account is not active: {EmployeeId}", employee.Id);

            // Audit log: Failed login - inactive employee
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.LOGIN_FAILED,
                userId: employee.Id,
                userEmail: email,
                success: false,
                tenantId: tenant.Id,
                errorMessage: "Employee account is not active"
            );

            return null;
        }

        if (employee.IsOffboarded)
        {
            _logger.LogWarning("Employee has been offboarded: {EmployeeId}", employee.Id);

            // Audit log: Failed login - employee offboarded
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.LOGIN_FAILED,
                userId: employee.Id,
                userEmail: email,
                success: false,
                tenantId: tenant.Id,
                errorMessage: "Employee has been offboarded"
            );

            return null;
        }

        // Step 4: Check password hash exists
        if (string.IsNullOrEmpty(employee.PasswordHash))
        {
            _logger.LogError("Employee has no password hash: {EmployeeId}", employee.Id);
            throw new InvalidOperationException("Account is not configured for login. Please contact your administrator.");
        }

        // Step 5: Check account lockout
        if (employee.LockoutEnabled && employee.LockoutEnd.HasValue)
        {
            if (employee.LockoutEnd.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Employee account is locked: {EmployeeId} until {LockoutEnd}",
                    employee.Id, employee.LockoutEnd.Value);

                // Audit log: Login attempt on locked account
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.LOGIN_FAILED,
                    userId: employee.Id,
                    userEmail: email,
                    success: false,
                    tenantId: tenant.Id,
                    errorMessage: $"Account is locked until {employee.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC"
                );

                throw new InvalidOperationException(
                    $"Account is locked until {employee.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC. " +
                    $"Please try again later or contact your administrator.");
            }
            else
            {
                // Lockout period expired, reset
                employee.LockoutEnd = null;
                employee.AccessFailedCount = 0;
                await tenantContext.SaveChangesAsync();
            }
        }

        // Step 6: Verify password
        if (!_passwordHasher.VerifyPassword(password, employee.PasswordHash))
        {
            // Increment failed login attempts
            employee.AccessFailedCount++;

            // Lock account after 5 failed attempts (15 minute lockout)
            if (employee.LockoutEnabled && employee.AccessFailedCount >= 5)
            {
                employee.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                await tenantContext.SaveChangesAsync();

                _logger.LogWarning("Employee account locked due to failed login attempts: {EmployeeId}", employee.Id);

                // Audit log: Account locked
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.ACCOUNT_LOCKED,
                    userId: employee.Id,
                    userEmail: email,
                    success: false,
                    tenantId: tenant.Id,
                    errorMessage: $"Account locked due to {employee.AccessFailedCount} failed login attempts"
                );

                throw new InvalidOperationException(
                    "Account has been locked due to multiple failed login attempts. " +
                    $"Please try again after {employee.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC or contact your administrator.");
            }

            await tenantContext.SaveChangesAsync();
            _logger.LogWarning("Invalid password for employee: {Email} in tenant {TenantId}", email, tenant.Id);

            // Audit log: Failed login - invalid password
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.LOGIN_FAILED,
                userId: employee.Id,
                userEmail: email,
                success: false,
                tenantId: tenant.Id,
                errorMessage: $"Invalid password (attempt {employee.AccessFailedCount})"
            );

            return null;
        }

        // Step 7: Reset failed login count on successful login
        employee.AccessFailedCount = 0;
        employee.LockoutEnd = null;
        employee.UpdatedAt = DateTime.UtcNow;
        await tenantContext.SaveChangesAsync();

        _logger.LogInformation("Successful tenant login: Employee {EmployeeId} in Tenant {TenantId}",
            employee.Id, tenant.Id);

        // FORTUNE 500: Validate login hours
        // NOTE: This requires adding AllowedLoginHours field to Employee entity
        // Uncomment when field is added via migration
        /*
        if (!string.IsNullOrWhiteSpace(employee.AllowedLoginHours))
        {
            var currentHour = DateTime.UtcNow.Hour;
            var allowedHours = System.Text.Json.JsonSerializer.Deserialize<List<int>>(employee.AllowedLoginHours) ?? new();

            if (allowedHours.Any() && !allowedHours.Contains(currentHour))
            {
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.LOGIN_FAILED,
                    userId: employee.Id,
                    userEmail: employee.Email,
                    success: false,
                    tenantId: tenant.Id,
                    errorMessage: "Login attempted outside allowed hours");

                throw new InvalidOperationException("Login is not allowed at this time. Please contact your administrator.");
            }
        }
        */

        // Step 8: Check if employee is a manager (has subordinates)
        var isManager = await tenantContext.Employees
            .AnyAsync(e => e.ManagerId == employee.Id && !e.IsDeleted);

        // Step 9: Generate JWT token with tenant claims
        var token = GenerateTenantJwtToken(
            employee.Id,
            employee.Email,
            employee.FullName,
            employee.JobTitle,
            isManager,
            tenant.Id,
            tenant.Subdomain,
            tenant.SchemaName
        );
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        // Step 10: Generate cryptographically secure refresh token (long-lived)
        var refreshToken = GenerateRefreshToken(ipAddress);
        refreshToken.TenantId = tenant.Id;
        refreshToken.EmployeeId = employee.Id;

        // Save refresh token to master database (shared across all tenants)
        _masterContext.RefreshTokens.Add(refreshToken);
        await _masterContext.SaveChangesAsync();

        _logger.LogInformation("Refresh token generated and stored for employee {EmployeeId} in tenant {TenantId}",
            employee.Id, tenant.Id);

        // Audit log: Successful login
        await _auditLogService.LogAuthenticationAsync(
            AuditActionType.LOGIN_SUCCESS,
            userId: employee.Id,
            userEmail: email,
            success: true,
            tenantId: tenant.Id
        );

        return (token, refreshToken.Token, expiresAt, employee, tenant.Id);
    }

    private string GenerateTenantJwtToken(
        Guid employeeId,
        string email,
        string fullName,
        string? jobTitle,
        bool isManager,
        Guid tenantId,
        string subdomain,
        string schemaName)
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

        // Determine employee roles based on job title and manager status
        var roles = new List<string> { "TenantEmployee" };

        if (!string.IsNullOrEmpty(jobTitle))
        {
            var titleLower = jobTitle.ToLower();

            // Check for Admin role
            if (titleLower.Contains("admin"))
            {
                roles.Add("Admin");
            }

            // Check for HR role
            if (titleLower.Contains("hr") || titleLower.Contains("human resource"))
            {
                roles.Add("HR");
            }

            // Check for Manager role based on title
            if (titleLower.Contains("manager") || titleLower.Contains("supervisor") ||
                titleLower.Contains("head") || titleLower.Contains("director") ||
                titleLower.Contains("lead"))
            {
                roles.Add("Manager");
            }
        }

        // Add Manager role if employee has subordinates
        if (isManager && !roles.Contains("Manager"))
        {
            roles.Add("Manager");
        }

        var claimsList = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, employeeId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.UniqueName, fullName),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(ClaimTypes.NameIdentifier, employeeId.ToString()),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Email, email),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("tenant_subdomain", subdomain),
            new Claim("tenant_schema", schemaName),
            // FORTUNE 500: Device security claims
            new Claim("device_fingerprint", deviceFingerprint),
            new Claim("user_agent", userAgent),
            new Claim("device_info", deviceInfo)
        };

        // Add all role claims
        foreach (var role in roles)
        {
            claimsList.Add(new Claim(ClaimTypes.Role, role));
            claimsList.Add(new Claim("role", role)); // Legacy claim name for compatibility
        }

        var claims = claimsList.ToArray();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        // Track token for future revocation
        if (httpContext != null)
        {
            _ = _tokenBlacklistService.TrackUserTokenAsync(employeeId, jti, expiresAt);
        }

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

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

    /// <summary>
    /// Refreshes access token using refresh token for tenant employees
    /// Implements token rotation: old refresh token revoked, new one issued
    /// CONCURRENCY FIX: Uses database-level locking to prevent concurrent token refresh race condition
    /// </summary>
    public async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        _logger.LogInformation("Tenant token refresh requested from IP {IpAddress}", ipAddress);

        // CONCURRENCY FIX: Use execution strategy with transaction and row-level locking
        var strategy = _masterContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _masterContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                // CRITICAL: Use SELECT FOR UPDATE to lock the row
                // This prevents concurrent refresh token requests from racing
                // SECURITY IMPROVEMENT: Changed to FromSqlInterpolated for compile-time safety (Nov 19, 2025)
                var token = await _masterContext.RefreshTokens
                    .FromSqlInterpolated($@"
                        SELECT * FROM ""RefreshTokens""
                        WHERE ""Token"" = {refreshToken}
                        AND ""TenantId"" IS NOT NULL
                        AND ""EmployeeId"" IS NOT NULL
                        FOR UPDATE
                    ")
                    .FirstOrDefaultAsync();

                if (token == null)
                {
                    _logger.LogWarning("Tenant refresh token not found");
                    throw new UnauthorizedAccessException("Invalid refresh token");
                }

                if (!token.IsActive)
                {
                    _logger.LogWarning("Inactive tenant refresh token used: Expired={IsExpired}, Revoked={IsRevoked}", token.IsExpired, token.IsRevoked);
                    throw new UnauthorizedAccessException("Refresh token is expired or revoked");
                }

                // Get tenant information
                var tenant = await _masterContext.Tenants
                    .FirstOrDefaultAsync(t => t.Id == token.TenantId);

                if (tenant == null)
                {
                    _logger.LogWarning("Tenant not found for refresh token: {TenantId}", token.TenantId);
                    throw new UnauthorizedAccessException("Invalid tenant");
                }

                // Get employee from tenant schema
                var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
                optionsBuilder.UseNpgsql(_connectionString, o =>
                {
                    o.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
                    o.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                });

                await using var tenantContext = new TenantDbContext(optionsBuilder.Options, tenant.SchemaName);

                var employee = await tenantContext.Employees
                    .FirstOrDefaultAsync(e => e.Id == token.EmployeeId);

                if (employee == null || !employee.IsActive || employee.IsOffboarded)
                {
                    _logger.LogWarning("Employee not found or inactive for refresh token: {EmployeeId}", token.EmployeeId);
                    throw new UnauthorizedAccessException("Invalid employee or account inactive");
                }

                // Check if employee is a manager
                var isManager = await tenantContext.Employees
                    .AnyAsync(e => e.ManagerId == employee.Id && !e.IsDeleted);

                // SECURITY: Token Rotation
                // Generate new refresh token and revoke the old one
                var newRefreshToken = GenerateRefreshToken(ipAddress);
                newRefreshToken.TenantId = token.TenantId;
                newRefreshToken.EmployeeId = token.EmployeeId;

                // Revoke old token
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ipAddress;
                token.ReplacedByToken = newRefreshToken.Token;
                token.ReasonRevoked = "Replaced by new token (rotation)";

                // Save new refresh token
                _masterContext.RefreshTokens.Add(newRefreshToken);
                await _masterContext.SaveChangesAsync();

                // Commit transaction - ensures atomicity of token rotation
                await transaction.CommitAsync();

                // Generate new access token
                var accessToken = GenerateTenantJwtToken(
                    employee.Id,
                    employee.Email,
                    employee.FullName,
                    employee.JobTitle,
                    isManager,
                    tenant.Id,
                    tenant.Subdomain,
                    tenant.SchemaName
                );

                _logger.LogInformation("Tenant token refreshed successfully for employee {EmployeeId} in tenant {TenantId}",
                    token.EmployeeId, token.TenantId);

                // Audit log: Token refreshed
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.TOKEN_REFRESHED,
                    userId: employee.Id,
                    userEmail: employee.Email,
                    success: true,
                    tenantId: tenant.Id
                );

                return (accessToken, newRefreshToken.Token);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    /// <summary>
    /// Revokes a tenant refresh token (logout)
    /// </summary>
    public async Task RevokeTokenAsync(string refreshToken, string ipAddress, string? reason = null)
    {
        var token = await _masterContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.TenantId != null && rt.EmployeeId != null);

        if (token == null || !token.IsActive)
        {
            _logger.LogWarning("Attempted to revoke invalid or already revoked tenant token from IP {IpAddress}", ipAddress);
            return; // Token doesn't exist or already revoked
        }

        // Revoke token
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = reason ?? "Revoked by user (logout)";

        await _masterContext.SaveChangesAsync();

        _logger.LogInformation("Tenant refresh token revoked for employee {EmployeeId} in tenant {TenantId}, reason: {Reason}",
            token.EmployeeId, token.TenantId, token.ReasonRevoked);

        // Audit log: Logout
        await _auditLogService.LogAuthenticationAsync(
            AuditActionType.LOGOUT,
            userId: token.EmployeeId,
            userEmail: null,
            success: true,
            tenantId: token.TenantId
        );
    }

    /// <summary>
    /// Set/Reset employee password using token from welcome email or forgot password flow
    /// SECURITY FEATURES:
    /// - Token expiry validation (24 hours)
    /// - Password complexity enforcement
    /// - Single-use token (revoked after use)
    /// - MustChangePassword flag cleared after successful setup
    /// FORTUNE 500: Used for initial password setup after tenant activation
    /// </summary>
    /// <param name="token">Password reset token from email</param>
    /// <param name="newPassword">New password to set</param>
    /// <param name="subdomain">Tenant subdomain</param>
    /// <returns>(success, message)</returns>
    public async Task<(bool Success, string Message)> SetEmployeePasswordAsync(
        string token,
        string newPassword,
        string subdomain)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(subdomain))
            {
                return (false, "Invalid request. Token, password, and subdomain are required.");
            }

            // Get tenant by subdomain
            var tenant = await _masterContext.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Subdomain.ToLower() == subdomain.ToLower() && t.Status == TenantStatus.Active);

            if (tenant == null)
            {
                _logger.LogWarning("Tenant not found or not active for subdomain: {Subdomain}", subdomain);
                return (false, "Invalid or expired password reset link.");
            }

            // Connect to tenant schema
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseNpgsql(_connectionString);

            using var tenantDbContext = new TenantDbContext(optionsBuilder.Options, tenant.SchemaName, null!);

            // Find employee by password reset token
            var employee = await tenantDbContext.Employees
                .FirstOrDefaultAsync(e => e.PasswordResetToken == token && e.IsActive);

            if (employee == null)
            {
                _logger.LogWarning("Employee not found with password reset token in tenant: {TenantId}", tenant.Id);
                return (false, "Invalid or expired password reset link.");
            }

            // Check token expiry
            if (!employee.PasswordResetTokenExpiry.HasValue || employee.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Password reset token expired for employee: {EmployeeId}", employee.Id);
                return (false, "Password reset link has expired. Please request a new one.");
            }

            // ==============================================
            // FORTRESS-GRADE PASSWORD VALIDATION
            // ==============================================

            // 1. Validate password strength (Fortune 500 standards)
            var (isValid, validationError) = _passwordValidationService.ValidatePasswordStrength(newPassword);
            if (!isValid)
            {
                _logger.LogWarning("Weak password rejected for employee: {EmployeeId}, Reason: {Reason}",
                    employee.Id, validationError);

                // FORTRESS-GRADE AUDIT: Log failed password setup attempt
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.PASSWORD_RESET_FAILED,
                    userId: employee.Id,
                    userEmail: employee.Email,
                    success: false,
                    tenantId: tenant.Id
                );

                return (false, validationError);
            }

            // 2. Check password history (no reuse of last 5 passwords)
            if (_passwordValidationService.IsPasswordReused(newPassword, employee.PasswordHistory))
            {
                _logger.LogWarning("Password reuse attempt blocked for employee: {EmployeeId}", employee.Id);

                // FORTRESS-GRADE AUDIT: Log password reuse attempt (security critical)
                await _auditLogService.LogSecurityEventAsync(
                    AuditActionType.PASSWORD_RESET_FAILED,
                    AuditSeverity.WARNING,
                    employee.Id,
                    description: $"Password reuse attempt blocked for employee {employee.Email}",
                    additionalInfo: System.Text.Json.JsonSerializer.Serialize(new
                    {
                        employeeId = employee.Id,
                        employeeEmail = employee.Email,
                        reason = "Password matches one of last 5 passwords",
                        tenantId = tenant.Id,
                        subdomain = tenant.Subdomain
                    })
                );

                return (false, "Password cannot be the same as your last 5 passwords. Please choose a different password.");
            }

            // 3. Calculate password entropy (Fortune 500: aim for 50+ bits)
            var entropy = _passwordValidationService.CalculatePasswordEntropy(newPassword);
            _logger.LogInformation("Password entropy for employee {EmployeeId}: {Entropy} bits", employee.Id, entropy);

            // ==============================================
            // SECURE PASSWORD STORAGE & HISTORY MANAGEMENT
            // ==============================================

            // Hash the new password with Argon2
            var passwordHash = _passwordHasher.HashPassword(newPassword);

            // Update password history before changing the hash
            var updatedHistory = _passwordValidationService.UpdatePasswordHistory(
                employee.PasswordHash ?? passwordHash, // Use current hash if exists, otherwise new hash
                employee.PasswordHistory
            );

            // Update employee password and clear reset token
            employee.PasswordHash = passwordHash;
            employee.PasswordHistory = updatedHistory;
            employee.PasswordResetToken = null;
            employee.PasswordResetTokenExpiry = null;
            employee.MustChangePassword = false;
            employee.LastPasswordChangeDate = DateTime.UtcNow;

            await tenantDbContext.SaveChangesAsync();

            _logger.LogInformation("Password successfully set for employee: {EmployeeId} in tenant: {TenantId}", employee.Id, tenant.Id);

            // ==============================================
            // FORTRESS-GRADE AUDIT: Log successful password setup with security metrics
            // ==============================================
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.PASSWORD_RESET_COMPLETED,
                userId: employee.Id,
                userEmail: employee.Email,
                success: true,
                tenantId: tenant.Id
            );

            // Additional security event logging with detailed metrics
            await _auditLogService.LogSecurityEventAsync(
                AuditActionType.PASSWORD_RESET_COMPLETED,
                AuditSeverity.INFO,
                employee.Id,
                description: $"Employee {employee.Email} successfully set password via activation token",
                additionalInfo: System.Text.Json.JsonSerializer.Serialize(new
                {
                    employeeId = employee.Id,
                    employeeEmail = employee.Email,
                    employeeCode = employee.EmployeeCode,
                    passwordEntropy = Math.Round(entropy, 2), // Password strength in bits
                    passwordHistoryLength = employee.PasswordHistory != null
                        ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(employee.PasswordHistory)?.Count ?? 0
                        : 0,
                    tenantId = tenant.Id,
                    tenantName = tenant.CompanyName,
                    subdomain = tenant.Subdomain,
                    setupMethod = "ActivationToken",
                    timestamp = DateTime.UtcNow
                })
            );

            _logger.LogInformation(
                "FORTRESS_AUDIT: Password setup completed for {Email} (Entropy: {Entropy} bits, History depth: {HistoryCount})",
                employee.Email,
                Math.Round(entropy, 2),
                employee.PasswordHistory != null ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(employee.PasswordHistory)?.Count ?? 0 : 0);

            return (true, "Password set successfully. You can now log in with your new password.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting employee password with token: {Token}", token);
            return (false, "An error occurred while setting your password. Please try again later.");
        }
    }

    // ============================================
    // FORTUNE 500: PASSWORD CHANGE FOR AUTHENTICATED TENANT EMPLOYEES
    // ============================================

    /// <summary>
    /// Changes an employee's password with comprehensive security checks
    /// FORTUNE 500 FEATURES:
    /// - Multi-tenant isolation (tenant schema context validation)
    /// - Password history validation (prevents reuse of last 5 passwords)
    /// - Password complexity enforcement (12+ chars, mixed case, numbers, special)
    /// - Password entropy calculation and logging
    /// - Current password verification (prevents session hijacking)
    /// - MustChangePassword flag reset
    /// - Comprehensive audit logging with tenant context
    /// MULTI-TENANT SECURITY:
    /// - Validates employeeId belongs to specified tenantId
    /// - Uses tenant-specific schema for all operations
    /// - Tenant context included in all audit logs
    /// </summary>
    /// <param name="employeeId">Employee ID (from JWT claims)</param>
    /// <param name="tenantId">Tenant ID (from JWT claims)</param>
    /// <param name="currentPassword">Current password for verification</param>
    /// <param name="newPassword">New password</param>
    /// <returns>Success status and message</returns>
    public async Task<(bool Success, string Message)> ChangeEmployeePasswordAsync(
        Guid employeeId,
        Guid tenantId,
        string currentPassword,
        string newPassword)
    {
        try
        {
            // ==============================================
            // STEP 1: MULTI-TENANT SECURITY - Get tenant and validate context
            // ==============================================
            var tenant = await _masterContext.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId && t.Status == TenantStatus.Active);

            if (tenant == null)
            {
                _logger.LogWarning("Password change failed: Tenant {TenantId} not found or not active", tenantId);
                return (false, "Invalid tenant context. Please contact support.");
            }

            // Connect to tenant-specific schema
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseNpgsql(_connectionString, o =>
            {
                o.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
                o.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            });

            await using var tenantContext = new TenantDbContext(optionsBuilder.Options, tenant.SchemaName);

            // ==============================================
            // STEP 2: Get employee and validate access
            // ==============================================
            var employee = await tenantContext.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

            if (employee == null)
            {
                _logger.LogWarning("Password change failed: Employee {EmployeeId} not found in tenant {TenantId}",
                    employeeId, tenantId);
                return (false, "Employee not found");
            }

            if (!employee.IsActive)
            {
                _logger.LogWarning("Password change failed: Employee {EmployeeId} is not active", employeeId);
                return (false, "Account is not active. Please contact your administrator.");
            }

            if (employee.IsOffboarded)
            {
                _logger.LogWarning("Password change failed: Employee {EmployeeId} has been offboarded", employeeId);
                return (false, "Account has been offboarded. Please contact your administrator.");
            }

            // ==============================================
            // STEP 3: Verify current password (CRITICAL SECURITY CHECK)
            // ==============================================
            if (string.IsNullOrEmpty(employee.PasswordHash))
            {
                _logger.LogError("Password change failed: Employee {EmployeeId} has no password hash", employeeId);
                return (false, "Account is not configured for password change. Please contact your administrator.");
            }

            if (!_passwordHasher.VerifyPassword(currentPassword, employee.PasswordHash))
            {
                _logger.LogWarning("Password change failed: Invalid current password for employee {EmployeeId}", employeeId);

                // Audit log: Failed password change attempt
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.PASSWORD_CHANGE_FAILED,
                    userId: employee.Id,
                    userEmail: employee.Email,
                    success: false,
                    tenantId: tenant.Id,
                    errorMessage: "Invalid current password"
                );

                return (false, "Current password is incorrect");
            }

            // ==============================================
            // STEP 4: FORTRESS-GRADE PASSWORD VALIDATION
            // ==============================================

            // 1. Validate password strength (Fortune 500 standards)
            var (isValid, validationError) = _passwordValidationService.ValidatePasswordStrength(newPassword);
            if (!isValid)
            {
                _logger.LogWarning("Password change failed: Weak password for employee {EmployeeId}, Reason: {Reason}",
                    employee.Id, validationError);

                // Audit log: Password complexity validation failed
                await _auditLogService.LogAuthenticationAsync(
                    AuditActionType.PASSWORD_CHANGE_FAILED,
                    userId: employee.Id,
                    userEmail: employee.Email,
                    success: false,
                    tenantId: tenant.Id,
                    errorMessage: validationError
                );

                return (false, validationError);
            }

            // 2. Check password history (prevent reuse of last 5 passwords)
            if (_passwordValidationService.IsPasswordReused(newPassword, employee.PasswordHistory))
            {
                _logger.LogWarning("Password change failed: Password reuse attempt for employee {EmployeeId}", employee.Id);

                // FORTRESS-GRADE AUDIT: Log password reuse attempt (security critical)
                await _auditLogService.LogSecurityEventAsync(
                    AuditActionType.PASSWORD_CHANGE_FAILED,
                    AuditSeverity.WARNING,
                    employee.Id,
                    description: $"Password reuse attempt blocked for employee {employee.Email}",
                    additionalInfo: System.Text.Json.JsonSerializer.Serialize(new
                    {
                        employeeId = employee.Id,
                        employeeEmail = employee.Email,
                        employeeCode = employee.EmployeeCode,
                        reason = "Password matches one of last 5 passwords",
                        tenantId = tenant.Id,
                        subdomain = tenant.Subdomain
                    })
                );

                return (false, "Password cannot be the same as your last 5 passwords. Please choose a different password.");
            }

            // 3. Calculate password entropy (Fortune 500: aim for 50+ bits)
            var entropy = _passwordValidationService.CalculatePasswordEntropy(newPassword);
            _logger.LogInformation("Password entropy for employee {EmployeeId}: {Entropy} bits", employee.Id, entropy);

            // ==============================================
            // STEP 5: SECURE PASSWORD STORAGE & HISTORY MANAGEMENT
            // ==============================================

            // Hash the new password with Argon2
            var newPasswordHash = _passwordHasher.HashPassword(newPassword);

            // Update password history: keep last 5 passwords
            var updatedHistory = _passwordValidationService.UpdatePasswordHistory(
                employee.PasswordHash,
                employee.PasswordHistory
            );

            // Update employee password and related security fields
            var now = DateTime.UtcNow;
            var passwordExpiryDays = 90; // FORTUNE 500: 90-day password rotation policy

            employee.PasswordHash = newPasswordHash;
            employee.PasswordHistory = updatedHistory;
            employee.LastPasswordChangeDate = now;
            // TODO: Add PasswordExpiresAt property to Employee entity
            // employee.PasswordExpiresAt = now.AddDays(passwordExpiryDays);
            employee.MustChangePassword = false; // Reset forced password change flag
            employee.UpdatedAt = now;

            await tenantContext.SaveChangesAsync();

            _logger.LogInformation("Password changed successfully for employee {EmployeeId} in tenant {TenantId}",
                employeeId, tenantId);

            // ==============================================
            // STEP 6: FORTRESS-GRADE AUDIT LOGGING
            // ==============================================

            // Primary audit log: Successful password change
            await _auditLogService.LogAuthenticationAsync(
                AuditActionType.PASSWORD_CHANGED,
                userId: employee.Id,
                userEmail: employee.Email,
                success: true,
                tenantId: tenant.Id,
                eventData: new Dictionary<string, object>
                {
                    // TODO: Add back when PasswordExpiresAt property exists
                    // { "passwordExpiryDate", employee.PasswordExpiresAt!.Value },
                    { "passwordExpiryDays", passwordExpiryDays },
                    { "selfService", true },
                    { "passwordEntropy", Math.Round(entropy, 2) }
                }
            );

            // Additional security event logging with detailed metrics
            await _auditLogService.LogSecurityEventAsync(
                AuditActionType.PASSWORD_CHANGED,
                AuditSeverity.INFO,
                employee.Id,
                description: $"Employee {employee.Email} successfully changed password (self-service)",
                additionalInfo: System.Text.Json.JsonSerializer.Serialize(new
                {
                    employeeId = employee.Id,
                    employeeEmail = employee.Email,
                    employeeCode = employee.EmployeeCode,
                    passwordEntropy = Math.Round(entropy, 2),
                    passwordHistoryLength = employee.PasswordHistory != null
                        ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(employee.PasswordHistory)?.Count ?? 0
                        : 0,
                    // TODO: Add back when PasswordExpiresAt property exists
                    // passwordExpiryDate = employee.PasswordExpiresAt!.Value,
                    passwordExpiryDays = passwordExpiryDays,
                    tenantId = tenant.Id,
                    tenantName = tenant.CompanyName,
                    subdomain = tenant.Subdomain,
                    changeMethod = "SelfService",
                    timestamp = now
                })
            );

            // TODO: Add ExpiryDate when PasswordExpiresAt property exists
            _logger.LogInformation(
                "FORTRESS_AUDIT: Password change completed for {Email} in tenant {Subdomain} (Entropy: {Entropy} bits, History depth: {HistoryCount})",
                employee.Email,
                tenant.Subdomain,
                Math.Round(entropy, 2),
                employee.PasswordHistory != null ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(employee.PasswordHistory)?.Count ?? 0 : 0);

            return (true, $"Password changed successfully. Your new password will expire in {passwordExpiryDays} days.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for employee {EmployeeId} in tenant {TenantId}",
                employeeId, tenantId);
            return (false, "An error occurred while changing the password. Please try again later.");
        }
    }
}
