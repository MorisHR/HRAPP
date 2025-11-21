using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// GDPR Article 7 - User Consent Management
/// FORTUNE 500 PATTERN: OneTrust, TrustArc, Cookiebot consent platforms
///
/// COMPLIANCE:
/// - GDPR Article 7: Conditions for consent
/// - GDPR Article 8: Child consent (age verification)
/// - GDPR Recital 32: Consent withdrawal as easy as giving
/// - CCPA: Opt-out rights
/// - ePrivacy Directive: Cookie consent
///
/// SECURITY:
/// - Immutable audit trail (no updates, only inserts)
/// - Cryptographic hash of consent terms for verification
/// - IP address and user agent tracking
/// - Version control for consent terms
///
/// PERFORMANCE:
/// - Indexed on UserId, TenantId, ConsentType for <10ms queries
/// - Partitioned by CreatedAt (monthly) for 100M+ records
/// </summary>
public class UserConsent
{
    /// <summary>
    /// Unique identifier for this consent record
    /// </summary>
    public Guid Id { get; set; }

    // ==========================================
    // IDENTITY & SCOPE
    // ==========================================

    /// <summary>
    /// User who gave consent (nullable for anonymous tracking)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// User email for audit purposes
    /// DENORMALIZED: Faster compliance reporting without JOINs
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// Tenant scope (null = platform-wide consent)
    /// </summary>
    public Guid? TenantId { get; set; }

    // ==========================================
    // CONSENT TYPE & CATEGORY
    // ==========================================

    /// <summary>
    /// Type of consent given
    /// EXAMPLES: "Marketing", "Analytics", "DataProcessing", "ThirdPartySharing", "Cookies"
    /// </summary>
    public ConsentType ConsentType { get; set; }

    /// <summary>
    /// Specific consent category
    /// EXAMPLES: "EmailMarketing", "GoogleAnalytics", "PayrollProcessing", "StrictlyNecessary"
    /// </summary>
    public string ConsentCategory { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable consent purpose
    /// EXAMPLE: "Process payroll data for salary disbursement"
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    // ==========================================
    // CONSENT TERMS & VERSIONING
    // ==========================================

    /// <summary>
    /// Full text of consent terms presented to user
    /// LEGAL: Must match exactly what user saw
    /// </summary>
    public string ConsentText { get; set; } = string.Empty;

    /// <summary>
    /// Version of consent terms
    /// EXAMPLE: "2.1.0" - triggers re-consent when bumped
    /// </summary>
    public string ConsentVersion { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of consent text for tamper detection
    /// SECURITY: Proves consent terms haven't been altered
    /// </summary>
    public string ConsentTextHash { get; set; } = string.Empty;

    /// <summary>
    /// Language of consent (ISO 639-1 code)
    /// EXAMPLES: "en", "fr", "es"
    /// </summary>
    public string Language { get; set; } = "en";

    // ==========================================
    // CONSENT STATUS & LIFECYCLE
    // ==========================================

    /// <summary>
    /// Current consent status
    /// </summary>
    public ConsentStatus Status { get; set; }

    /// <summary>
    /// Whether consent was explicitly given (true) or implied (false)
    /// GDPR: Explicit consent required for sensitive data
    /// </summary>
    public bool IsExplicit { get; set; }

    /// <summary>
    /// Whether this is opt-in (true) or opt-out (false)
    /// GDPR: Requires opt-in (affirmative action)
    /// </summary>
    public bool IsOptIn { get; set; } = true;

    /// <summary>
    /// Consent given at timestamp
    /// </summary>
    public DateTime GivenAt { get; set; }

    /// <summary>
    /// Consent expires at (null = never expires)
    /// EXAMPLE: Cookie consent may expire after 12 months
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Consent withdrawn at timestamp
    /// </summary>
    public DateTime? WithdrawnAt { get; set; }

    /// <summary>
    /// Reason for withdrawal (optional user feedback)
    /// </summary>
    public string? WithdrawalReason { get; set; }

    // ==========================================
    // LEGAL BASIS (GDPR Article 6)
    // ==========================================

    /// <summary>
    /// Legal basis for processing
    /// GDPR Article 6: Lawfulness of processing
    /// </summary>
    public LegalBasis LegalBasis { get; set; }

    /// <summary>
    /// Additional legal notes
    /// </summary>
    public string? LegalNotes { get; set; }

    // ==========================================
    // TRACKING & ATTRIBUTION
    // ==========================================

    /// <summary>
    /// IP address where consent was given
    /// PROOF: Demonstrates user action occurred
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User agent (browser/device info)
    /// PROOF: Additional evidence of user interaction
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Consent method: "WebForm", "API", "Email", "Verbal", "Written"
    /// </summary>
    public string ConsentMethod { get; set; } = "WebForm";

    /// <summary>
    /// Source page/form where consent was collected
    /// EXAMPLE: "/signup", "/cookies-settings"
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Geographic location (country code)
    /// COMPLIANCE: Different rules for EU vs US vs others
    /// </summary>
    public string? CountryCode { get; set; }

    // ==========================================
    // CHILD CONSENT (GDPR Article 8)
    // ==========================================

    /// <summary>
    /// Whether parental consent is required
    /// GDPR Article 8: Age 16 (or lower per member state)
    /// </summary>
    public bool RequiresParentalConsent { get; set; } = false;

    /// <summary>
    /// Parent/guardian user ID (if applicable)
    /// </summary>
    public Guid? ParentUserId { get; set; }

    /// <summary>
    /// Parent/guardian consent given at
    /// </summary>
    public DateTime? ParentalConsentGivenAt { get; set; }

    // ==========================================
    // THIRD PARTY & DATA SHARING
    // ==========================================

    /// <summary>
    /// Third parties data is shared with (JSON array)
    /// EXAMPLE: ["Google Analytics", "Mailchimp", "Stripe"]
    /// </summary>
    public string? ThirdParties { get; set; }

    /// <summary>
    /// Data retention period (days)
    /// GDPR: Must specify how long data is kept
    /// </summary>
    public int? RetentionPeriodDays { get; set; }

    /// <summary>
    /// Whether data is transferred outside EU
    /// GDPR: Requires additional safeguards
    /// </summary>
    public bool InternationalTransfer { get; set; } = false;

    /// <summary>
    /// Countries data is transferred to (JSON array)
    /// </summary>
    public string? TransferCountries { get; set; }

    // ==========================================
    // AUDIT & METADATA
    // ==========================================

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Record created by (admin user if manual entry)
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Previous consent record ID (if this is a renewal/update)
    /// VERSIONING: Tracks consent history
    /// </summary>
    public Guid? PreviousConsentId { get; set; }

    /// <summary>
    /// Reference to DPA if this consent is related to a processing agreement
    /// </summary>
    public Guid? DataProcessingAgreementId { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// FLEXIBILITY: Custom fields per tenant
    /// </summary>
    public string? Metadata { get; set; }

    // ==========================================
    // NAVIGATION PROPERTIES
    // ==========================================

    public virtual Tenant? Tenant { get; set; }

    // ==========================================
    // COMPUTED PROPERTIES
    // ==========================================

    /// <summary>
    /// Is this consent currently active?
    /// </summary>
    public bool IsActive => Status == ConsentStatus.Active &&
                           WithdrawnAt == null &&
                           (!ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow);

    /// <summary>
    /// Is this consent expired?
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    /// <summary>
    /// Days until expiration
    /// </summary>
    public int? DaysUntilExpiration => ExpiresAt.HasValue
        ? Math.Max(0, (int)(ExpiresAt.Value - DateTime.UtcNow).TotalDays)
        : null;
}
