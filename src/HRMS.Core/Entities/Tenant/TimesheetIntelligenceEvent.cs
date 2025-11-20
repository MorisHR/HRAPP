using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Audit log for all intelligent timesheet decisions
/// Tracks AI suggestions, anomaly detections, auto-approvals, etc.
/// Used for compliance, debugging, and ML improvement
/// </summary>
public class TimesheetIntelligenceEvent : BaseEntity
{
    /// <summary>
    /// Employee ID (if applicable)
    /// </summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>
    /// Timesheet ID (if applicable)
    /// </summary>
    public Guid? TimesheetId { get; set; }

    /// <summary>
    /// Timesheet Entry ID (if applicable)
    /// </summary>
    public Guid? TimesheetEntryId { get; set; }

    /// <summary>
    /// Project ID (if applicable)
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Event type:
    /// "SuggestionGenerated", "SuggestionAccepted", "SuggestionRejected",
    /// "AnomalyDetected", "AutoApproved", "AutoRejected", "PatternLearned",
    /// "ComplianceViolation", "FraudAlert"
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event severity: "Info", "Warning", "Error", "Critical"
    /// </summary>
    public string Severity { get; set; } = "Info";

    /// <summary>
    /// Event timestamp
    /// </summary>
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Event description (human-readable)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Detailed event data (JSON)
    /// </summary>
    public string? EventData { get; set; }

    /// <summary>
    /// AI/ML model used (if applicable)
    /// E.g., "PatternMatcher-v1.2", "AnomalyDetector-v2.0"
    /// </summary>
    public string? ModelUsed { get; set; }

    /// <summary>
    /// Confidence score of the decision (0-100)
    /// </summary>
    public int? ConfidenceScore { get; set; }

    /// <summary>
    /// Action taken automatically (if any)
    /// </summary>
    public string? AutomatedAction { get; set; }

    /// <summary>
    /// Was manual intervention required?
    /// </summary>
    public bool RequiredManualIntervention { get; set; } = false;

    /// <summary>
    /// Who resolved this (if manual intervention was needed)?
    /// </summary>
    public Guid? ResolvedBy { get; set; }

    /// <summary>
    /// When was this resolved?
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Resolution notes
    /// </summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Was the AI decision correct?
    /// Used for model training and improvement
    /// </summary>
    public bool? WasDecisionCorrect { get; set; }

    /// <summary>
    /// Feedback for ML improvement
    /// </summary>
    public string? FeedbackNotes { get; set; }

    /// <summary>
    /// Tenant isolation
    /// </summary>
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual Timesheet? Timesheet { get; set; }
    public virtual TimesheetEntry? TimesheetEntry { get; set; }
    public virtual Project? Project { get; set; }

    /// <summary>
    /// Create an info event
    /// </summary>
    public static TimesheetIntelligenceEvent CreateInfo(
        string eventType,
        string description,
        Guid tenantId,
        Guid? employeeId = null,
        object? eventData = null)
    {
        return new TimesheetIntelligenceEvent
        {
            EventType = eventType,
            Severity = "Info",
            Description = description,
            TenantId = tenantId,
            EmployeeId = employeeId,
            EventData = eventData != null ? System.Text.Json.JsonSerializer.Serialize(eventData) : null
        };
    }

    /// <summary>
    /// Create a warning event
    /// </summary>
    public static TimesheetIntelligenceEvent CreateWarning(
        string eventType,
        string description,
        Guid tenantId,
        Guid? employeeId = null,
        object? eventData = null)
    {
        return new TimesheetIntelligenceEvent
        {
            EventType = eventType,
            Severity = "Warning",
            Description = description,
            TenantId = tenantId,
            EmployeeId = employeeId,
            EventData = eventData != null ? System.Text.Json.JsonSerializer.Serialize(eventData) : null,
            RequiredManualIntervention = true
        };
    }

    /// <summary>
    /// Create a critical event
    /// </summary>
    public static TimesheetIntelligenceEvent CreateCritical(
        string eventType,
        string description,
        Guid tenantId,
        Guid? employeeId = null,
        object? eventData = null)
    {
        return new TimesheetIntelligenceEvent
        {
            EventType = eventType,
            Severity = "Critical",
            Description = description,
            TenantId = tenantId,
            EmployeeId = employeeId,
            EventData = eventData != null ? System.Text.Json.JsonSerializer.Serialize(eventData) : null,
            RequiredManualIntervention = true
        };
    }
}
