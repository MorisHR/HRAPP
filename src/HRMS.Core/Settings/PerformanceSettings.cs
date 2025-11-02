namespace HRMS.Core.Settings;

/// <summary>
/// Performance optimization settings
/// </summary>
public class PerformanceSettings
{
    /// <summary>
    /// Enable response compression (gzip/brotli)
    /// </summary>
    public bool EnableResponseCompression { get; set; } = true;

    /// <summary>
    /// Enable response caching
    /// </summary>
    public bool EnableResponseCaching { get; set; } = true;

    /// <summary>
    /// Maximum request body size in bytes (default: 10MB)
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 10485760;

    /// <summary>
    /// Database command timeout in seconds
    /// </summary>
    public int DatabaseCommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Database connection pool min size
    /// </summary>
    public int DatabasePoolMinSize { get; set; } = 5;

    /// <summary>
    /// Database connection pool max size
    /// </summary>
    public int DatabasePoolMaxSize { get; set; } = 100;

    /// <summary>
    /// Enable query result caching
    /// </summary>
    public bool EnableQueryCaching { get; set; } = true;

    /// <summary>
    /// Default cache duration in minutes
    /// </summary>
    public int DefaultCacheDurationMinutes { get; set; } = 60;
}
