using Microsoft.Extensions.Logging;
using HRMS.Infrastructure.Services;

/*
 * TENANT MIGRATION CLI UTILITY
 *
 * Applies pending EF Core migrations to tenant schemas.
 *
 * Usage:
 *   dotnet run --project src/HRMS.TenantMigrator                    # Migrate all tenants
 *   dotnet run --project src/HRMS.TenantMigrator tenant_siraaj      # Migrate specific tenant
 *   dotnet run --project src/HRMS.TenantMigrator --status tenant_siraaj  # Check status
 *
 * Environment Variables:
 *   DB_CONNECTION_STRING - PostgreSQL connection string (optional)
 */

namespace HRMS.TenantMigrator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════");
        Console.WriteLine("  HRMS Tenant Migration Utility");
        Console.WriteLine("═══════════════════════════════════════════════════");
        Console.WriteLine();

        // Parse arguments
        bool checkStatusOnly = args.Contains("--status");
        string? targetSchema = args.FirstOrDefault(a => !a.StartsWith("--"));
        bool migrateAll = targetSchema == null;

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<TenantMigrationService>();

        // Get connection string
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Host=localhost;Database=hrms_master;Username=postgres;Password=postgres";

        var migrationService = new TenantMigrationService(connectionString, logger);

        try
        {
            if (checkStatusOnly)
            {
                if (string.IsNullOrEmpty(targetSchema))
                {
                    Console.WriteLine("❌ Error: Schema name required for --status check");
                    Console.WriteLine("Usage: dotnet run --status tenant_siraaj");
                    return 1;
                }

                return await CheckMigrationStatus(migrationService, targetSchema);
            }
            else if (migrateAll)
            {
                return await MigrateAllTenants(migrationService);
            }
            else
            {
                return await MigrateSingleTenant(migrationService, targetSchema!);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ FATAL ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static async Task<int> CheckMigrationStatus(TenantMigrationService service, string schemaName)
    {
        Console.WriteLine($"🔍 Checking migration status for: {schemaName}");
        Console.WriteLine();

        var status = await service.GetMigrationStatusAsync(schemaName);

        if (!string.IsNullOrEmpty(status.Error))
        {
            Console.WriteLine($"❌ Error: {status.Error}");
            return 1;
        }

        Console.WriteLine($"Schema: {status.SchemaName}");
        Console.WriteLine($"Status: {(status.IsUpToDate ? "✅ Up to date" : "⚠️ Pending migrations")}");
        Console.WriteLine();

        if (status.AppliedMigrations.Count > 0)
        {
            Console.WriteLine($"Applied Migrations ({status.AppliedMigrations.Count}):");
            foreach (var migration in status.AppliedMigrations.TakeLast(5))
            {
                Console.WriteLine($"  ✓ {migration}");
            }
            if (status.AppliedMigrations.Count > 5)
            {
                Console.WriteLine($"  ... and {status.AppliedMigrations.Count - 5} more");
            }
            Console.WriteLine();
        }

        if (status.PendingMigrations.Count > 0)
        {
            Console.WriteLine($"Pending Migrations ({status.PendingMigrations.Count}):");
            foreach (var migration in status.PendingMigrations)
            {
                Console.WriteLine($"  ⚠️  {migration}");
            }
            Console.WriteLine();
            Console.WriteLine("Run without --status to apply these migrations.");
        }

        return status.IsUpToDate ? 0 : 1;
    }

    static async Task<int> MigrateSingleTenant(TenantMigrationService service, string schemaName)
    {
        Console.WriteLine($"🔄 Migrating tenant schema: {schemaName}");
        Console.WriteLine();

        // Check status first
        var status = await service.GetMigrationStatusAsync(schemaName);

        if (!string.IsNullOrEmpty(status.Error))
        {
            Console.WriteLine($"❌ Error checking migration status: {status.Error}");
            return 1;
        }

        Console.WriteLine($"Applied Migrations: {status.AppliedMigrations.Count}");
        Console.WriteLine($"Pending Migrations: {status.PendingMigrations.Count}");
        Console.WriteLine();

        if (status.PendingMigrations.Count > 0)
        {
            Console.WriteLine("Pending migrations:");
            foreach (var migration in status.PendingMigrations)
            {
                Console.WriteLine($"  - {migration}");
            }
            Console.WriteLine();

            var success = await service.MigrateTenantSchemaAsync(schemaName);

            if (success)
            {
                Console.WriteLine($"✅ Successfully migrated {schemaName}");
                return 0;
            }
            else
            {
                Console.WriteLine($"❌ Failed to migrate {schemaName}");
                return 1;
            }
        }
        else
        {
            Console.WriteLine("✅ No pending migrations - schema is up to date");
            return 0;
        }
    }

    static async Task<int> MigrateAllTenants(TenantMigrationService service)
    {
        Console.WriteLine("🔄 Migrating ALL tenant schemas...");
        Console.WriteLine();

        var results = await service.MigrateAllTenantsAsync();

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════");
        Console.WriteLine("  Migration Results");
        Console.WriteLine("═══════════════════════════════════════════════════");

        foreach (var (schema, success) in results)
        {
            var icon = success ? "✅" : "❌";
            var status = success ? "SUCCESS" : "FAILED";
            Console.WriteLine($"{icon} {schema,-20} {status}");
        }

        var successCount = results.Count(r => r.Value);
        var failCount = results.Count(r => !r.Value);

        Console.WriteLine();
        Console.WriteLine($"Total: {results.Count} | Success: {successCount} | Failed: {failCount}");

        return failCount > 0 ? 1 : 0;
    }
}
