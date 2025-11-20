using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HRMS.Infrastructure.Caching;

/// <summary>
/// Production-grade tenant-aware caching implementation
/// Provides distributed caching with tenant isolation
/// Supports Redis (production) and Memory (development)
/// </summary>
public class TenantCacheService : ITenantCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<TenantCacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);

    public TenantCacheService(
        IDistributedCache cache,
        ILogger<TenantCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, Guid tenantId) where T : class
    {
        var tenantKey = GetTenantKey(key, tenantId);

        try
        {
            var cached = await _cache.GetStringAsync(tenantKey);
            if (cached == null)
            {
                _logger.LogDebug("Cache miss for key {Key}", tenantKey);
                return null;
            }

            var value = JsonSerializer.Deserialize<T>(cached);
            _logger.LogDebug("Cache hit for key {Key}", tenantKey);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from cache: {Key}", tenantKey);
            return null; // Graceful degradation
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        Guid tenantId,
        TimeSpan? expiration = null) where T : class
    {
        var tenantKey = GetTenantKey(key, tenantId);

        try
        {
            var serialized = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultExpiration
            };

            await _cache.SetStringAsync(tenantKey, serialized, options);
            _logger.LogDebug(
                "Cached value for key {Key} with expiration {Expiration}",
                tenantKey,
                expiration ?? DefaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache: {Key}", tenantKey);
            // Don't throw - caching failures shouldn't break the application
        }
    }

    public async Task RemoveAsync(string key, Guid tenantId)
    {
        var tenantKey = GetTenantKey(key, tenantId);

        try
        {
            await _cache.RemoveAsync(tenantKey);
            _logger.LogDebug("Removed cache key {Key}", tenantKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cache: {Key}", tenantKey);
        }
    }

    public Task RemoveByPatternAsync(string pattern, Guid tenantId)
    {
        // Note: Pattern-based removal requires Redis-specific implementation
        // For now, individual keys must be removed explicitly
        _logger.LogWarning(
            "Pattern-based cache removal not implemented. Pattern: {Pattern}",
            pattern);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, Guid tenantId)
    {
        var tenantKey = GetTenantKey(key, tenantId);

        try
        {
            var value = await _cache.GetStringAsync(tenantKey);
            return value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence: {Key}", tenantKey);
            return false;
        }
    }

    private static string GetTenantKey(string key, Guid tenantId)
    {
        return $"tenant:{tenantId}:{key}";
    }
}
