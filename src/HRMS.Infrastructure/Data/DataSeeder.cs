using HRMS.Core.Entities.Master;
using HRMS.Core.Interfaces;
using HRMS.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HRMS.Infrastructure.Data;

public class DataSeeder
{
    private readonly MasterDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        MasterDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Seed default Super Admin user
            await SeedDefaultAdminUser();

            // BOOTSTRAP FAILSAFE: Ensure at least one SuperAdmin has FULL_ACCESS
            await EnsureBootstrapSuperAdminAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    private async Task SeedDefaultAdminUser()
    {
        // Check if any admin user exists
        var adminExists = await _context.AdminUsers.AnyAsync();

        if (!adminExists)
        {
            _logger.LogInformation("Creating default Super Admin user...");

            // SECURITY FIX: Assign wildcard (*) permission to bootstrap SuperAdmin
            // This resolves the catch-22 where SuperAdmin couldn't assign permissions without having permissions
            // NOTE: Uses string "*" wildcard (Permissions.ALL), not enum SuperAdminPermission
            var fullAccessPermission = new[] { "*" };  // Wildcard grants ALL permissions
            var permissionsJson = JsonSerializer.Serialize(fullAccessPermission);

            var defaultAdmin = new AdminUser
            {
                Id = Guid.NewGuid(),
                UserName = "Super Admin",
                Email = "admin@hrms.com",
                FirstName = "Super",
                LastName = "Admin",
                PasswordHash = _passwordHasher.HashPassword("Admin@123"),
                IsActive = true,
                IsTwoFactorEnabled = false,
                PhoneNumber = null,
                Permissions = permissionsJson, // CRITICAL: Grant FULL_ACCESS on creation
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedBy = "System"
            };

            _context.AdminUsers.Add(defaultAdmin);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Default Super Admin user created successfully");
            _logger.LogInformation("  Email: admin@hrms.com");
            _logger.LogInformation("  Password: Admin@123");
            _logger.LogInformation("  Permissions: FULL_ACCESS (all operations allowed)");
            _logger.LogWarning("IMPORTANT: Please change the default password after first login!");
        }
        else
        {
            _logger.LogInformation("Admin users already exist. Skipping default admin creation.");
        }
    }

    /// <summary>
    /// BOOTSTRAP FAILSAFE: Ensures at least one SuperAdmin has FULL_ACCESS permission
    /// This prevents the catch-22 where no SuperAdmin can assign permissions
    /// Runs on every application startup to auto-heal permission issues
    /// </summary>
    private async Task EnsureBootstrapSuperAdminAsync()
    {
        try
        {
            // Check if ANY SuperAdmin has FULL_ACCESS permission (wildcard "*")
            var fullAccessPermissionJson = JsonSerializer.Serialize(new[] { "*" });

            var hasFullAccessAdmin = await _context.AdminUsers
                .AnyAsync(u => u.IsActive && u.Permissions != null && u.Permissions.Contains("*"));

            if (!hasFullAccessAdmin)
            {
                // CRITICAL: No SuperAdmin has FULL_ACCESS - this is a system failure state
                _logger.LogWarning("⚠️  BOOTSTRAP FAILSAFE TRIGGERED: No SuperAdmin has FULL_ACCESS permission!");

                // Find the oldest active admin (likely the original bootstrap admin)
                var bootstrapAdmin = await _context.AdminUsers
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.CreatedAt)
                    .FirstOrDefaultAsync();

                if (bootstrapAdmin != null)
                {
                    // Grant FULL_ACCESS to the oldest admin
                    bootstrapAdmin.Permissions = fullAccessPermissionJson;
                    bootstrapAdmin.UpdatedAt = DateTime.UtcNow;
                    bootstrapAdmin.UpdatedBy = "System - Bootstrap Failsafe";

                    await _context.SaveChangesAsync();

                    _logger.LogWarning("✅ BOOTSTRAP FAILSAFE: Granted FULL_ACCESS to SuperAdmin {Email} (ID: {Id})",
                        bootstrapAdmin.Email, bootstrapAdmin.Id);
                    _logger.LogInformation("   This prevents the catch-22 where no admin can assign permissions");
                }
                else
                {
                    _logger.LogError("❌ CRITICAL: No active SuperAdmin found! Cannot apply bootstrap failsafe.");
                }
            }
            else
            {
                _logger.LogInformation("✅ Bootstrap check passed: At least one SuperAdmin has FULL_ACCESS");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bootstrap failsafe check");
            // Don't throw - this is a failsafe, not a critical startup requirement
        }
    }
}
