namespace HRMS.Core.Settings;

/// <summary>
/// Redis cache configuration settings
/// </summary>
public class RedisSettings
{
    /// <summary>
    /// Redis connection string
    /// Format: "host:port,password=xxx,ssl=true,abortConnect=false"
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Instance name prefix for cache keys
    /// </summary>
    public string InstanceName { get; set; } = "HRMS_";

    /// <summary>
    /// Default cache expiration time in minutes
    /// </summary>
    public int DefaultCacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Enable Redis connection pooling
    /// </summary>
    public bool EnableConnectionPooling { get; set; } = true;

    /// <summary>
    /// Connection timeout in milliseconds
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Sync timeout in milliseconds
    /// </summary>
    public int SyncTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Abort connection on failure (should be false in production for resilience)
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;

    /// <summary>
    /// Enable SSL/TLS for Redis connection (required for Google Cloud Memorystore)
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Database number to use (0-15)
    /// </summary>
    public int Database { get; set; } = 0;
}
