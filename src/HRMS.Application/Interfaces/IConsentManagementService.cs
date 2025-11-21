using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;

namespace HRMS.Application.Interfaces;

/// <summary>
/// GDPR Article 7 - Consent Management Service
/// FORTUNE 500 PATTERN: OneTrust, TrustArc consent platforms
///
/// COMPLIANCE:
/// - GDPR Article 7: Conditions for consent
/// - GDPR Recital 32: Withdrawal as easy as giving consent
/// - CCPA: Consumer rights and opt-out mechanisms
/// - ePrivacy Directive: Cookie consent
///
/// SECURITY:
/// - Immutable audit trail
/// - Cryptographic verification
/// - Rate limiting on consent operations
/// </summary>
public interface IConsentManagementService
{
    // ==========================================
    // CONSENT LIFECYCLE
    // ==========================================

    /// <summary>
    /// Record user consent
    /// SECURITY: IP address and user agent tracking, hash verification
    /// </summary>
    Task<UserConsent> RecordConsentAsync(
        Guid? userId,
        string? userEmail,
        ConsentType consentType,
        string consentCategory,
        string consentText,
        string consentVersion,
        bool isExplicit = true,
        Guid? tenantId = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraw user consent
    /// GDPR: Must be as easy as giving consent
    /// </summary>
    Task<bool> WithdrawConsentAsync(
        Guid consentId,
        string? withdrawalReason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk withdraw all consents for a user
    /// GDPR Article 17: Right to erasure preparation
    /// </summary>
    Task<int> WithdrawAllUserConsentsAsync(
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renew/update consent (creates new record, marks old as superseded)
    /// VERSIONING: Consent terms changed, requires re-consent
    /// </summary>
    Task<UserConsent> RenewConsentAsync(
        Guid previousConsentId,
        string newConsentText,
        string newConsentVersion,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    // ==========================================
    // CONSENT QUERIES
    // ==========================================

    /// <summary>
    /// Get all consents for a user
    /// PERFORMANCE: Indexed by UserId
    /// </summary>
    Task<List<UserConsent>> GetUserConsentsAsync(
        Guid userId,
        bool activeOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active consent for specific type and category
    /// USE CASE: Check if user has consented to marketing emails
    /// </summary>
    Task<UserConsent?> GetActiveConsentAsync(
        Guid userId,
        ConsentType consentType,
        string consentCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has active consent
    /// PERFORMANCE: Optimized boolean check
    /// </summary>
    Task<bool> HasActiveConsentAsync(
        Guid userId,
        ConsentType consentType,
        string consentCategory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get consents expiring soon (within specified days)
    /// AUTOMATION: Send renewal reminders
    /// </summary>
    Task<List<UserConsent>> GetExpiringSoonConsentsAsync(
        int withinDays = 30,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all consents for a tenant
    /// REPORTING: Tenant-level consent analytics
    /// </summary>
    Task<List<UserConsent>> GetTenantConsentsAsync(
        Guid tenantId,
        DateTime? from = null,
        DateTime? to = null,
        ConsentStatus? status = null,
        CancellationToken cancellationToken = default);

    // ==========================================
    // CONSENT ANALYTICS & REPORTING
    // ==========================================

    /// <summary>
    /// Get consent statistics for a tenant
    /// DASHBOARD: Consent adoption rates, withdrawal trends
    /// </summary>
    Task<ConsentStatistics> GetConsentStatisticsAsync(
        Guid? tenantId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get consent compliance report (GDPR Article 7 audit)
    /// AUDIT: Required for data protection authority inspections
    /// </summary>
    Task<ConsentComplianceReport> GenerateComplianceReportAsync(
        DateTime from,
        DateTime to,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    // ==========================================
    // CONSENT VERIFICATION
    // ==========================================

    /// <summary>
    /// Verify consent text hash integrity
    /// SECURITY: Detect tampering with consent records
    /// </summary>
    Task<bool> VerifyConsentIntegrityAsync(
        Guid consentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Audit consent history for a user
    /// COMPLIANCE: Show full consent lifecycle to user/auditor
    /// </summary>
    Task<List<ConsentAuditEntry>> GetConsentAuditTrailAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Consent statistics DTO
/// </summary>
public class ConsentStatistics
{
    public int TotalConsents { get; set; }
    public int ActiveConsents { get; set; }
    public int WithdrawnConsents { get; set; }
    public int ExpiredConsents { get; set; }
    public Dictionary<ConsentType, int> ConsentsByType { get; set; } = new();
    public Dictionary<string, int> ConsentsByCategory { get; set; } = new();
    public decimal WithdrawalRate { get; set; }
    public List<ConsentTrend> Trends { get; set; } = new();
}

/// <summary>
/// Consent trend data point
/// </summary>
public class ConsentTrend
{
    public DateTime Date { get; set; }
    public int ConsentsGiven { get; set; }
    public int ConsentsWithdrawn { get; set; }
    public int NetConsents { get; set; }
}

/// <summary>
/// Consent compliance report
/// </summary>
public class ConsentComplianceReport
{
    public DateTime ReportGeneratedAt { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Guid? TenantId { get; set; }
    public int TotalConsentsRecorded { get; set; }
    public int ExplicitConsentsCount { get; set; }
    public int ImpliedConsentsCount { get; set; }
    public decimal ExplicitConsentRate { get; set; }
    public List<ConsentViolation> PotentialViolations { get; set; } = new();
    public List<ConsentRecommendation> Recommendations { get; set; } = new();
}

/// <summary>
/// Potential consent violation
/// </summary>
public class ConsentViolation
{
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AffectedCount { get; set; }
    public string Severity { get; set; } = "Low";
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Consent recommendation
/// </summary>
public class ConsentRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
}

/// <summary>
/// Consent audit trail entry
/// </summary>
public class ConsentAuditEntry
{
    public Guid ConsentId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty; // "Given", "Withdrawn", "Renewed", "Expired"
    public ConsentType ConsentType { get; set; }
    public string ConsentCategory { get; set; } = string.Empty;
    public string ConsentVersion { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public ConsentStatus Status { get; set; }
}
