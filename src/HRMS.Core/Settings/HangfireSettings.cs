namespace HRMS.Core.Settings;

/// <summary>
/// Hangfire background job configuration
/// </summary>
public class HangfireSettings
{
    /// <summary>
    /// Number of concurrent job workers
    /// </summary>
    public int WorkerCount { get; set; } = 5;

    /// <summary>
    /// Enable Hangfire dashboard
    /// </summary>
    public bool DashboardEnabled { get; set; }

    /// <summary>
    /// Require authentication to access dashboard
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    /// Dashboard path (default: /hangfire)
    /// </summary>
    public string DashboardPath { get; set; } = "/hangfire";

    /// <summary>
    /// Job storage type (PostgreSQL, Redis, Memory)
    /// </summary>
    public string StorageType { get; set; } = "PostgreSQL";

    /// <summary>
    /// Job retention days (how long to keep job history)
    /// </summary>
    public int JobRetentionDays { get; set; } = 30;
}
