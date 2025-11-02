namespace HRMS.Core.Settings;

/// <summary>
/// Health check configuration settings
/// </summary>
public class HealthCheckSettings
{
    /// <summary>
    /// Enable health checks
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Show detailed error information in health check responses
    /// (should be false in production)
    /// </summary>
    public bool DetailedErrors { get; set; }

    /// <summary>
    /// Health check timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Database health check timeout in seconds
    /// </summary>
    public int DatabaseTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Redis health check timeout in seconds
    /// </summary>
    public int RedisTimeoutSeconds { get; set; } = 5;
}
