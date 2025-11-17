using System;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 50: System alert tracking and management
/// Monitors critical conditions, SLA violations, and security incidents
/// COMPLIANCE: ISO 27001 (A.16 - Information security incident management)
/// </summary>
public class AlertDto
{
    /// <summary>
    /// Unique alert identifier
    /// </summary>
    public long AlertId { get; set; }

    /// <summary>
    /// Alert severity level: Critical, High, Medium, Low, Info
    /// CRITICAL: Immediate action required (SLA breach, security incident)
    /// HIGH: Investigation required within 1 hour
    /// MEDIUM: Review within 4 hours
    /// LOW: Review within 24 hours
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Alert type/category: Performance, Security, Availability, Capacity, Compliance
    /// </summary>
    public string AlertType { get; set; } = string.Empty;

    /// <summary>
    /// Alert title/summary for quick identification
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed alert message with context and metrics
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Affected component or service (e.g., "API", "Database", "Tenant: acme")
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Tenant subdomain (if alert is tenant-specific), NULL for system-wide alerts
    /// </summary>
    public string? TenantSubdomain { get; set; }

    /// <summary>
    /// Metric that triggered the alert (e.g., "P95_RESPONSE_TIME", "ERROR_RATE")
    /// </summary>
    public string? TriggerMetric { get; set; }

    /// <summary>
    /// Threshold value that was exceeded
    /// </summary>
    public decimal? Threshold { get; set; }

    /// <summary>
    /// Actual metric value that triggered the alert
    /// </summary>
    public decimal? ActualValue { get; set; }

    /// <summary>
    /// Alert status: Active, Acknowledged, Resolved, Suppressed
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// When the alert was first triggered
    /// </summary>
    public DateTime TriggeredAt { get; set; }

    /// <summary>
    /// When the alert was acknowledged by an admin
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Who acknowledged the alert
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// When the alert was resolved (condition no longer exists)
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Who resolved the alert
    /// </summary>
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Resolution notes and actions taken
    /// </summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Alert duration in seconds (ResolvedAt - TriggeredAt)
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Alert notification channels used (Email, SMS, Slack, PagerDuty)
    /// </summary>
    public string? NotificationChannels { get; set; }

    /// <summary>
    /// Whether alert was successfully delivered via configured channels
    /// </summary>
    public bool IsNotified { get; set; }

    /// <summary>
    /// Number of times this alert has been triggered (for recurring alerts)
    /// </summary>
    public int OccurrenceCount { get; set; } = 1;

    /// <summary>
    /// Link to relevant documentation or runbook for this alert type
    /// </summary>
    public string? RunbookUrl { get; set; }
}
