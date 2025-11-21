namespace HRMS.Core.Entities.Master;

/// <summary>
/// Daily Storage Snapshot for Growth Tracking
/// FORTUNE 500 PATTERN: AWS Cost Explorer, Azure Cost Management, Datadog Metrics
/// ANALYTICS: Time-series data for trend analysis, forecasting, capacity planning
/// RETENTION: Keep 90 days of daily snapshots, then monthly aggregates
/// </summary>
public class TenantStorageSnapshot : BaseEntity
{
    /// <summary>
    /// Tenant this snapshot belongs to
    /// INDEXED: Fast queries per tenant
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Snapshot date (UTC, midnight)
    /// INDEXED: Time-series queries
    /// </summary>
    public DateTime SnapshotDate { get; set; }

    /// <summary>
    /// Total storage in GB at snapshot time
    /// </summary>
    public decimal TotalStorageGB { get; set; }

    /// <summary>
    /// Database storage in GB
    /// </summary>
    public decimal DatabaseStorageGB { get; set; }

    /// <summary>
    /// File storage in GB
    /// </summary>
    public decimal FileStorageGB { get; set; }

    /// <summary>
    /// Backup storage in GB
    /// </summary>
    public decimal BackupStorageGB { get; set; }

    /// <summary>
    /// Storage quota at snapshot time
    /// HISTORICAL: Track quota changes over time
    /// </summary>
    public decimal QuotaGB { get; set; }

    /// <summary>
    /// Usage percentage (0-100+)
    /// </summary>
    public decimal UsagePercentage { get; set; }

    /// <summary>
    /// Total file count
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Active file count (not deleted)
    /// </summary>
    public int ActiveFiles { get; set; }

    /// <summary>
    /// Deleted file count (soft deleted, pending permanent deletion)
    /// </summary>
    public int DeletedFiles { get; set; }

    /// <summary>
    /// Duplicate file count
    /// OPTIMIZATION: Track deduplication opportunities
    /// </summary>
    public int DuplicateFiles { get; set; }

    /// <summary>
    /// Storage wasted by duplicates in GB
    /// COST SAVINGS: Potential savings from deduplication
    /// </summary>
    public decimal DuplicateStorageGB { get; set; }

    /// <summary>
    /// Storage growth from previous snapshot in GB
    /// POSITIVE = growth, NEGATIVE = reduction
    /// </summary>
    public decimal GrowthGB { get; set; }

    /// <summary>
    /// Growth percentage from previous snapshot
    /// </summary>
    public decimal GrowthPercentage { get; set; }

    /// <summary>
    /// 7-day rolling average growth rate (GB/day)
    /// FORECASTING: Predict future storage needs
    /// </summary>
    public decimal AvgGrowthRate7Day { get; set; }

    /// <summary>
    /// 30-day rolling average growth rate (GB/day)
    /// </summary>
    public decimal AvgGrowthRate30Day { get; set; }

    /// <summary>
    /// Predicted days until quota exceeded
    /// Based on growth rate
    /// </summary>
    public int? PredictedDaysUntilFull { get; set; }

    /// <summary>
    /// Storage breakdown by module (JSON)
    /// EXAMPLE: {"Employees": 5.2, "Payroll": 12.8, "Documents": 8.5}
    /// </summary>
    public string? StorageByModule { get; set; }

    /// <summary>
    /// Storage breakdown by file type (JSON)
    /// EXAMPLE: {"PDF": 8.5, "Image": 6.2, "CSV": 2.1}
    /// </summary>
    public string? StorageByFileType { get; set; }

    /// <summary>
    /// Top 10 largest files (JSON array)
    /// CLEANUP: Identify space hogs
    /// </summary>
    public string? LargestFiles { get; set; }

    /// <summary>
    /// Monthly cost in USD
    /// BILLING: Track storage costs
    /// </summary>
    public decimal MonthlyCostUSD { get; set; }

    /// <summary>
    /// Cost change from previous snapshot
    /// </summary>
    public decimal CostChangeUSD { get; set; }

    /// <summary>
    /// Snapshot generation duration in milliseconds
    /// PERFORMANCE: Monitor snapshot job performance
    /// </summary>
    public int GenerationDurationMs { get; set; }

    /// <summary>
    /// Data quality score (0-100)
    /// RELIABILITY: Track snapshot accuracy
    /// </summary>
    public int DataQualityScore { get; set; } = 100;

    /// <summary>
    /// Errors encountered during snapshot generation
    /// </summary>
    public string? Errors { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
}
