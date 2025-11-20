namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Audit log for department changes
/// Provides comprehensive audit trail for compliance (SOX, GDPR)
/// </summary>
public class DepartmentAuditLog
{
    public Guid Id { get; set; }

    /// <summary>
    /// Department that was modified
    /// </summary>
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Type of activity (Created, Updated, Deleted, Activated, Deactivated, Merged, etc.)
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the change
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Old value (JSON serialized for complex changes)
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value (JSON serialized for complex changes)
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// User ID who performed the action
    /// </summary>
    public string PerformedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the action was performed
    /// </summary>
    public DateTime PerformedAt { get; set; }

    /// <summary>
    /// IP Address of the user (for security audit)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent (browser/client information)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Navigation property to department
    /// </summary>
    public Department? Department { get; set; }
}
