namespace HRMS.Core.Entities.Master;

/// <summary>
/// Dashboard Statistics Snapshot for Historical Trend Analysis
/// FORTUNE 500 PATTERN: AWS CloudWatch Metrics, Datadog Time Series, New Relic Insights
/// Captures key platform metrics daily for month-over-month trend analysis
/// </summary>
public class DashboardStatisticsSnapshot : BaseEntity
{
    /// <summary>
    /// Snapshot timestamp (typically end of day UTC)
    /// </summary>
    public DateTime SnapshotDate { get; set; }

    /// <summary>
    /// Total number of tenants (all statuses)
    /// </summary>
    public int TotalTenants { get; set; }

    /// <summary>
    /// Number of active tenants
    /// </summary>
    public int ActiveTenants { get; set; }

    /// <summary>
    /// Total employees across all tenants
    /// </summary>
    public int TotalEmployees { get; set; }

    /// <summary>
    /// Monthly recurring revenue (MRR)
    /// </summary>
    public decimal MonthlyRevenue { get; set; }

    /// <summary>
    /// Number of tenants in trial
    /// </summary>
    public int TrialTenants { get; set; }

    /// <summary>
    /// Number of suspended tenants
    /// </summary>
    public int SuspendedTenants { get; set; }

    /// <summary>
    /// Number of expired tenants
    /// </summary>
    public int ExpiredTenants { get; set; }

    /// <summary>
    /// Total storage usage in GB
    /// </summary>
    public decimal TotalStorageGB { get; set; }

    /// <summary>
    /// Average storage usage percentage
    /// </summary>
    public decimal AverageStorageUsagePercent { get; set; }

    /// <summary>
    /// Number of new tenants created this day
    /// </summary>
    public int NewTenantsToday { get; set; }

    /// <summary>
    /// Number of churned tenants this day
    /// </summary>
    public int ChurnedTenantsToday { get; set; }

    /// <summary>
    /// Was this snapshot automatically captured or manually triggered?
    /// </summary>
    public bool IsAutomatic { get; set; } = true;

    /// <summary>
    /// Notes about this snapshot (e.g., "End of month", "After major migration")
    /// </summary>
    public string? Notes { get; set; }
}
