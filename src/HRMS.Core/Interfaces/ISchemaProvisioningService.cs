namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for creating and managing tenant database schemas
/// </summary>
public interface ISchemaProvisioningService
{
    /// <summary>
    /// Create a new schema for a tenant and run all migrations
    /// </summary>
    Task<bool> CreateTenantSchemaAsync(string schemaName);

    /// <summary>
    /// Drop a tenant schema (hard delete - IRREVERSIBLE)
    /// </summary>
    Task<bool> DropTenantSchemaAsync(string schemaName);

    /// <summary>
    /// Check if a schema exists
    /// </summary>
    Task<bool> SchemaExistsAsync(string schemaName);

    /// <summary>
    /// Seed initial data for a new tenant
    /// </summary>
    Task<bool> SeedTenantDataAsync(string schemaName);
}
