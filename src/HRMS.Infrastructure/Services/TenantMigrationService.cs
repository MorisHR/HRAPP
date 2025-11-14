using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for updating existing tenant schemas with latest migrations
/// Ensures all tenants stay up-to-date with schema changes
/// </summary>
public class TenantMigrationService : ITenantMigrationService
{
    private readonly string _connectionString;
    private readonly ILogger<TenantMigrationService> _logger;

    public TenantMigrationService(
        string connectionString,
        ILogger<TenantMigrationService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    /// <summary>
    /// Applies pending migrations to a specific tenant schema
    /// </summary>
    public async Task<bool> MigrateTenantSchemaAsync(string schemaName)
    {
        try
        {
            _logger.LogInformation("Applying migrations to tenant schema: {SchemaName}", schemaName);

            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseNpgsql(_connectionString)
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

            await using var tenantContext = new TenantDbContext(optionsBuilder.Options, schemaName);

            // Get pending migrations
            var pendingMigrations = await tenantContext.Database.GetPendingMigrationsAsync();
            var pendingCount = pendingMigrations.Count();

            if (pendingCount == 0)
            {
                _logger.LogInformation("No pending migrations for schema: {SchemaName}", schemaName);
                return true;
            }

            _logger.LogInformation("Found {Count} pending migrations for schema: {SchemaName}",
                pendingCount, schemaName);

            foreach (var migration in pendingMigrations)
            {
                _logger.LogInformation("  - {Migration}", migration);
            }

            // Apply migrations
            await tenantContext.Database.MigrateAsync();

            _logger.LogInformation("✅ Successfully applied {Count} migrations to schema: {SchemaName}",
                pendingCount, schemaName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error applying migrations to tenant schema: {SchemaName}", schemaName);
            return false;
        }
    }

    /// <summary>
    /// Applies pending migrations to ALL tenant schemas
    /// </summary>
    public async Task<Dictionary<string, bool>> MigrateAllTenantsAsync()
    {
        var results = new Dictionary<string, bool>();

        try
        {
            _logger.LogInformation("Starting migration for all tenant schemas...");

            // Get all tenant schemas
            var tenantSchemas = await GetAllTenantSchemasAsync();

            _logger.LogInformation("Found {Count} tenant schemas to migrate", tenantSchemas.Count);

            foreach (var schema in tenantSchemas)
            {
                var success = await MigrateTenantSchemaAsync(schema);
                results[schema] = success;
            }

            var successCount = results.Count(r => r.Value);
            var failCount = results.Count(r => !r.Value);

            _logger.LogInformation(
                "Migration complete: {Success} succeeded, {Failed} failed out of {Total} tenants",
                successCount, failCount, results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk tenant migration");
            return results;
        }
    }

    /// <summary>
    /// Gets a list of all tenant schemas from the database
    /// </summary>
    private async Task<List<string>> GetAllTenantSchemasAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT ""SchemaName""
                FROM master.""Tenants""
                WHERE ""IsDeleted"" = false
                AND ""Status"" = 1
                ORDER BY ""SchemaName""";

            await using var command = new NpgsqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            var schemas = new List<string>();
            while (await reader.ReadAsync())
            {
                var schema = reader.GetString(0);
                schemas.Add(schema);
            }

            return schemas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant schemas from database");
            return new List<string>();
        }
    }

    /// <summary>
    /// Gets migration status for a specific tenant
    /// </summary>
    public async Task<TenantMigrationStatus> GetMigrationStatusAsync(string schemaName)
    {
        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseNpgsql(_connectionString);

            await using var tenantContext = new TenantDbContext(optionsBuilder.Options, schemaName);

            var appliedMigrations = await tenantContext.Database.GetAppliedMigrationsAsync();
            var pendingMigrations = await tenantContext.Database.GetPendingMigrationsAsync();

            return new TenantMigrationStatus
            {
                SchemaName = schemaName,
                AppliedMigrations = appliedMigrations.ToList(),
                PendingMigrations = pendingMigrations.ToList(),
                IsUpToDate = !pendingMigrations.Any()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting migration status for schema: {SchemaName}", schemaName);
            return new TenantMigrationStatus
            {
                SchemaName = schemaName,
                AppliedMigrations = new List<string>(),
                PendingMigrations = new List<string>(),
                IsUpToDate = false,
                Error = ex.Message
            };
        }
    }
}
