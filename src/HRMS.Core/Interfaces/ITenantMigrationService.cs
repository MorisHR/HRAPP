namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for managing database migrations across tenant schemas
/// Ensures all tenants stay up-to-date with latest schema changes
/// </summary>
public interface ITenantMigrationService
{
    /// <summary>
    /// Applies pending migrations to a specific tenant schema
    /// </summary>
    /// <param name="schemaName">The tenant schema name (e.g., "tenant_siraaj")</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> MigrateTenantSchemaAsync(string schemaName);

    /// <summary>
    /// Applies pending migrations to ALL active tenant schemas
    /// </summary>
    /// <returns>Dictionary of schema names and their migration results</returns>
    Task<Dictionary<string, bool>> MigrateAllTenantsAsync();

    /// <summary>
    /// Gets the current migration status for a specific tenant
    /// </summary>
    /// <param name="schemaName">The tenant schema name</param>
    /// <returns>Migration status including applied and pending migrations</returns>
    Task<TenantMigrationStatus> GetMigrationStatusAsync(string schemaName);
}

/// <summary>
/// Represents the migration status of a tenant schema
/// </summary>
public class TenantMigrationStatus
{
    public string SchemaName { get; set; } = string.Empty;
    public List<string> AppliedMigrations { get; set; } = new();
    public List<string> PendingMigrations { get; set; } = new();
    public bool IsUpToDate { get; set; }
    public string? Error { get; set; }
}
