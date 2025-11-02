using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for creating and managing PostgreSQL tenant schemas
/// </summary>
public class SchemaProvisioningService : ISchemaProvisioningService
{
    private readonly string _connectionString;
    private readonly ILogger<SchemaProvisioningService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SchemaProvisioningService(
        string connectionString,
        ILogger<SchemaProvisioningService> logger,
        IServiceProvider serviceProvider)
    {
        _connectionString = connectionString;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> CreateTenantSchemaAsync(string schemaName)
    {
        try
        {
            _logger.LogInformation("Creating tenant schema: {SchemaName}", schemaName);

            // Step 1: Create the schema if it doesn't exist
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var createSchemaCommand = $"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\"";
            await using (var command = new NpgsqlCommand(createSchemaCommand, connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("Schema {SchemaName} created successfully", schemaName);

            // Step 2: Apply migrations to the new schema
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseNpgsql(_connectionString);

            await using var tenantContext = new TenantDbContext(optionsBuilder.Options, schemaName);

            // Run migrations
            await tenantContext.Database.MigrateAsync();

            _logger.LogInformation("Migrations applied to schema {SchemaName}", schemaName);

            // Step 3: Seed initial data
            await SeedTenantDataAsync(schemaName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant schema: {SchemaName}", schemaName);
            return false;
        }
    }

    public async Task<bool> DropTenantSchemaAsync(string schemaName)
    {
        try
        {
            _logger.LogWarning("Dropping tenant schema (IRREVERSIBLE): {SchemaName}", schemaName);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var dropSchemaCommand = $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE";
            await using (var command = new NpgsqlCommand(dropSchemaCommand, connection))
            {
                await command.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("Schema {SchemaName} dropped successfully", schemaName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dropping tenant schema: {SchemaName}", schemaName);
            return false;
        }
    }

    public async Task<bool> SchemaExistsAsync(string schemaName)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT EXISTS(SELECT 1 FROM information_schema.schemata WHERE schema_name = @schemaName)";
            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("schemaName", schemaName);

            var result = await command.ExecuteScalarAsync();
            return result is bool exists && exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if schema exists: {SchemaName}", schemaName);
            return false;
        }
    }

    public async Task<bool> SeedTenantDataAsync(string schemaName)
    {
        try
        {
            _logger.LogInformation("Seeding initial data for tenant schema: {SchemaName}", schemaName);

            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseNpgsql(_connectionString);

            await using var tenantContext = new TenantDbContext(optionsBuilder.Options, schemaName);

            // Seed default departments
            if (!await tenantContext.Departments.AnyAsync())
            {
                var departments = new[]
                {
                    new Core.Entities.Tenant.Department
                    {
                        Id = Guid.NewGuid(),
                        Name = "Human Resources",
                        Code = "HR",
                        Description = "Human Resources Department",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Core.Entities.Tenant.Department
                    {
                        Id = Guid.NewGuid(),
                        Name = "Finance",
                        Code = "FIN",
                        Description = "Finance Department",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Core.Entities.Tenant.Department
                    {
                        Id = Guid.NewGuid(),
                        Name = "IT",
                        Code = "IT",
                        Description = "Information Technology Department",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await tenantContext.Departments.AddRangeAsync(departments);
                await tenantContext.SaveChangesAsync();

                _logger.LogInformation("Default departments seeded for schema: {SchemaName}", schemaName);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding tenant data for schema: {SchemaName}", schemaName);
            return false;
        }
    }
}
