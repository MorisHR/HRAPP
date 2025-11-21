using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// TIER 1: Impersonation audit trail
/// CRITICAL: Track every superadmin impersonation for security compliance (SOX, GDPR, ISO 27001)
/// FORTUNE 500 PATTERN: Complete audit trail for regulatory compliance
/// </summary>
public class TenantImpersonationLog : BaseEntity
{
    /// <summary>
    /// Tenant being impersonated
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// SuperAdmin user ID who initiated impersonation
    /// </summary>
    public string SuperAdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// SuperAdmin username for quick reference
    /// </summary>
    public string SuperAdminUserName { get; set; } = string.Empty;

    /// <summary>
    /// When impersonation started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When impersonation ended (null if still active)
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Duration in seconds (computed on end)
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Business justification for impersonation
    /// COMPLIANCE: Required for audit trail
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// IP address of superadmin
    /// SECURITY: Track source of impersonation
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User agent of superadmin
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Actions performed during impersonation
    /// CRITICAL: Track what superadmin did for compliance
    /// </summary>
    public string ActionsPerformed { get; set; } = "[]"; // JSON array of ImpersonationAction

    /// <summary>
    /// Was any data modified during impersonation?
    /// CRITICAL: Flag for high-risk impersonations
    /// </summary>
    public bool DataModified { get; set; } = false;

    /// <summary>
    /// Was any data exported during impersonation?
    /// CRITICAL: GDPR compliance tracking
    /// </summary>
    public bool DataExported { get; set; } = false;

    /// <summary>
    /// Detailed activity log
    /// JSON array of timestamped actions for forensics
    /// </summary>
    public string? ActivityLog { get; set; }

    /// <summary>
    /// Risk score of this impersonation session (0-100)
    /// ANALYTICS: ML-based risk scoring for anomaly detection
    /// </summary>
    public int RiskScore { get; set; } = 0;

    /// <summary>
    /// Was this session flagged by security systems?
    /// </summary>
    public bool FlaggedBySecurity { get; set; } = false;

    /// <summary>
    /// Security flag reason
    /// </summary>
    public string? SecurityFlagReason { get; set; }

    /// <summary>
    /// Session ended normally or forced logout?
    /// </summary>
    public bool WasForcedLogout { get; set; } = false;

    // Navigation property
    public virtual Tenant Tenant { get; set; } = null!;

    /// <summary>
    /// Calculate duration if session has ended
    /// </summary>
    public TimeSpan? GetDuration()
    {
        if (!EndedAt.HasValue)
            return null;

        return EndedAt.Value - StartedAt;
    }

    /// <summary>
    /// Is this an active impersonation session?
    /// </summary>
    public bool IsActive()
    {
        return !EndedAt.HasValue;
    }
}
