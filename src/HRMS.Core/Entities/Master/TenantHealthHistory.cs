using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// TIER 1: Tenant Health History
/// Tracks health score changes over time for predictive analytics and trend analysis
/// PATTERN: Proactive intervention before critical failures (Netflix, Datadog)
/// </summary>
public class TenantHealthHistory : BaseEntity
{
    // Foreign key
    public Guid TenantId { get; set; }

    // Health score tracking
    public decimal HealthScore { get; set; }
    public TenantHealthSeverity Severity { get; set; }
    public DateTime CalculatedAt { get; set; }
    public decimal? ScoreChange { get; set; }
    public decimal? PreviousScore { get; set; }

    // Scoring breakdown (JSON)
    public string HealthScoreFactors { get; set; } = "{}";
    public string? IssuesDetected { get; set; }
    public int CriticalIssueCount { get; set; }
    public int WarningCount { get; set; }
    public string? RecommendedActions { get; set; }

    // Alert tracking
    public bool AlertSent { get; set; }
    public string? AlertType { get; set; }
    public bool AcknowledgedByAdmin { get; set; }
    public DateTime? AcknowledgedAt { get; set; }

    // Auto-remediation tracking
    public bool AutoRemediationAttempted { get; set; }
    public string? AutoRemediationResult { get; set; }

    // Contextual data
    public TenantStatus TenantStatusAtCheck { get; set; }
    public int? ActiveUsersAtCheck { get; set; }
    public int? ApiCallVolume24h { get; set; }
    public decimal? StorageUsagePercent { get; set; }

    // Navigation property
    public virtual Tenant Tenant { get; set; } = null!;
}

/// <summary>
/// Tenant Health Severity Levels
/// </summary>
public enum TenantHealthSeverity
{
    Healthy = 0,
    Warning = 1,
    Critical = 2,
    Emergency = 3
}
