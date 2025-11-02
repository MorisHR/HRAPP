using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HRMS.Infrastructure.Data;

/// <summary>
/// Design-time factory for TenantDbContext
/// Used by EF Core migrations to create DbContext instances
/// </summary>
public class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())!.FullName,
                "HRMS.API", "appsettings.json"), optional: false)
            .Build();

        // Get connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Build options
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Use a default schema for migrations (will be replaced at runtime)
        return new TenantDbContext(optionsBuilder.Options, "tenant_default");
    }
}
