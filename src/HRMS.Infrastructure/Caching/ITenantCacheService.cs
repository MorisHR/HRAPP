namespace HRMS.Infrastructure.Caching;

/// <summary>
/// Tenant-aware distributed cache service
/// Provides per-tenant cache isolation and management
/// </summary>
public interface ITenantCacheService
{
    /// <summary>
    /// Get cached value for tenant
    /// </summary>
    Task<T?> GetAsync<T>(string key, Guid tenantId) where T : class;

    /// <summary>
    /// Set cached value for tenant with expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, Guid tenantId, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Remove cached value for tenant
    /// </summary>
    Task RemoveAsync(string key, Guid tenantId);

    /// <summary>
    /// Remove all cached values matching pattern for tenant
    /// </summary>
    Task RemoveByPatternAsync(string pattern, Guid tenantId);

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key, Guid tenantId);
}
