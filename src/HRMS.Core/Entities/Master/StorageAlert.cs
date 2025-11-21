using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Storage Quota Alert Entity
/// FORTUNE 500 PATTERN: AWS CloudWatch Alarms, Azure Monitor Alerts, Datadog Monitors
/// COMPLIANCE: Proactive capacity management, SLA adherence
/// BUSINESS VALUE: Prevent service disruptions, optimize costs
/// </summary>
public class StorageAlert : BaseEntity
{
    /// <summary>
    /// Tenant this alert applies to
    /// NULL = Platform-wide alert
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Alert type
    /// VALUES: QUOTA_WARNING, QUOTA_CRITICAL, QUOTA_EXCEEDED, GROWTH_ANOMALY
    /// </summary>
    public StorageAlertType AlertType { get; set; }

    /// <summary>
    /// Alert severity
    /// PATTERN: P0 (Critical), P1 (High), P2 (Medium), P3 (Low)
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Alert title
    /// EXAMPLE: "Storage 90% full for Tenant XYZ"
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Alert description
    /// EXAMPLE: "Current usage: 45GB / 50GB (90%). Approaching quota limit."
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current storage usage in GB
    /// </summary>
    public decimal CurrentUsageGB { get; set; }

    /// <summary>
    /// Storage quota/limit in GB
    /// </summary>
    public decimal QuotaGB { get; set; }

    /// <summary>
    /// Usage percentage (0-100+)
    /// CAN EXCEED 100 if over quota
    /// </summary>
    public decimal UsagePercentage { get; set; }

    /// <summary>
    /// Threshold that triggered this alert
    /// VALUES: 80, 90, 95, 100
    /// </summary>
    public int ThresholdPercentage { get; set; }

    /// <summary>
    /// Alert status
    /// </summary>
    public AlertStatus Status { get; set; } = AlertStatus.ACTIVE;

    /// <summary>
    /// When alert was triggered
    /// INDEXED: For alert history and trending
    /// </summary>
    public DateTime TriggeredAt { get; set; }

    /// <summary>
    /// When alert was acknowledged by admin
    /// SLA: Track response times
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Admin who acknowledged the alert
    /// AUDIT: Track alert handling
    /// </summary>
    public Guid? AcknowledgedBy { get; set; }

    /// <summary>
    /// Acknowledgement notes
    /// EXAMPLE: "Cleaning up old files", "Upgrading tier"
    /// </summary>
    public string? AcknowledgementNotes { get; set; }

    /// <summary>
    /// When alert was resolved (usage back below threshold)
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Resolution method
    /// VALUES: "manual_cleanup", "tier_upgrade", "quota_increase", "auto_cleanup"
    /// </summary>
    public string? ResolutionMethod { get; set; }

    /// <summary>
    /// Time to resolve (TTRR) in minutes
    /// SLA METRIC: How quickly alerts are resolved
    /// </summary>
    public int? TimeToResolveMinutes { get; set; }

    /// <summary>
    /// Was email notification sent?
    /// </summary>
    public bool EmailSent { get; set; } = false;

    /// <summary>
    /// Email sent timestamp
    /// </summary>
    public DateTime? EmailSentAt { get; set; }

    /// <summary>
    /// Email recipients (JSON array of emails)
    /// </summary>
    public string? EmailRecipients { get; set; }

    /// <summary>
    /// Was in-app notification sent?
    /// </summary>
    public bool InAppNotificationSent { get; set; } = false;

    /// <summary>
    /// Recommended actions (JSON array)
    /// EXAMPLE: ["Delete old files", "Archive to cold storage", "Upgrade tier"]
    /// </summary>
    public string? RecommendedActions { get; set; }

    /// <summary>
    /// Predicted days until quota exceeded
    /// ML/ANALYTICS: Based on growth rate
    /// </summary>
    public int? PredictedDaysUntilFull { get; set; }

    /// <summary>
    /// Alert recurrence count
    /// PATTERN: How many times this alert has fired (increments on re-trigger)
    /// </summary>
    public int RecurrenceCount { get; set; } = 1;

    /// <summary>
    /// Last recurrence timestamp
    /// </summary>
    public DateTime? LastRecurrenceAt { get; set; }

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
}

/// <summary>
/// Storage alert types
/// </summary>
public enum StorageAlertType
{
    QUOTA_WARNING,          // 80% threshold
    QUOTA_CRITICAL,         // 90% threshold
    QUOTA_NEAR_LIMIT,       // 95% threshold
    QUOTA_EXCEEDED,         // 100%+ threshold
    GROWTH_ANOMALY,         // Unusual growth spike
    DUPLICATE_FILES,        // Excessive duplicates detected
    ORPHANED_FILES,         // Files without database references
    LARGE_FILE_UPLOADED,    // Single file exceeds threshold
    BACKUP_SIZE_WARNING     // Backup storage growing too fast
}

/// <summary>
/// Alert status
/// </summary>
public enum AlertStatus
{
    ACTIVE,
    ACKNOWLEDGED,
    RESOLVED,
    SNOOZED,
    DISMISSED
}

/// <summary>
/// Alert severity (P0-P3)
/// </summary>
public enum AlertSeverity
{
    P0_CRITICAL,    // Immediate action required
    P1_HIGH,        // Action required within 24h
    P2_MEDIUM,      // Action required within 7d
    P3_LOW          // Informational
}
