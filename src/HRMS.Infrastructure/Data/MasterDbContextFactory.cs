using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace HRMS.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core migrations
/// This allows migrations to run without full application startup
/// </summary>
public class MasterDbContextFactory : IDesignTimeDbContextFactory<MasterDbContext>
{
    public MasterDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MasterDbContext>();

        // Use environment variable or default connection string for migrations
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Host=localhost;Database=hrms_master;Username=postgres;Password=postgres;SSL Mode=Prefer";

        optionsBuilder.UseNpgsql(connectionString, b =>
        {
            b.MigrationsHistoryTable("__EFMigrationsHistory", "master");
        });

        // Enable sensitive data logging for migrations (helps with debugging)
        optionsBuilder.EnableSensitiveDataLogging();

        return new MasterDbContext(optionsBuilder.Options);
    }
}
