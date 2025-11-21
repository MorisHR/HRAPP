namespace HRMS.Core.Enums;

/// <summary>
/// GDPR Consent Types
/// Categories of consent for data processing activities
/// </summary>
public enum ConsentType
{
    /// <summary>Marketing communications (email, SMS, phone)</summary>
    Marketing = 1,

    /// <summary>Analytics and statistics (Google Analytics, usage tracking)</summary>
    Analytics = 2,

    /// <summary>Core data processing (employee records, payroll)</summary>
    DataProcessing = 3,

    /// <summary>Third-party data sharing (integrations, vendors)</summary>
    ThirdPartySharing = 4,

    /// <summary>Cookie usage (strictly necessary, functional, marketing)</summary>
    Cookies = 5,

    /// <summary>Profiling and automated decision-making</summary>
    Profiling = 6,

    /// <summary>Sensitive data processing (health, biometric)</summary>
    SensitiveData = 7,

    /// <summary>International data transfers</summary>
    InternationalTransfer = 8,

    /// <summary>Research and development</summary>
    Research = 9,

    /// <summary>Account creation and authentication</summary>
    AccountManagement = 10
}

/// <summary>
/// Consent Status Lifecycle
/// </summary>
public enum ConsentStatus
{
    /// <summary>Consent is active and valid</summary>
    Active = 1,

    /// <summary>Consent has been withdrawn by user</summary>
    Withdrawn = 2,

    /// <summary>Consent has expired (time-based)</summary>
    Expired = 3,

    /// <summary>Pending user action (not yet confirmed)</summary>
    Pending = 4,

    /// <summary>Consent was rejected/declined by user</summary>
    Declined = 5,

    /// <summary>Superseded by newer consent version</summary>
    Superseded = 6
}

/// <summary>
/// GDPR Article 6 - Legal Basis for Processing
/// </summary>
public enum LegalBasis
{
    /// <summary>Article 6(1)(a) - User has given consent</summary>
    Consent = 1,

    /// <summary>Article 6(1)(b) - Processing necessary for contract performance</summary>
    Contract = 2,

    /// <summary>Article 6(1)(c) - Processing necessary for legal obligation</summary>
    LegalObligation = 3,

    /// <summary>Article 6(1)(d) - Processing necessary to protect vital interests</summary>
    VitalInterests = 4,

    /// <summary>Article 6(1)(e) - Processing necessary for public interest</summary>
    PublicInterest = 5,

    /// <summary>Article 6(1)(f) - Processing necessary for legitimate interests</summary>
    LegitimateInterests = 6
}

/// <summary>
/// Data Processing Agreement Status
/// </summary>
public enum DpaStatus
{
    /// <summary>DPA is in draft, not yet finalized</summary>
    Draft = 1,

    /// <summary>DPA is pending vendor signature</summary>
    PendingSignature = 2,

    /// <summary>DPA is active and valid</summary>
    Active = 3,

    /// <summary>DPA is under review for renewal</summary>
    UnderReview = 4,

    /// <summary>DPA has expired</summary>
    Expired = 5,

    /// <summary>DPA has been terminated</summary>
    Terminated = 6,

    /// <summary>DPA has been suspended (temporary)</summary>
    Suspended = 7
}

/// <summary>
/// Vendor Risk Level (for DPA tracking)
/// </summary>
public enum VendorRiskLevel
{
    /// <summary>Low risk - strictly necessary services</summary>
    Low = 1,

    /// <summary>Medium risk - standard data processing</summary>
    Medium = 2,

    /// <summary>High risk - sensitive data or international transfers</summary>
    High = 3,

    /// <summary>Critical risk - requires enhanced due diligence</summary>
    Critical = 4
}
