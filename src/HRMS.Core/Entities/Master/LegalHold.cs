using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Legal hold entity for litigation and regulatory compliance
/// FORTUNE 500 PATTERN: E-Discovery platforms, litigation hold management
/// </summary>
public class LegalHold
{
    /// <summary>Unique identifier</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant ID (null for cross-tenant holds)</summary>
    public Guid? TenantId { get; set; }

    // Case Information
    /// <summary>Legal case number or reference</summary>
    public string CaseNumber { get; set; } = string.Empty;

    /// <summary>Description of the legal hold</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Reason for the legal hold</summary>
    public string? Reason { get; set; }

    // Date Range
    /// <summary>Legal hold start date</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Legal hold end date (null if indefinite)</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Legal hold status</summary>
    public LegalHoldStatus Status { get; set; }

    // Scope
    /// <summary>User IDs subject to legal hold (JSON array)</summary>
    public string? UserIds { get; set; }

    /// <summary>Entity types subject to legal hold (JSON array: ["Employee", "Payroll", "LeaveRequest"])</summary>
    public string? EntityTypes { get; set; }

    /// <summary>Search keywords for data identification (JSON array)</summary>
    public string? SearchKeywords { get; set; }

    // Requestor Information
    /// <summary>User ID who requested the legal hold</summary>
    public Guid RequestedBy { get; set; }

    /// <summary>Legal representative or attorney name</summary>
    public string? LegalRepresentative { get; set; }

    /// <summary>Legal representative contact email</summary>
    public string? LegalRepresentativeEmail { get; set; }

    /// <summary>Law firm or legal department</summary>
    public string? LawFirm { get; set; }

    /// <summary>Court order reference (if applicable)</summary>
    public string? CourtOrder { get; set; }

    // Release Information
    /// <summary>User ID who released the legal hold</summary>
    public Guid? ReleasedBy { get; set; }

    /// <summary>Legal hold release date</summary>
    public DateTime? ReleasedAt { get; set; }

    /// <summary>Release notes</summary>
    public string? ReleaseNotes { get; set; }

    // Statistics
    /// <summary>Number of audit logs affected by this legal hold</summary>
    public int AffectedAuditLogCount { get; set; }

    /// <summary>Number of entities affected by this legal hold</summary>
    public int AffectedEntityCount { get; set; }

    // Notifications
    /// <summary>Users notified about the legal hold (JSON array)</summary>
    public string? NotifiedUsers { get; set; }

    /// <summary>Notification sent date</summary>
    public DateTime? NotificationSentAt { get; set; }

    // Compliance
    /// <summary>Compliance frameworks applicable (SOX, GDPR, etc.)</summary>
    public string? ComplianceFrameworks { get; set; }

    /// <summary>Retention period in days (overrides normal retention)</summary>
    public int? RetentionPeriodDays { get; set; }

    // Metadata
    /// <summary>Creation timestamp</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Created by user ID</summary>
    public Guid CreatedBy { get; set; }

    /// <summary>Last update timestamp</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Last updated by user ID</summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>Additional metadata (JSON)</summary>
    public string? AdditionalMetadata { get; set; }
}
