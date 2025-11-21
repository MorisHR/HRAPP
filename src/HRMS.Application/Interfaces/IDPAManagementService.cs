using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;

namespace HRMS.Application.Interfaces;

/// <summary>
/// GDPR Article 28 - Data Processing Agreement Management Service
/// FORTUNE 500 PATTERN: OneTrust Vendor Risk Management, ServiceNow VRM
///
/// COMPLIANCE:
/// - GDPR Article 28: Processor contracts and obligations
/// - GDPR Article 46: International transfer safeguards
/// - ISO 27001 A.15: Supplier relationships
/// - SOC 2: Third-party vendor management
///
/// BUSINESS VALUE:
/// - Centralized vendor/sub-processor registry
/// - Automated renewal reminders (90 days before expiry)
/// - Risk-based vendor assessment
/// - Compliance evidence for audits
/// </summary>
public interface IDPAManagementService
{
    // ==========================================
    // DPA LIFECYCLE
    // ==========================================

    /// <summary>
    /// Create new Data Processing Agreement
    /// VALIDATION: Required fields, valid dates, vendor uniqueness
    /// </summary>
    Task<DataProcessingAgreement> CreateDPAAsync(
        DataProcessingAgreement dpa,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing DPA
    /// AUDIT: Full change tracking
    /// </summary>
    Task<DataProcessingAgreement> UpdateDPAAsync(
        Guid dpaId,
        DataProcessingAgreement updatedDpa,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminate DPA early
    /// COMPLIANCE: Document termination reason and data handling
    /// </summary>
    Task<bool> TerminateDPAAsync(
        Guid dpaId,
        string terminationReason,
        Guid terminatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renew DPA (creates new agreement, archives old)
    /// AUTOMATION: Triggered 90 days before expiry
    /// </summary>
    Task<DataProcessingAgreement> RenewDPAAsync(
        Guid existingDpaId,
        DateTime newEffectiveDate,
        DateTime newExpiryDate,
        Guid renewedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve DPA (compliance officer review)
    /// WORKFLOW: Draft → Pending → Approved
    /// </summary>
    Task<bool> ApproveDPAAsync(
        Guid dpaId,
        Guid approvedBy,
        CancellationToken cancellationToken = default);

    // ==========================================
    // DPA QUERIES
    // ==========================================

    /// <summary>
    /// Get DPA by ID
    /// </summary>
    Task<DataProcessingAgreement?> GetDPAByIdAsync(
        Guid dpaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all DPAs for tenant
    /// </summary>
    Task<List<DataProcessingAgreement>> GetTenantDPAsAsync(
        Guid tenantId,
        DpaStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all platform-wide DPAs (null tenantId)
    /// EXAMPLE: AWS, Google Cloud, Stripe
    /// </summary>
    Task<List<DataProcessingAgreement>> GetPlatformDPAsAsync(
        DpaStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get DPAs expiring soon
    /// AUTOMATION: Send renewal reminders
    /// </summary>
    Task<List<DataProcessingAgreement>> GetExpiringDPAsAsync(
        int withinDays = 90,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get DPAs requiring risk reassessment
    /// COMPLIANCE: Annual risk assessment required
    /// </summary>
    Task<List<DataProcessingAgreement>> GetOverdueRiskAssessmentsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get DPAs with overdue audits
    /// </summary>
    Task<List<DataProcessingAgreement>> GetOverdueAuditsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search DPAs by vendor name
    /// </summary>
    Task<List<DataProcessingAgreement>> SearchDPAsByVendorAsync(
        string vendorName,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    // ==========================================
    // RISK ASSESSMENT
    // ==========================================

    /// <summary>
    /// Record vendor risk assessment
    /// COMPLIANCE: SOC 2, ISO 27001 annual review
    /// </summary>
    Task<bool> RecordRiskAssessmentAsync(
        Guid dpaId,
        VendorRiskLevel riskLevel,
        string? assessmentNotes,
        DateTime? nextAssessmentDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get DPAs by risk level
    /// REPORTING: Focus on high/critical risk vendors
    /// </summary>
    Task<List<DataProcessingAgreement>> GetDPAsByRiskLevelAsync(
        VendorRiskLevel riskLevel,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    // ==========================================
    // SUB-PROCESSOR MANAGEMENT
    // ==========================================

    /// <summary>
    /// Add sub-processor to DPA
    /// GDPR Article 28(2): Requires controller authorization
    /// </summary>
    Task<bool> AddSubProcessorAsync(
        Guid dpaId,
        string subProcessorName,
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove sub-processor
    /// </summary>
    Task<bool> RemoveSubProcessorAsync(
        Guid dpaId,
        string subProcessorName,
        CancellationToken cancellationToken = default);

    // ==========================================
    // AUDIT & COMPLIANCE
    // ==========================================

    /// <summary>
    /// Record vendor audit
    /// </summary>
    Task<bool> RecordAuditAsync(
        Guid dpaId,
        DateTime auditDate,
        string? auditFindings,
        DateTime? nextAuditDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get DPA compliance dashboard
    /// METRICS: Total DPAs, expiring, high-risk, overdue assessments
    /// </summary>
    Task<DPAComplianceDashboard> GetComplianceDashboardAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate GDPR Article 30 processor registry
    /// COMPLIANCE: Required by data protection authorities
    /// </summary>
    Task<ProcessorRegistry> GenerateProcessorRegistryAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get vendors with international data transfers
    /// COMPLIANCE: GDPR Chapter V reporting
    /// </summary>
    Task<List<DataProcessingAgreement>> GetInternationalTransferDPAsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DPA Compliance Dashboard
/// </summary>
public class DPAComplianceDashboard
{
    public int TotalDPAs { get; set; }
    public int ActiveDPAs { get; set; }
    public int ExpiringSoon { get; set; }
    public int PendingApproval { get; set; }
    public int OverdueRiskAssessments { get; set; }
    public int OverdueAudits { get; set; }
    public Dictionary<VendorRiskLevel, int> DPAsByRiskLevel { get; set; } = new();
    public Dictionary<string, int> DPAsByCountry { get; set; } = new();
    public int InternationalTransferCount { get; set; }
    public decimal AverageContractValueUsd { get; set; }
    public List<DPAAlert> Alerts { get; set; } = new();
}

/// <summary>
/// DPA Alert
/// </summary>
public class DPAAlert
{
    public Guid DPAId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty; // "Expiring", "OverdueAssessment", "HighRisk"
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public DateTime AlertDate { get; set; }
}

/// <summary>
/// GDPR Article 30 - Processor Registry
/// </summary>
public class ProcessorRegistry
{
    public DateTime GeneratedAt { get; set; }
    public Guid? TenantId { get; set; }
    public List<ProcessorEntry> Processors { get; set; } = new();
}

/// <summary>
/// Processor Registry Entry
/// </summary>
public class ProcessorEntry
{
    public string ProcessorName { get; set; } = string.Empty;
    public string ProcessorType { get; set; } = string.Empty;
    public string ProcessingPurpose { get; set; } = string.Empty;
    public List<string> DataCategories { get; set; } = new();
    public List<string> DataSubjectCategories { get; set; } = new();
    public string RetentionPeriod { get; set; } = string.Empty;
    public bool InternationalTransfer { get; set; }
    public List<string> TransferCountries { get; set; } = new();
    public string TransferMechanism { get; set; } = string.Empty;
    public List<string> SecurityMeasures { get; set; } = new();
    public List<string> SubProcessors { get; set; } = new();
}
