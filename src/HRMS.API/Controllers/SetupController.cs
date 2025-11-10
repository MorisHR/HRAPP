using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Core.Entities.Master;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;
using HRMS.Application.Interfaces;
using HRMS.Core.Enums;
using System.Security.Cryptography;
using System.Text;

namespace HRMS.API.Controllers;

/// <summary>
/// Setup API for first-time system initialization
/// FORTUNE 500 ENHANCED: Cryptographically secure password generation, audit logging, forced password change
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
public class SetupController : ControllerBase
{
    private readonly MasterDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<SetupController> _logger;
    private readonly IAuditLogService _auditLogService;
    private readonly IConfiguration _configuration;

    public SetupController(
        MasterDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<SetupController> logger,
        IAuditLogService auditLogService,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _auditLogService = auditLogService;
        _configuration = configuration;
    }

    /// <summary>
    /// Create the first admin user for system bootstrap
    /// POST /api/admin/setup/create-first-admin
    /// FORTUNE 500 ENHANCED: Cryptographically secure password, forced change, audit logging
    /// </summary>
    /// <returns>Success message with credentials or error if admin already exists</returns>
    [HttpPost("create-first-admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateFirstAdmin()
    {
        try
        {
            // Check if ANY admin user exists in the master.admin_users table
            var existingAdminCount = await _context.AdminUsers.CountAsync();

            if (existingAdminCount > 0)
            {
                _logger.LogWarning("Attempted to create first admin but {Count} admin(s) already exist", existingAdminCount);

                return Conflict(new
                {
                    success = false,
                    message = "Admin user already exists. Cannot create duplicate."
                });
            }

            // FORTUNE 500: Generate cryptographically secure random password
            const string email = "admin@hrms.com";
            const string firstName = "Super";
            const string lastName = "Admin";
            var password = GenerateSecurePassword(20); // 20 characters with complexity

            // Hash the password using Argon2
            var passwordHash = _passwordHasher.HashPassword(password);

            // FORTUNE 500: Set password rotation and expiry
            var now = DateTime.UtcNow;
            var passwordExpiryDays = _configuration.GetValue<int>("Security:PasswordExpiryDays", 90);

            var adminUser = new AdminUser
            {
                Id = Guid.NewGuid(),
                UserName = email.Split('@')[0], // "admin"
                Email = email,
                PasswordHash = passwordHash,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,

                // FORTUNE 500: Security enhancements
                MustChangePassword = true, // Force password change on first login
                IsInitialSetupAccount = true, // Mark as bootstrap account
                SessionTimeoutMinutes = 15, // Stricter timeout for SuperAdmin
                LastPasswordChangeDate = now,
                PasswordExpiresAt = now.AddDays(passwordExpiryDays),
                LockoutEnabled = true,
                AccessFailedCount = 0
            };

            _context.AdminUsers.Add(adminUser);
            await _context.SaveChangesAsync();

            // FORTUNE 500: Audit log for initial setup (critical operation)
            await _auditLogService.LogSuperAdminActionAsync(
                AuditActionType.SUPERADMIN_CREATED,
                adminUser.Id,
                email,
                description: "BOOTSTRAP: Initial SuperAdmin account created during system setup",
                newValues: System.Text.Json.JsonSerializer.Serialize(new
                {
                    adminUser.Id,
                    adminUser.Email,
                    adminUser.FirstName,
                    adminUser.LastName,
                    adminUser.IsInitialSetupAccount,
                    adminUser.SessionTimeoutMinutes,
                    PasswordExpiryDays = passwordExpiryDays
                }),
                success: true,
                additionalContext: new Dictionary<string, object>
                {
                    { "operationType", "SYSTEM_BOOTSTRAP" },
                    { "mustChangePassword", true },
                    { "sessionTimeoutMinutes", 15 }
                }
            );

            _logger.LogInformation("First admin user created successfully with email: {Email} (password must be changed on first login)", email);

            return Ok(new
            {
                success = true,
                message = $"SuperAdmin account created successfully. IMPORTANT: This password will be shown ONLY ONCE. Save it securely.",
                data = new
                {
                    email = email,
                    temporaryPassword = password, // Shown only once, never logged
                    firstName = firstName,
                    lastName = lastName,
                    isActive = true,
                    mustChangePassword = true,
                    sessionTimeoutMinutes = 15,
                    passwordExpiresInDays = passwordExpiryDays,
                    securityLevel = "FORTUNE_500_COMPLIANT",
                    warning = "🔐 CRITICAL SECURITY NOTICE:",
                    instructions = new[]
                    {
                        "1. Save this password in a secure password manager immediately",
                        "2. This password will NOT be shown again",
                        "3. You MUST change this password on first login",
                        $"4. Password will expire in {passwordExpiryDays} days",
                        "5. Failed login attempts will lock the account",
                        "6. Session timeout is 15 minutes (stricter for SuperAdmin)"
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating first admin user");

            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while creating the admin user",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate cryptographically secure random password
    /// FORTUNE 500 COMPLIANT: 20+ chars, uppercase, lowercase, digits, symbols
    /// </summary>
    private string GenerateSecurePassword(int length = 20)
    {
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Excluding I, O for clarity
        const string lowercase = "abcdefghijkmnpqrstuvwxyz"; // Excluding l, o for clarity
        const string digits = "23456789"; // Excluding 0, 1 for clarity
        const string symbols = "!@#$%^&*-_+="; // Common safe symbols

        var allChars = uppercase + lowercase + digits + symbols;
        var password = new StringBuilder();

        using (var rng = RandomNumberGenerator.Create())
        {
            // Ensure at least one character from each category
            password.Append(uppercase[GetRandomNumber(rng, uppercase.Length)]);
            password.Append(lowercase[GetRandomNumber(rng, lowercase.Length)]);
            password.Append(digits[GetRandomNumber(rng, digits.Length)]);
            password.Append(symbols[GetRandomNumber(rng, symbols.Length)]);

            // Fill remaining characters randomly
            for (int i = 4; i < length; i++)
            {
                password.Append(allChars[GetRandomNumber(rng, allChars.Length)]);
            }

            // Shuffle the password to avoid predictable patterns
            return new string(password.ToString().OrderBy(x => GetRandomNumber(rng, int.MaxValue)).ToArray());
        }
    }

    /// <summary>
    /// Get cryptographically secure random number
    /// </summary>
    private int GetRandomNumber(RandomNumberGenerator rng, int max)
    {
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        return (int)(BitConverter.ToUInt32(bytes, 0) % (uint)max);
    }

    /// <summary>
    /// Check system setup status
    /// GET /api/admin/setup/status
    /// </summary>
    /// <returns>System setup status information</returns>
    [HttpGet("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSetupStatus()
    {
        try
        {
            var adminCount = await _context.AdminUsers.CountAsync();
            var isSetupComplete = adminCount > 0;

            return Ok(new
            {
                success = true,
                data = new
                {
                    isSetupComplete = isSetupComplete,
                    adminUserCount = adminCount,
                    message = isSetupComplete
                        ? "System is set up. Admin users exist."
                        : "System needs initialization. No admin users found."
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking setup status");

            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while checking setup status",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Reset system (DANGER: Deletes all admin users)
    /// DELETE /api/admin/setup/reset
    /// SECURITY: Requires SuperAdmin role AND Development environment
    /// </summary>
    [HttpDelete("reset")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetSystem()
    {
        try
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (environment != "Development")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Reset is only allowed in Development environment"
                });
            }

            var adminUsers = await _context.AdminUsers.ToListAsync();
            _context.AdminUsers.RemoveRange(adminUsers);
            await _context.SaveChangesAsync();

            _logger.LogWarning("System reset: All admin users deleted (Count: {Count})", adminUsers.Count);

            return Ok(new
            {
                success = true,
                message = $"System reset successfully. Deleted {adminUsers.Count} admin user(s).",
                deletedCount = adminUsers.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting system");

            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while resetting the system",
                error = ex.Message
            });
        }
    }
}
