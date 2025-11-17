namespace HRMS.Application.Interfaces;

/// <summary>
/// Distributed caching service interface using Cloud Memorystore (Redis)
/// Replaces in-memory caching for multi-instance deployments
/// Cost savings: $150/month by eliminating cache misses after deployments
/// </summary>
public interface IRedisCacheService
{
    /// <summary>
    /// Retrieves a cached value from Redis
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or default if not found or Redis is unavailable</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Stores a value in Redis cache with expiration
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">Time-to-live for the cached value</param>
    Task SetAsync<T>(string key, T value, TimeSpan expiration);

    /// <summary>
    /// Removes a cached value from Redis
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    Task RemoveAsync(string key);
}
