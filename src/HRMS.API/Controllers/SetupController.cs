using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Core.Entities.Master;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;

namespace HRMS.API.Controllers;

/// <summary>
/// Setup API for first-time system initialization
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
public class SetupController : ControllerBase
{
    private readonly MasterDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<SetupController> _logger;

    public SetupController(
        MasterDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<SetupController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Create the first admin user for system bootstrap
    /// POST /api/admin/setup/create-first-admin
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

            // Create the first admin user
            const string email = "admin@hrms.com";
            const string password = "Admin@123";
            const string firstName = "Super";
            const string lastName = "Admin";

            // Hash the password using Argon2
            var passwordHash = _passwordHasher.HashPassword(password);

            var adminUser = new AdminUser
            {
                Id = Guid.NewGuid(),
                UserName = email.Split('@')[0], // "admin"
                Email = email,
                PasswordHash = passwordHash,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AdminUsers.Add(adminUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("First admin user created successfully with email: {Email}", email);

            return Ok(new
            {
                success = true,
                message = $"Admin user created successfully. Email: {email}, Password: {password}",
                data = new
                {
                    email = email,
                    password = password,
                    firstName = firstName,
                    lastName = lastName,
                    isActive = true,
                    warning = "⚠️ Please change this password after first login!"
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
