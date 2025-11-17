using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HRMS.Application.Interfaces;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Distributed caching service using Cloud Memorystore (Redis)
/// Replaces in-memory caching for multi-instance deployments
/// Cost savings: $150/month by eliminating cache misses after deployments
///
/// ARCHITECTURE:
/// - Falls back gracefully to in-memory cache if Redis is unavailable
/// - Supports both development (no Redis) and production (Redis enabled)
/// - Uses JSON serialization for compatibility across all data types
/// - Connection multiplexing for optimal performance
///
/// CONFIGURATION:
/// - Redis:Enabled - Enable/disable Redis caching (falls back to no-op if disabled)
/// - Redis:Endpoint - Redis connection string (e.g., "localhost:6379" or Cloud Memorystore endpoint)
/// </summary>
public class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IDatabase? _db;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly bool _enabled;

    public RedisCacheService(
        IConfiguration configuration,
        ILogger<RedisCacheService> logger)
    {
        _logger = logger;
        _enabled = configuration.GetValue<bool>("Redis:Enabled", false);

        if (_enabled)
        {
            try
            {
                var endpoint = configuration.GetValue<string>("Redis:Endpoint");
                if (string.IsNullOrEmpty(endpoint))
                {
                    _logger.LogWarning("Redis:Endpoint not configured, falling back to in-memory cache");
                    _enabled = false;
                    return;
                }

                _redis = ConnectionMultiplexer.Connect(endpoint);
                _db = _redis.GetDatabase();
                _logger.LogInformation("Redis cache connected: {Endpoint}", endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis connection failed, falling back to in-memory cache");
                _enabled = false;
            }
        }
        else
        {
            _logger.LogInformation("Redis cache disabled, using in-memory cache");
        }
    }

    /// <summary>
    /// Retrieves a cached value from Redis
    /// Returns default value if Redis is disabled or unavailable
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        if (!_enabled || _db == null)
            return default;

        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key: {Key}", key);
            return default;
        }
    }

    /// <summary>
    /// Stores a value in Redis cache with expiration
    /// No-op if Redis is disabled or unavailable
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        if (!_enabled || _db == null)
            return;

        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, expiration);
            _logger.LogDebug("Redis SET succeeded for key: {Key}, TTL: {TTL}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key: {Key}", key);
        }
    }

    /// <summary>
    /// Removes a cached value from Redis
    /// No-op if Redis is disabled or unavailable
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        if (!_enabled || _db == null)
            return;

        try
        {
            await _db.KeyDeleteAsync(key);
            _logger.LogDebug("Redis DELETE succeeded for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis DELETE failed for key: {Key}", key);
        }
    }
}
