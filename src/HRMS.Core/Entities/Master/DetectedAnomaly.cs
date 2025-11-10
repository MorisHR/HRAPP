using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Detected security anomaly requiring investigation
/// FORTUNE 500 PATTERN: Splunk Notable Events, AWS Security Hub Findings
/// </summary>
public class DetectedAnomaly
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // Anomaly Details
    public AnomalyType AnomalyType { get; set; }
    public AnomalyRiskLevel RiskLevel { get; set; }
    public AnomalyStatus Status { get; set; }
    public int RiskScore { get; set; } // 0-100

    // Context
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public DateTime DetectedAt { get; set; }

    // Evidence
    public string Description { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty; // JSON
    public string? RelatedAuditLogIds { get; set; } // Comma-separated GUIDs

    // Detection Method
    public string DetectionRule { get; set; } = string.Empty;
    public string? ModelVersion { get; set; }

    // Investigation
    public Guid? InvestigatedBy { get; set; }
    public DateTime? InvestigatedAt { get; set; }
    public string? InvestigationNotes { get; set; }
    public string? Resolution { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Notifications
    public bool NotificationSent { get; set; }
    public DateTime? NotificationSentAt { get; set; }
    public string? NotificationRecipients { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
