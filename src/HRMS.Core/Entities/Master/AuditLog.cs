using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Production-grade audit log entity for comprehensive compliance tracking
/// Stored in Master schema with monthly partitioning for performance
///
/// CRITICAL COMPLIANCE REQUIREMENTS:
/// - Immutable (no UPDATE/DELETE allowed after creation)
/// - Supports 10+ year retention
/// - Meets Mauritius Workers' Rights Act, Data Protection Act, MRA Tax requirements
/// - Provides complete audit trail for all system operations
///
/// NOTE: Does NOT inherit from BaseEntity to avoid confusion
/// Audit logs have their own creation tracking (PerformedAt, PerformedBy)
/// and must never be updated or deleted
/// </summary>
public class AuditLog
{
    // ============================================
    // PRIMARY KEY
    // ============================================

    /// <summary>Unique identifier for this audit log entry</summary>
    public Guid Id { get; set; }

    // ============================================
    // WHO - User Information
    // ============================================

    /// <summary>Tenant ID (null for SuperAdmin actions on platform level)</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Tenant name for easier querying and reporting</summary>
    public string? TenantName { get; set; }

    /// <summary>User ID who performed the action</summary>
    public Guid? UserId { get; set; }

    /// <summary>User email address</summary>
    public string? UserEmail { get; set; }

    /// <summary>User full name for audit trail readability</summary>
    public string? UserFullName { get; set; }

    /// <summary>User role at the time of action (SuperAdmin, TenantAdmin, HR, Manager, Employee)</summary>
    public string? UserRole { get; set; }

    /// <summary>Session ID for tracking related actions in a single session</summary>
    public string? SessionId { get; set; }

    // ============================================
    // WHAT - Action Information
    // ============================================

    /// <summary>Standardized action type (enum value)</summary>
    public AuditActionType ActionType { get; set; }

    /// <summary>High-level category for filtering and compliance reporting</summary>
    public AuditCategory Category { get; set; }

    /// <summary>Severity level for prioritization and alerting</summary>
    public AuditSeverity Severity { get; set; }

    /// <summary>Entity type affected (Employee, LeaveRequest, Payroll, Tenant, etc.)</summary>
    public string? EntityType { get; set; }

    /// <summary>Entity ID affected (if applicable)</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Whether the action succeeded</summary>
    public bool Success { get; set; }

    /// <summary>Error message if action failed</summary>
    public string? ErrorMessage { get; set; }

    // ============================================
    // HOW - Change Details
    // ============================================

    /// <summary>Old values before change (JSON format for complex objects)</summary>
    public string? OldValues { get; set; }

    /// <summary>New values after change (JSON format for complex objects)</summary>
    public string? NewValues { get; set; }

    /// <summary>Comma-separated list of changed field names</summary>
    public string? ChangedFields { get; set; }

    /// <summary>User-provided reason for the action (required for sensitive operations)</summary>
    public string? Reason { get; set; }

    /// <summary>Approval reference (if action required approval)</summary>
    public string? ApprovalReference { get; set; }

    // ============================================
    // WHERE - Location Information
    // ============================================

    /// <summary>IP address of the user (IPv4 or IPv6)</summary>
    public string? IpAddress { get; set; }

    /// <summary>Geolocation information (city, country, coordinates)</summary>
    public string? Geolocation { get; set; }

    /// <summary>User agent string (browser/device information)</summary>
    public string? UserAgent { get; set; }

    /// <summary>Parsed device information (mobile, desktop, tablet, OS, browser)</summary>
    public string? DeviceInfo { get; set; }

    /// <summary>Network information (ISP, organization, connection type)</summary>
    public string? NetworkInfo { get; set; }

    // ============================================
    // WHEN - Timestamp Information
    // ============================================

    /// <summary>Timestamp when action was performed (UTC)</summary>
    public DateTime PerformedAt { get; set; }

    /// <summary>Action duration in milliseconds (for performance tracking)</summary>
    public int? DurationMs { get; set; }

    /// <summary>Business date (for actions tied to payroll periods, leave dates, etc.)</summary>
    public DateTime? BusinessDate { get; set; }

    // ============================================
    // WHY - Justification
    // ============================================

    /// <summary>Policy reference that triggered this action</summary>
    public string? PolicyReference { get; set; }

    /// <summary>Link to related documentation or policy document</summary>
    public string? DocumentationLink { get; set; }

    // ============================================
    // CONTEXT - Technical Details
    // ============================================

    /// <summary>HTTP method (GET, POST, PUT, DELETE)</summary>
    public string? HttpMethod { get; set; }

    /// <summary>Request path/endpoint</summary>
    public string? RequestPath { get; set; }

    /// <summary>Query string parameters</summary>
    public string? QueryString { get; set; }

    /// <summary>HTTP response status code</summary>
    public int? ResponseCode { get; set; }

    /// <summary>Correlation ID for distributed tracing</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Parent action ID for tracking multi-step operations</summary>
    public Guid? ParentActionId { get; set; }

    /// <summary>Additional metadata in JSON format (flexible for future extensions)</summary>
    public string? AdditionalMetadata { get; set; }

    // ============================================
    // SECURITY & INTEGRITY
    // ============================================

    /// <summary>SHA256 checksum of critical fields for tamper detection</summary>
    public string? Checksum { get; set; }

    /// <summary>Flag indicating if this entry has been archived to cold storage</summary>
    public bool IsArchived { get; set; }

    /// <summary>Archival date (when moved to cold storage)</summary>
    public DateTime? ArchivedAt { get; set; }

    // ============================================
    // LEGAL HOLD & E-DISCOVERY
    // ============================================

    /// <summary>Flag indicating if this entry is under legal hold</summary>
    public bool IsUnderLegalHold { get; set; }

    /// <summary>Legal hold ID (if applicable)</summary>
    public Guid? LegalHoldId { get; set; }

    // ============================================
    // PARTITIONING METADATA
    // ============================================
    // NOTE: The table will be partitioned by PerformedAt (monthly partitions)
    // No additional fields needed - PostgreSQL handles partitioning automatically
}
