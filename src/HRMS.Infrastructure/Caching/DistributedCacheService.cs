using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HRMS.Infrastructure.Caching;

/// <summary>
/// Redis-backed distributed cache service implementation
/// PERFORMANCE: Sub-millisecond cache lookups
/// SCALABILITY: Shared cache across multiple app servers
/// RELIABILITY: Graceful degradation if Redis unavailable
/// </summary>
public class DistributedCacheService : IDistributedCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Default TTL values (in minutes)
    private const int DEFAULT_TTL_MINUTES = 5;
    private const int DASHBOARD_TTL_MINUTES = 2;   // Dashboard data changes frequently
    private const int REPORT_TTL_MINUTES = 15;      // Reports are more static
    private const int LOOKUP_TTL_MINUTES = 60;      // Lookup data (departments, etc.) rarely changes

    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure JSON serialization for cache
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false, // Minimize cache size
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("Cache miss for key: {CacheKey}", key);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
        }
        catch (Exception ex)
        {
            // Graceful degradation - log error but don't fail
            _logger.LogError(ex, "Error reading from cache for key: {CacheKey}. Returning null.", key);
            return null;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        int absoluteExpirationMinutes = DEFAULT_TTL_MINUTES,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var jsonData = JsonSerializer.Serialize(value, _jsonOptions);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(absoluteExpirationMinutes)
            };

            await _cache.SetStringAsync(key, jsonData, options, cancellationToken);

            _logger.LogDebug(
                "Cached data for key: {CacheKey} with TTL: {TTL} minutes",
                key,
                absoluteExpirationMinutes);
        }
        catch (Exception ex)
        {
            // Graceful degradation - log error but don't fail
            _logger.LogError(ex, "Error writing to cache for key: {CacheKey}.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Removed cache key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {CacheKey}.", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: Pattern-based removal requires Redis-specific implementation
        // IDistributedCache doesn't support patterns natively
        // This is a placeholder - implement with StackExchange.Redis if needed
        try
        {
            _logger.LogWarning(
                "Pattern-based cache removal requested for pattern: {Pattern}. " +
                "This requires Redis-specific implementation with StackExchange.Redis. " +
                "Consider implementing IConnectionMultiplexer for pattern support.",
                pattern);

            // For now, log the request
            // Full implementation would use StackExchange.Redis:
            // var keys = server.Keys(pattern: pattern);
            // foreach (var key in keys) await RemoveAsync(key, cancellationToken);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache by pattern: {Pattern}.", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(cachedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {CacheKey}.", key);
            return false;
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        int absoluteExpirationMinutes = DEFAULT_TTL_MINUTES,
        CancellationToken cancellationToken = default) where T : class
    {
        // Try to get from cache
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Cache miss - create value using factory
        _logger.LogDebug("Cache miss for key: {CacheKey}. Creating new value.", key);

        try
        {
            var value = await factory(cancellationToken);

            // Cache the newly created value
            await SetAsync(key, value, absoluteExpirationMinutes, cancellationToken);

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cache factory for key: {CacheKey}.", key);
            throw; // Re-throw factory exceptions
        }
    }

    public async Task<bool> RefreshAsync(
        string key,
        int absoluteExpirationMinutes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current value
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("Cannot refresh non-existent key: {CacheKey}", key);
                return false;
            }

            // Re-set with new TTL
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(absoluteExpirationMinutes)
            };

            await _cache.SetStringAsync(key, cachedData, options, cancellationToken);

            _logger.LogDebug("Refreshed TTL for key: {CacheKey} to {TTL} minutes", key, absoluteExpirationMinutes);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache for key: {CacheKey}.", key);
            return false;
        }
    }
}

/// <summary>
/// Cache key builder for consistent cache key generation
/// BEST PRACTICE: Use structured keys for easy invalidation and debugging
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Dashboard summary cache key for a tenant
    /// Pattern: tenant:{tenantId}:dashboard:summary
    /// TTL: 2 minutes (frequent updates)
    /// </summary>
    public static string DashboardSummary(Guid tenantId) =>
        $"tenant:{tenantId}:dashboard:summary";

    /// <summary>
    /// Monthly payroll summary cache key
    /// Pattern: tenant:{tenantId}:payroll:{year}:{month}
    /// TTL: 15 minutes (payroll doesn't change often once processed)
    /// </summary>
    public static string PayrollSummary(Guid tenantId, int year, int month) =>
        $"tenant:{tenantId}:payroll:{year}:{month}";

    /// <summary>
    /// Monthly attendance report cache key
    /// Pattern: tenant:{tenantId}:attendance:{year}:{month}
    /// TTL: 15 minutes
    /// </summary>
    public static string AttendanceReport(Guid tenantId, int year, int month) =>
        $"tenant:{tenantId}:attendance:{year}:{month}";

    /// <summary>
    /// Leave balance report cache key
    /// Pattern: tenant:{tenantId}:leave-balance:{year}
    /// TTL: 10 minutes (changes with leave applications)
    /// </summary>
    public static string LeaveBalanceReport(Guid tenantId, int year) =>
        $"tenant:{tenantId}:leave-balance:{year}";

    /// <summary>
    /// Headcount report cache key
    /// Pattern: tenant:{tenantId}:headcount
    /// TTL: 60 minutes (changes only with new hires/terminations)
    /// </summary>
    public static string HeadcountReport(Guid tenantId) =>
        $"tenant:{tenantId}:headcount";

    /// <summary>
    /// Department list cache key for a tenant
    /// Pattern: tenant:{tenantId}:departments
    /// TTL: 60 minutes (departments change rarely)
    /// </summary>
    public static string DepartmentList(Guid tenantId) =>
        $"tenant:{tenantId}:departments";

    /// <summary>
    /// Invalidate all cache for a tenant
    /// Pattern: tenant:{tenantId}:*
    /// Use with RemoveByPatternAsync()
    /// </summary>
    public static string TenantPattern(Guid tenantId) =>
        $"tenant:{tenantId}:*";

    /// <summary>
    /// Invalidate all dashboard cache for a tenant
    /// Pattern: tenant:{tenantId}:dashboard:*
    /// </summary>
    public static string DashboardPattern(Guid tenantId) =>
        $"tenant:{tenantId}:dashboard:*";
}
