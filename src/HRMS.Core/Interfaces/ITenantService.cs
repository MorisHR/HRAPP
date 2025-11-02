namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for resolving and managing tenant context
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Get the current tenant ID from HTTP context
    /// </summary>
    Guid? GetCurrentTenantId();

    /// <summary>
    /// Get the current tenant schema name
    /// </summary>
    string? GetCurrentTenantSchema();

    /// <summary>
    /// Set the current tenant context
    /// </summary>
    void SetTenantContext(Guid tenantId, string schemaName);

    /// <summary>
    /// Get subdomain from host
    /// </summary>
    string? GetSubdomainFromHost(string host);
}
