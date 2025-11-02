using HRMS.Core.Entities.Master;
using HRMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
            _logger.LogWarning("IMPORTANT: Please change the default password after first login!");
        }
        else
        {
            _logger.LogInformation("Admin users already exist. Skipping default admin creation.");
        }
    }
}
