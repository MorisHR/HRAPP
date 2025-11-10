using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Production-grade security alert entity for real-time threat detection
/// Stores security events that require immediate attention
///
/// CRITICAL FEATURES:
/// - Real-time alerting for CRITICAL/EMERGENCY severity events
/// - Anomaly detection tracking
/// - Alert distribution via Email/SMS/Slack/SIEM
/// - Acknowledgement and resolution workflow
/// - Compliance with SOX, GDPR, ISO 27001, PCI-DSS
/// </summary>
public class SecurityAlert
{
    // ============================================
    // PRIMARY KEY
    // ============================================

    /// <summary>Unique identifier for this security alert</summary>
    public Guid Id { get; set; }

    // ============================================
    // ALERT CLASSIFICATION
    // ============================================

    /// <summary>Alert type (enum: FailedLogin, UnauthorizedAccess, DataExfiltration, etc.)</summary>
    public SecurityAlertType AlertType { get; set; }

    /// <summary>Severity level (CRITICAL, EMERGENCY, HIGH, MEDIUM, LOW)</summary>
    public AuditSeverity Severity { get; set; }

    /// <summary>Alert category for classification</summary>
    public AuditCategory Category { get; set; }

    /// <summary>Alert status (New, Acknowledged, InProgress, Resolved, FalsePositive)</summary>
    public SecurityAlertStatus Status { get; set; }

    // ============================================
    // ALERT DETAILS
    // ============================================

    /// <summary>Alert title/summary</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Detailed description of the alert</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Recommended actions to address the alert</summary>
    public string? RecommendedActions { get; set; }

    /// <summary>Risk score (0-100) calculated by anomaly detection</summary>
    public int RiskScore { get; set; }

    // ============================================
    // RELATED AUDIT LOG
    // ============================================

    /// <summary>Related audit log entry ID</summary>
    public Guid? AuditLogId { get; set; }

    /// <summary>Related audit log action type</summary>
    public AuditActionType? AuditActionType { get; set; }

    // ============================================
    // WHO - User/Target Information
    // ============================================

    /// <summary>Tenant ID (null for platform-level alerts)</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Tenant name for easier querying</summary>
    public string? TenantName { get; set; }

    /// <summary>User ID who triggered the alert</summary>
    public Guid? UserId { get; set; }

    /// <summary>User email address</summary>
    public string? UserEmail { get; set; }

    /// <summary>User full name</summary>
    public string? UserFullName { get; set; }

    /// <summary>User role at time of alert</summary>
    public string? UserRole { get; set; }

    // ============================================
    // WHERE - Location Information
    // ============================================

    /// <summary>IP address associated with the alert</summary>
    public string? IpAddress { get; set; }

    /// <summary>Geolocation information</summary>
    public string? Geolocation { get; set; }

    /// <summary>User agent string</summary>
    public string? UserAgent { get; set; }

    /// <summary>Device information</summary>
    public string? DeviceInfo { get; set; }

    // ============================================
    // WHEN - Timestamp Information
    // ============================================

    /// <summary>When the alert was created (UTC)</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the alert was first detected (UTC)</summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>When the alert was acknowledged (UTC)</summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>When the alert was resolved (UTC)</summary>
    public DateTime? ResolvedAt { get; set; }

    // ============================================
    // WORKFLOW & ASSIGNMENT
    // ============================================

    /// <summary>User ID who acknowledged the alert</summary>
    public Guid? AcknowledgedBy { get; set; }

    /// <summary>User email who acknowledged the alert</summary>
    public string? AcknowledgedByEmail { get; set; }

    /// <summary>User ID who resolved the alert</summary>
    public Guid? ResolvedBy { get; set; }

    /// <summary>User email who resolved the alert</summary>
    public string? ResolvedByEmail { get; set; }

    /// <summary>Resolution notes</summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>Assigned to user ID (for investigation)</summary>
    public Guid? AssignedTo { get; set; }

    /// <summary>Assigned to user email</summary>
    public string? AssignedToEmail { get; set; }

    // ============================================
    // NOTIFICATION TRACKING
    // ============================================

    /// <summary>Whether email notification was sent</summary>
    public bool EmailSent { get; set; }

    /// <summary>Email sent timestamp</summary>
    public DateTime? EmailSentAt { get; set; }

    /// <summary>Email recipients (comma-separated)</summary>
    public string? EmailRecipients { get; set; }

    /// <summary>Whether SMS notification was sent</summary>
    public bool SmsSent { get; set; }

    /// <summary>SMS sent timestamp</summary>
    public DateTime? SmsSentAt { get; set; }

    /// <summary>SMS recipients (comma-separated phone numbers)</summary>
    public string? SmsRecipients { get; set; }

    /// <summary>Whether Slack notification was sent</summary>
    public bool SlackSent { get; set; }

    /// <summary>Slack sent timestamp</summary>
    public DateTime? SlackSentAt { get; set; }

    /// <summary>Slack channel(s) notified</summary>
    public string? SlackChannels { get; set; }

    /// <summary>Whether SIEM notification was sent</summary>
    public bool SiemSent { get; set; }

    /// <summary>SIEM sent timestamp</summary>
    public DateTime? SiemSentAt { get; set; }

    /// <summary>SIEM system name (Splunk, QRadar, Sentinel, etc.)</summary>
    public string? SiemSystem { get; set; }

    // ============================================
    // ANOMALY DETECTION METADATA
    // ============================================

    /// <summary>Anomaly detection rule that triggered this alert (JSON)</summary>
    public string? DetectionRule { get; set; }

    /// <summary>Baseline metrics used for detection (JSON)</summary>
    public string? BaselineMetrics { get; set; }

    /// <summary>Current metrics that triggered the alert (JSON)</summary>
    public string? CurrentMetrics { get; set; }

    /// <summary>Deviation percentage from baseline</summary>
    public decimal? DeviationPercentage { get; set; }

    // ============================================
    // CONTEXT & METADATA
    // ============================================

    /// <summary>Correlation ID for distributed tracing</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Additional metadata in JSON format</summary>
    public string? AdditionalMetadata { get; set; }

    /// <summary>Tags for categorization (comma-separated)</summary>
    public string? Tags { get; set; }

    // ============================================
    // COMPLIANCE & AUDIT
    // ============================================

    /// <summary>Compliance framework(s) related to this alert (SOX, GDPR, PCI-DSS, etc.)</summary>
    public string? ComplianceFrameworks { get; set; }

    /// <summary>Whether this alert requires escalation</summary>
    public bool RequiresEscalation { get; set; }

    /// <summary>Escalated to (email or system)</summary>
    public string? EscalatedTo { get; set; }

    /// <summary>Escalation timestamp</summary>
    public DateTime? EscalatedAt { get; set; }

    // ============================================
    // SOFT DELETE
    // ============================================

    /// <summary>Soft delete flag (for data retention policies)</summary>
    public bool IsDeleted { get; set; }

    /// <summary>When the alert was soft-deleted</summary>
    public DateTime? DeletedAt { get; set; }
}
