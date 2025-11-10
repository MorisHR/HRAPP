namespace HRMS.Core.Interfaces;

/// <summary>
/// Interface for accessing current tenant context information
/// Used by controllers and services that need tenant-scoped data
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID from the HTTP request context
    /// Returns null if no tenant context is available (e.g., SuperAdmin endpoints)
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Gets the current tenant schema name
    /// Returns null if no tenant context is available
    /// </summary>
    string? TenantSchema { get; }

    /// <summary>
    /// Gets the current tenant name for display purposes
    /// Returns null if no tenant context is available
    /// </summary>
    string? TenantName { get; }
}
