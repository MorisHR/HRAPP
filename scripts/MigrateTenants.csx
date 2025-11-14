#!/usr/bin/env dotnet-script
/*
 * TENANT MIGRATION UTILITY
 *
 * This script applies pending EF Core migrations to all active tenant schemas.
 *
 * Usage:
 *   dotnet script scripts/MigrateTenants.csx
 *   dotnet script scripts/MigrateTenants.csx -- tenant_siraaj
 *   dotnet script scripts/MigrateTenants.csx -- --all
 *
 * Requirements:
 *   - dotnet-script (install: dotnet tool install -g dotnet-script)
 *   - Connection string in appsettings.json or environment variable
 */

#r "nuget: Microsoft.EntityFrameworkCore, 8.0.0"
#r "nuget: Npgsql.EntityFrameworkCore.PostgreSQL, 8.0.0"
#r "nuget: Microsoft.Extensions.Logging, 8.0.0"
#r "nuget: Microsoft.Extensions.Logging.Console, 8.0.0"
#r "../src/HRMS.Infrastructure/bin/Debug/net8.0/HRMS.Infrastructure.dll"
#r "../src/HRMS.Core/bin/Debug/net8.0/HRMS.Core.dll"

using Microsoft.Extensions.Logging;
using HRMS.Infrastructure.Services;

// Parse command line arguments
var args = Args.ToArray();
string? targetSchema = args.Length > 0 ? args[0] : null;
bool migrateAll = targetSchema == "--all" || targetSchema == null;

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

Console.WriteLine("═══════════════════════════════════════════════════");
Console.WriteLine("  HRMS Tenant Migration Utility");
Console.WriteLine("═══════════════════════════════════════════════════");
Console.WriteLine();

var migrationService = new TenantMigrationService(connectionString, logger);

try
{
    if (migrateAll)
    {
        Console.WriteLine("🔄 Migrating ALL tenant schemas...");
        Console.WriteLine();

        var results = await migrationService.MigrateAllTenantsAsync();

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

        Environment.Exit(failCount > 0 ? 1 : 0);
    }
    else
    {
        Console.WriteLine($"🔄 Migrating tenant schema: {targetSchema}");
        Console.WriteLine();

        // Check status first
        var status = await migrationService.GetMigrationStatusAsync(targetSchema!);

        if (!string.IsNullOrEmpty(status.Error))
        {
            Console.WriteLine($"❌ Error checking migration status: {status.Error}");
            Environment.Exit(1);
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

            var success = await migrationService.MigrateTenantSchemaAsync(targetSchema!);

            if (success)
            {
                Console.WriteLine($"✅ Successfully migrated {targetSchema}");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine($"❌ Failed to migrate {targetSchema}");
                Environment.Exit(1);
            }
        }
        else
        {
            Console.WriteLine("✅ No pending migrations - schema is up to date");
            Environment.Exit(0);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ FATAL ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}
