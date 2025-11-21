using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// GDPR Article 28 - Data Processing Agreement (DPA) Management
/// FORTUNE 500 PATTERN: OneTrust Vendorpedia, ServiceNow Vendor Risk, Vanta DPA Management
///
/// COMPLIANCE:
/// - GDPR Article 28: Processor obligations and contracts
/// - GDPR Article 46: Transfers with appropriate safeguards
/// - ISO 27001 A.15: Supplier relationships
/// - SOC 2: Vendor management and due diligence
///
/// BUSINESS VALUE:
/// - Centralized vendor/sub-processor tracking
/// - Automated DPA renewal reminders
/// - Risk assessment and due diligence documentation
/// - GDPR Article 30 records of processing activities
///
/// SECURITY:
/// - Encrypted document storage
/// - Audit trail for all DPA changes
/// - Role-based access control (only compliance officers)
/// </summary>
public class DataProcessingAgreement
{
    /// <summary>
    /// Unique identifier for this DPA
    /// </summary>
    public Guid Id { get; set; }

    // ==========================================
    // VENDOR/PROCESSOR INFORMATION
    // ==========================================

    /// <summary>
    /// Vendor/processor legal entity name
    /// EXAMPLE: "Amazon Web Services, Inc.", "Google LLC"
    /// </summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>
    /// Vendor type: "DataProcessor", "SubProcessor", "JointController"
    /// GDPR: Different obligations for each type
    /// </summary>
    public string VendorType { get; set; } = "DataProcessor";

    /// <summary>
    /// Primary contact name at vendor
    /// </summary>
    public string? VendorContactName { get; set; }

    /// <summary>
    /// Primary contact email at vendor
    /// </summary>
    public string? VendorContactEmail { get; set; }

    /// <summary>
    /// Vendor phone number
    /// </summary>
    public string? VendorPhone { get; set; }

    /// <summary>
    /// Vendor registered address
    /// COMPLIANCE: Required for GDPR Article 28
    /// </summary>
    public string? VendorAddress { get; set; }

    /// <summary>
    /// Vendor country of registration
    /// CRITICAL: Determines if international transfer
    /// </summary>
    public string VendorCountry { get; set; } = string.Empty;

    /// <summary>
    /// Vendor website
    /// </summary>
    public string? VendorWebsite { get; set; }

    /// <summary>
    /// Data protection officer contact at vendor
    /// GDPR Article 37: DPO required for certain processors
    /// </summary>
    public string? DataProtectionOfficer { get; set; }

    // ==========================================
    // DPA DETAILS
    // ==========================================

    /// <summary>
    /// DPA reference number or contract ID
    /// </summary>
    public string DpaReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// DPA status
    /// </summary>
    public DpaStatus Status { get; set; }

    /// <summary>
    /// Date DPA was signed
    /// </summary>
    public DateTime? SignedDate { get; set; }

    /// <summary>
    /// Date DPA becomes effective
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Date DPA expires
    /// AUTOMATION: Trigger renewal reminders 90 days before
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Auto-renewal clause: true = auto-renews, false = manual renewal
    /// </summary>
    public bool IsAutoRenewal { get; set; } = false;

    /// <summary>
    /// Notice period for termination (days)
    /// EXAMPLE: 30, 60, 90 days
    /// </summary>
    public int NoticePeriodDays { get; set; } = 30;

    /// <summary>
    /// Uploaded DPA document path (cloud storage)
    /// SECURITY: Encrypted at rest, access-controlled
    /// </summary>
    public string? DocumentPath { get; set; }

    /// <summary>
    /// Document MIME type
    /// </summary>
    public string? DocumentMimeType { get; set; }

    /// <summary>
    /// Document file size (bytes)
    /// </summary>
    public long? DocumentSizeBytes { get; set; }

    /// <summary>
    /// SHA-256 hash of DPA document for integrity verification
    /// </summary>
    public string? DocumentHash { get; set; }

    // ==========================================
    // PROCESSING SCOPE
    // ==========================================

    /// <summary>
    /// Purpose of data processing
    /// EXAMPLE: "Cloud hosting", "Email marketing", "Payment processing"
    /// </summary>
    public string ProcessingPurpose { get; set; } = string.Empty;

    /// <summary>
    /// Categories of data subjects
    /// EXAMPLE: "Employees", "Customers", "Job Applicants"
    /// JSON array format
    /// </summary>
    public string DataSubjectCategories { get; set; } = "[]";

    /// <summary>
    /// Categories of personal data processed
    /// EXAMPLE: "Contact info", "Financial data", "Health data"
    /// JSON array format
    /// </summary>
    public string PersonalDataCategories { get; set; } = "[]";

    /// <summary>
    /// Special categories of personal data (GDPR Article 9)
    /// EXAMPLES: "Health", "Biometric", "Racial/ethnic origin"
    /// JSON array format
    /// </summary>
    public string? SpecialDataCategories { get; set; }

    /// <summary>
    /// Whether sensitive data is processed
    /// SECURITY: Triggers enhanced security requirements
    /// </summary>
    public bool ProcessesSensitiveData { get; set; } = false;

    /// <summary>
    /// Data retention period (days)
    /// GDPR: Must be specified and enforced
    /// </summary>
    public int RetentionPeriodDays { get; set; }

    /// <summary>
    /// What happens to data after retention period
    /// VALUES: "Delete", "Anonymize", "Archive", "Return"
    /// </summary>
    public string DataDisposalMethod { get; set; } = "Delete";

    // ==========================================
    // INTERNATIONAL TRANSFERS
    // ==========================================

    /// <summary>
    /// Whether data is transferred outside EU/EEA
    /// GDPR Chapter V: Triggers additional safeguards
    /// </summary>
    public bool InternationalDataTransfer { get; set; } = false;

    /// <summary>
    /// Countries data is transferred to (JSON array)
    /// </summary>
    public string? TransferCountries { get; set; }

    /// <summary>
    /// Transfer mechanism: "StandardContractualClauses", "AdequacyDecision", "BCR"
    /// GDPR: Specifies legal basis for transfer
    /// </summary>
    public string? TransferMechanism { get; set; }

    /// <summary>
    /// Date of adequacy decision (if applicable)
    /// EXAMPLE: EU-US Data Privacy Framework
    /// </summary>
    public DateTime? AdequacyDecisionDate { get; set; }

    // ==========================================
    // SECURITY & COMPLIANCE
    // ==========================================

    /// <summary>
    /// Vendor risk assessment level
    /// </summary>
    public VendorRiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Last risk assessment date
    /// BEST PRACTICE: Annual reassessment
    /// </summary>
    public DateTime? LastRiskAssessmentDate { get; set; }

    /// <summary>
    /// Next scheduled risk assessment date
    /// </summary>
    public DateTime? NextRiskAssessmentDate { get; set; }

    /// <summary>
    /// Vendor certifications (JSON array)
    /// EXAMPLES: "ISO 27001", "SOC 2 Type II", "ISO 27701"
    /// </summary>
    public string? Certifications { get; set; }

    /// <summary>
    /// Security measures implemented by processor
    /// GDPR Article 32: Technical and organizational measures
    /// </summary>
    public string? SecurityMeasures { get; set; }

    /// <summary>
    /// Data breach notification timeframe (hours)
    /// GDPR: Must notify controller without undue delay
    /// </summary>
    public int BreachNotificationHours { get; set; } = 24;

    /// <summary>
    /// Whether processor has right to use sub-processors
    /// GDPR Article 28(2): Controller authorization required
    /// </summary>
    public bool AllowsSubProcessors { get; set; } = false;

    /// <summary>
    /// List of authorized sub-processors (JSON array)
    /// </summary>
    public string? AuthorizedSubProcessors { get; set; }

    /// <summary>
    /// Whether processor must obtain prior consent for new sub-processors
    /// </summary>
    public bool RequiresPriorConsentForSubProcessors { get; set; } = true;

    // ==========================================
    // AUDIT RIGHTS
    // ==========================================

    /// <summary>
    /// Controller audit rights: "OnDemand", "Annual", "Quarterly", "None"
    /// </summary>
    public string AuditRights { get; set; } = "Annual";

    /// <summary>
    /// Last audit date
    /// </summary>
    public DateTime? LastAuditDate { get; set; }

    /// <summary>
    /// Next scheduled audit date
    /// </summary>
    public DateTime? NextAuditDate { get; set; }

    /// <summary>
    /// Audit findings/notes
    /// </summary>
    public string? AuditNotes { get; set; }

    // ==========================================
    // FINANCIAL
    // ==========================================

    /// <summary>
    /// Annual contract value (USD)
    /// BUSINESS: Track vendor spending
    /// </summary>
    public decimal? AnnualValueUsd { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Payment terms
    /// </summary>
    public string? PaymentTerms { get; set; }

    // ==========================================
    // LIFECYCLE & METADATA
    // ==========================================

    /// <summary>
    /// Tenant this DPA belongs to (null = platform-wide vendor)
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Created at timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Created by user ID (compliance officer)
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Last updated by user ID
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// DPA review/approval status
    /// VALUES: "Draft", "PendingApproval", "Approved", "Rejected"
    /// </summary>
    public string? ApprovalStatus { get; set; }

    /// <summary>
    /// Approved by user ID (legal/compliance officer)
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>
    /// Approval timestamp
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Internal notes for compliance team
    /// </summary>
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// FLEXIBILITY: Custom fields per organization
    /// </summary>
    public string? Metadata { get; set; }

    // ==========================================
    // TERMINATION
    // ==========================================

    /// <summary>
    /// Termination date (if DPA terminated early)
    /// </summary>
    public DateTime? TerminatedAt { get; set; }

    /// <summary>
    /// Termination reason
    /// </summary>
    public string? TerminationReason { get; set; }

    /// <summary>
    /// Terminated by user ID
    /// </summary>
    public Guid? TerminatedBy { get; set; }

    // ==========================================
    // NAVIGATION PROPERTIES
    // ==========================================

    public virtual Tenant? Tenant { get; set; }

    // ==========================================
    // COMPUTED PROPERTIES
    // ==========================================

    /// <summary>
    /// Is this DPA currently active?
    /// </summary>
    public bool IsActive => Status == DpaStatus.Active &&
                           SignedDate.HasValue &&
                           DateTime.UtcNow >= EffectiveDate &&
                           DateTime.UtcNow < ExpiryDate;

    /// <summary>
    /// Is DPA expiring soon (within 90 days)?
    /// </summary>
    public bool IsExpiringSoon => (ExpiryDate - DateTime.UtcNow).TotalDays <= 90 &&
                                  (ExpiryDate - DateTime.UtcNow).TotalDays > 0;

    /// <summary>
    /// Days until expiration
    /// </summary>
    public int DaysUntilExpiration => Math.Max(0, (int)(ExpiryDate - DateTime.UtcNow).TotalDays);

    /// <summary>
    /// Is risk assessment overdue?
    /// </summary>
    public bool IsRiskAssessmentOverdue => NextRiskAssessmentDate.HasValue &&
                                           NextRiskAssessmentDate.Value < DateTime.UtcNow;

    /// <summary>
    /// Is audit overdue?
    /// </summary>
    public bool IsAuditOverdue => NextAuditDate.HasValue &&
                                  NextAuditDate.Value < DateTime.UtcNow;
}
