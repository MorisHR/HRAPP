using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;

namespace HRMS.API.Controllers;

/// <summary>
/// GDPR Article 28 - Data Processing Agreement (DPA) Management API
/// FORTUNE 500 PATTERN: OneTrust Vendor Risk Management, ServiceNow VRM
///
/// COMPLIANCE:
/// - GDPR Article 28: Processor contracts and obligations
/// - GDPR Article 46: International transfer safeguards
/// - ISO 27001 A.15: Supplier relationships
/// - SOC 2 Type II: Third-party vendor management
///
/// SECURITY:
/// - Role-based authorization (SuperAdmin, ComplianceOfficer, TenantAdmin)
/// - Tenant isolation (except SuperAdmin can see all)
/// - Audit trail for all DPA modifications
///
/// ENDPOINTS:
/// - POST /api/dpa - Create new DPA
/// - PUT /api/dpa/{id} - Update DPA
/// - DELETE /api/dpa/{id} - Terminate DPA
/// - POST /api/dpa/{id}/renew - Renew DPA
/// - POST /api/dpa/{id}/approve - Approve DPA
/// - GET /api/dpa/{id} - Get DPA by ID
/// - GET /api/dpa/tenant/{tenantId} - Get tenant DPAs
/// - GET /api/dpa/platform - Get platform-wide DPAs
/// - GET /api/dpa/expiring - Get expiring DPAs
/// - POST /api/dpa/{id}/risk-assessment - Record risk assessment
/// - POST /api/dpa/{id}/sub-processor - Add sub-processor
/// - GET /api/dpa/dashboard - Get compliance dashboard
/// - GET /api/dpa/processor-registry - Generate processor registry
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,ComplianceOfficer,TenantAdmin")]
public class DPAController : ControllerBase
{
    private readonly IDPAManagementService _dpaService;
    private readonly ILogger<DPAController> _logger;

    public DPAController(
        IDPAManagementService dpaService,
        ILogger<DPAController> logger)
    {
        _dpaService = dpaService;
        _logger = logger;
    }

    // ==========================================
    // DPA LIFECYCLE ENDPOINTS
    // ==========================================

    /// <summary>
    /// Create new Data Processing Agreement
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DataProcessingAgreement), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDPA(
        [FromBody] CreateDPARequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpa = new DataProcessingAgreement
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                VendorName = request.VendorName,
                VendorType = request.VendorType ?? "DataProcessor",
                VendorContactName = request.VendorContactName,
                VendorContactEmail = request.VendorContactEmail,
                VendorCountry = request.VendorCountry ?? string.Empty,
                VendorAddress = request.VendorAddress,
                ProcessingPurpose = request.ProcessingPurpose,
                PersonalDataCategories = System.Text.Json.JsonSerializer.Serialize(request.DataCategories ?? new List<string>()),
                DataSubjectCategories = System.Text.Json.JsonSerializer.Serialize(request.DataSubjectCategories ?? new List<string>()),
                RetentionPeriodDays = request.RetentionPeriodDays,
                SecurityMeasures = request.SecurityMeasures,
                EffectiveDate = request.EffectiveDate,
                ExpiryDate = request.ExpiryDate,
                AnnualValueUsd = request.AnnualValueUsd,
                Status = DpaStatus.Draft,
                RiskLevel = request.RiskLevel ?? VendorRiskLevel.Medium,
                InternationalDataTransfer = request.InternationalDataTransfer,
                TransferCountries = System.Text.Json.JsonSerializer.Serialize(request.TransferCountries ?? new List<string>()),
                TransferMechanism = request.TransferMechanism,
                AllowsSubProcessors = request.AllowsSubProcessors,
                AuthorizedSubProcessors = System.Text.Json.JsonSerializer.Serialize(request.SubProcessorsList ?? new List<string>()),
                AuditRights = request.AuditRights ?? "Annual",
                NextAuditDate = request.NextAuditDate,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString()),
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _dpaService.CreateDPAAsync(dpa, cancellationToken);

            _logger.LogInformation("Created DPA {DPAId} for vendor {VendorName}", created.Id, created.VendorName);

            return CreatedAtAction(nameof(GetDPAById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create DPA");
            return StatusCode(500, new { error = "Failed to create DPA", details = ex.Message });
        }
    }

    /// <summary>
    /// Update existing DPA
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DataProcessingAgreement), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDPA(
        Guid id,
        [FromBody] UpdateDPARequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _dpaService.GetDPAByIdAsync(id, cancellationToken);
            if (existing == null)
            {
                return NotFound(new { error = "DPA not found" });
            }

            // Update properties
            if (request.VendorName != null) existing.VendorName = request.VendorName;
            if (request.VendorType != null) existing.VendorType = request.VendorType;
            if (request.VendorContactName != null) existing.VendorContactName = request.VendorContactName;
            if (request.VendorContactEmail != null) existing.VendorContactEmail = request.VendorContactEmail;
            if (request.VendorCountry != null) existing.VendorCountry = request.VendorCountry;
            if (request.VendorAddress != null) existing.VendorAddress = request.VendorAddress;
            if (request.ProcessingPurpose != null) existing.ProcessingPurpose = request.ProcessingPurpose;
            if (request.DataCategories != null) existing.PersonalDataCategories = System.Text.Json.JsonSerializer.Serialize(request.DataCategories);
            if (request.DataSubjectCategories != null) existing.DataSubjectCategories = System.Text.Json.JsonSerializer.Serialize(request.DataSubjectCategories);
            if (request.RetentionPeriodDays.HasValue) existing.RetentionPeriodDays = request.RetentionPeriodDays.Value;
            if (request.SecurityMeasures != null) existing.SecurityMeasures = request.SecurityMeasures;
            if (request.EffectiveDate.HasValue) existing.EffectiveDate = request.EffectiveDate.Value;
            if (request.ExpiryDate.HasValue) existing.ExpiryDate = request.ExpiryDate.Value;
            if (request.AnnualValueUsd.HasValue) existing.AnnualValueUsd = request.AnnualValueUsd;
            if (request.RiskLevel.HasValue) existing.RiskLevel = request.RiskLevel.Value;
            if (request.InternationalDataTransfer.HasValue) existing.InternationalDataTransfer = request.InternationalDataTransfer.Value;
            if (request.TransferCountries != null) existing.TransferCountries = System.Text.Json.JsonSerializer.Serialize(request.TransferCountries);
            if (request.TransferMechanism != null) existing.TransferMechanism = request.TransferMechanism;
            if (request.AllowsSubProcessors.HasValue) existing.AllowsSubProcessors = request.AllowsSubProcessors.Value;
            if (request.SubProcessorsList != null) existing.AuthorizedSubProcessors = System.Text.Json.JsonSerializer.Serialize(request.SubProcessorsList);
            if (request.AuditRights != null) existing.AuditRights = request.AuditRights;

            var updated = await _dpaService.UpdateDPAAsync(id, existing, cancellationToken);

            _logger.LogInformation("Updated DPA {DPAId}", id);

            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update DPA {DPAId}", id);
            return StatusCode(500, new { error = "Failed to update DPA", details = ex.Message });
        }
    }

    /// <summary>
    /// Terminate DPA early (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TerminateDPA(
        Guid id,
        [FromBody] TerminateDPARequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _dpaService.TerminateDPAAsync(
                id,
                request.TerminationReason,
                request.TerminatedBy,
                cancellationToken);

            if (!success)
            {
                return NotFound(new { error = "DPA not found" });
            }

            _logger.LogInformation("Terminated DPA {DPAId}", id);

            return Ok(new { message = "DPA terminated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate DPA {DPAId}", id);
            return StatusCode(500, new { error = "Failed to terminate DPA", details = ex.Message });
        }
    }

    /// <summary>
    /// Renew DPA (creates new agreement, archives old)
    /// </summary>
    [HttpPost("{id}/renew")]
    [ProducesResponseType(typeof(DataProcessingAgreement), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RenewDPA(
        Guid id,
        [FromBody] RenewDPARequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var renewed = await _dpaService.RenewDPAAsync(
                id,
                request.NewEffectiveDate,
                request.NewExpiryDate,
                request.RenewedBy,
                cancellationToken);

            _logger.LogInformation("Renewed DPA {OldDPAId}, created new DPA {NewDPAId}", id, renewed.Id);

            return CreatedAtAction(nameof(GetDPAById), new { id = renewed.Id }, renewed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to renew DPA {DPAId}", id);
            return StatusCode(500, new { error = "Failed to renew DPA", details = ex.Message });
        }
    }

    /// <summary>
    /// Approve DPA (compliance officer review)
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "SuperAdmin,ComplianceOfficer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveDPA(
        Guid id,
        [FromBody] ApproveDPARequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _dpaService.ApproveDPAAsync(
                id,
                request.ApprovedBy,
                cancellationToken);

            if (!success)
            {
                return NotFound(new { error = "DPA not found" });
            }

            _logger.LogInformation("Approved DPA {DPAId} by {ApprovedBy}", id, request.ApprovedBy);

            return Ok(new { message = "DPA approved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve DPA {DPAId}", id);
            return StatusCode(500, new { error = "Failed to approve DPA", details = ex.Message });
        }
    }

    // ==========================================
    // DPA QUERY ENDPOINTS
    // ==========================================

    /// <summary>
    /// Get DPA by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DataProcessingAgreement), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDPAById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpa = await _dpaService.GetDPAByIdAsync(id, cancellationToken);

            if (dpa == null)
            {
                return NotFound(new { error = "DPA not found" });
            }

            return Ok(dpa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get DPA {DPAId}", id);
            return StatusCode(500, new { error = "Failed to get DPA", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all DPAs for a tenant
    /// </summary>
    [HttpGet("tenant/{tenantId}")]
    [ProducesResponseType(typeof(List<DataProcessingAgreement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantDPAs(
        Guid tenantId,
        [FromQuery] DpaStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpas = await _dpaService.GetTenantDPAsAsync(tenantId, status, cancellationToken);
            return Ok(dpas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get DPAs for tenant {TenantId}", tenantId);
            return StatusCode(500, new { error = "Failed to get tenant DPAs", details = ex.Message });
        }
    }

    /// <summary>
    /// Get platform-wide DPAs (AWS, Google Cloud, etc.)
    /// </summary>
    [HttpGet("platform")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(List<DataProcessingAgreement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlatformDPAs(
        [FromQuery] DpaStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpas = await _dpaService.GetPlatformDPAsAsync(status, cancellationToken);
            return Ok(dpas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get platform DPAs");
            return StatusCode(500, new { error = "Failed to get platform DPAs", details = ex.Message });
        }
    }

    /// <summary>
    /// Get DPAs expiring soon (renewal reminders)
    /// </summary>
    [HttpGet("expiring")]
    [ProducesResponseType(typeof(List<DataProcessingAgreement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiringDPAs(
        [FromQuery] int withinDays = 90,
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpas = await _dpaService.GetExpiringDPAsAsync(withinDays, tenantId, cancellationToken);
            return Ok(dpas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get expiring DPAs");
            return StatusCode(500, new { error = "Failed to get expiring DPAs", details = ex.Message });
        }
    }

    /// <summary>
    /// Get DPAs requiring risk reassessment
    /// </summary>
    [HttpGet("overdue-risk-assessments")]
    [ProducesResponseType(typeof(List<DataProcessingAgreement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdueRiskAssessments(
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpas = await _dpaService.GetOverdueRiskAssessmentsAsync(tenantId, cancellationToken);
            return Ok(dpas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get overdue risk assessments");
            return StatusCode(500, new { error = "Failed to get overdue risk assessments", details = ex.Message });
        }
    }

    /// <summary>
    /// Get DPAs with overdue audits
    /// </summary>
    [HttpGet("overdue-audits")]
    [ProducesResponseType(typeof(List<DataProcessingAgreement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdueAudits(
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpas = await _dpaService.GetOverdueAuditsAsync(tenantId, cancellationToken);
            return Ok(dpas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get overdue audits");
            return StatusCode(500, new { error = "Failed to get overdue audits", details = ex.Message });
        }
    }

    /// <summary>
    /// Search DPAs by vendor name
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<DataProcessingAgreement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchDPAs(
        [FromQuery] string vendorName,
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(vendorName))
            {
                return BadRequest(new { error = "Vendor name is required" });
            }

            var dpas = await _dpaService.SearchDPAsByVendorAsync(vendorName, tenantId, cancellationToken);
            return Ok(dpas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search DPAs");
            return StatusCode(500, new { error = "Failed to search DPAs", details = ex.Message });
        }
    }

    /// <summary>
    /// Get DPAs by risk level
    /// </summary>
    [HttpGet("risk-level/{riskLevel}")]
    [ProducesResponseType(typeof(List<DataProcessingAgreement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDPAsByRiskLevel(
        VendorRiskLevel riskLevel,
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpas = await _dpaService.GetDPAsByRiskLevelAsync(riskLevel, tenantId, cancellationToken);
            return Ok(dpas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get DPAs by risk level");
            return StatusCode(500, new { error = "Failed to get DPAs by risk level", details = ex.Message });
        }
    }

    /// <summary>
    /// Get vendors with international data transfers
    /// </summary>
    [HttpGet("international-transfers")]
    [ProducesResponseType(typeof(List<DataProcessingAgreement>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInternationalTransferDPAs(
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpas = await _dpaService.GetInternationalTransferDPAsAsync(tenantId, cancellationToken);
            return Ok(dpas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get international transfer DPAs");
            return StatusCode(500, new { error = "Failed to get international transfer DPAs", details = ex.Message });
        }
    }

    // ==========================================
    // RISK ASSESSMENT ENDPOINTS
    // ==========================================

    /// <summary>
    /// Record vendor risk assessment
    /// </summary>
    [HttpPost("{id}/risk-assessment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordRiskAssessment(
        Guid id,
        [FromBody] RiskAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _dpaService.RecordRiskAssessmentAsync(
                id,
                request.RiskLevel,
                request.AssessmentNotes,
                request.NextAssessmentDate,
                cancellationToken);

            if (!success)
            {
                return NotFound(new { error = "DPA not found" });
            }

            _logger.LogInformation("Recorded risk assessment for DPA {DPAId}", id);

            return Ok(new { message = "Risk assessment recorded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record risk assessment for DPA {DPAId}", id);
            return StatusCode(500, new { error = "Failed to record risk assessment", details = ex.Message });
        }
    }

    // ==========================================
    // SUB-PROCESSOR ENDPOINTS
    // ==========================================

    /// <summary>
    /// Add sub-processor to DPA
    /// </summary>
    [HttpPost("{id}/sub-processor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddSubProcessor(
        Guid id,
        [FromBody] AddSubProcessorRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _dpaService.AddSubProcessorAsync(
                id,
                request.SubProcessorName,
                request.Purpose,
                cancellationToken);

            if (!success)
            {
                return NotFound(new { error = "DPA not found" });
            }

            _logger.LogInformation("Added sub-processor {SubProcessorName} to DPA {DPAId}",
                request.SubProcessorName, id);

            return Ok(new { message = "Sub-processor added successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add sub-processor to DPA {DPAId}", id);
            return StatusCode(500, new { error = "Failed to add sub-processor", details = ex.Message });
        }
    }

    /// <summary>
    /// Remove sub-processor from DPA
    /// </summary>
    [HttpDelete("{id}/sub-processor/{subProcessorName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSubProcessor(
        Guid id,
        string subProcessorName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _dpaService.RemoveSubProcessorAsync(
                id,
                subProcessorName,
                cancellationToken);

            if (!success)
            {
                return NotFound(new { error = "DPA or sub-processor not found" });
            }

            _logger.LogInformation("Removed sub-processor {SubProcessorName} from DPA {DPAId}",
                subProcessorName, id);

            return Ok(new { message = "Sub-processor removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove sub-processor from DPA {DPAId}", id);
            return StatusCode(500, new { error = "Failed to remove sub-processor", details = ex.Message });
        }
    }

    // ==========================================
    // AUDIT & COMPLIANCE ENDPOINTS
    // ==========================================

    /// <summary>
    /// Record vendor audit
    /// </summary>
    [HttpPost("{id}/audit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordAudit(
        Guid id,
        [FromBody] RecordAuditRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _dpaService.RecordAuditAsync(
                id,
                request.AuditDate,
                request.AuditFindings,
                request.NextAuditDate,
                cancellationToken);

            if (!success)
            {
                return NotFound(new { error = "DPA not found" });
            }

            _logger.LogInformation("Recorded audit for DPA {DPAId}", id);

            return Ok(new { message = "Audit recorded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record audit for DPA {DPAId}", id);
            return StatusCode(500, new { error = "Failed to record audit", details = ex.Message });
        }
    }

    /// <summary>
    /// Get DPA compliance dashboard
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DPAComplianceDashboard), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComplianceDashboard(
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dashboard = await _dpaService.GetComplianceDashboardAsync(tenantId, cancellationToken);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get DPA compliance dashboard");
            return StatusCode(500, new { error = "Failed to get compliance dashboard", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate GDPR Article 30 processor registry
    /// </summary>
    [HttpGet("processor-registry")]
    [ProducesResponseType(typeof(ProcessorRegistry), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateProcessorRegistry(
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var registry = await _dpaService.GenerateProcessorRegistryAsync(tenantId, cancellationToken);
            return Ok(registry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate processor registry");
            return StatusCode(500, new { error = "Failed to generate processor registry", details = ex.Message });
        }
    }
}

// ==========================================
// REQUEST DTOs
// ==========================================

public class CreateDPARequest
{
    public Guid? TenantId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? VendorType { get; set; } // "DataProcessor", "SubProcessor", "JointController"
    public string? VendorContactName { get; set; }
    public string? VendorContactEmail { get; set; }
    public string? VendorCountry { get; set; }
    public string? VendorAddress { get; set; }
    public string ProcessingPurpose { get; set; } = string.Empty;
    public List<string>? DataCategories { get; set; } // PersonalDataCategories
    public List<string>? DataSubjectCategories { get; set; }
    public int RetentionPeriodDays { get; set; }
    public string? SecurityMeasures { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal? AnnualValueUsd { get; set; }
    public VendorRiskLevel? RiskLevel { get; set; }
    public bool InternationalDataTransfer { get; set; }
    public List<string>? TransferCountries { get; set; }
    public string? TransferMechanism { get; set; } // "StandardContractualClauses", "AdequacyDecision", "BCR"
    public bool AllowsSubProcessors { get; set; }
    public List<string>? SubProcessorsList { get; set; } // AuthorizedSubProcessors
    public string? AuditRights { get; set; } // "OnDemand", "Annual", "Quarterly", "None"
    public DateTime? NextAuditDate { get; set; }
}

public class UpdateDPARequest
{
    public string? VendorName { get; set; }
    public string? VendorType { get; set; }
    public string? VendorContactName { get; set; }
    public string? VendorContactEmail { get; set; }
    public string? VendorCountry { get; set; }
    public string? VendorAddress { get; set; }
    public string? ProcessingPurpose { get; set; }
    public List<string>? DataCategories { get; set; }
    public List<string>? DataSubjectCategories { get; set; }
    public int? RetentionPeriodDays { get; set; }
    public string? SecurityMeasures { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal? AnnualValueUsd { get; set; }
    public VendorRiskLevel? RiskLevel { get; set; }
    public bool? InternationalDataTransfer { get; set; }
    public List<string>? TransferCountries { get; set; }
    public string? TransferMechanism { get; set; }
    public bool? AllowsSubProcessors { get; set; }
    public List<string>? SubProcessorsList { get; set; }
    public string? AuditRights { get; set; }
}

public class TerminateDPARequest
{
    public string TerminationReason { get; set; } = string.Empty;
    public Guid TerminatedBy { get; set; }
}

public class RenewDPARequest
{
    public DateTime NewEffectiveDate { get; set; }
    public DateTime NewExpiryDate { get; set; }
    public Guid RenewedBy { get; set; }
}

public class ApproveDPARequest
{
    public Guid ApprovedBy { get; set; }
}

public class RiskAssessmentRequest
{
    public VendorRiskLevel RiskLevel { get; set; }
    public string? AssessmentNotes { get; set; }
    public DateTime? NextAssessmentDate { get; set; }
}

public class AddSubProcessorRequest
{
    public string SubProcessorName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
}

public class RecordAuditRequest
{
    public DateTime AuditDate { get; set; }
    public string? AuditFindings { get; set; }
    public DateTime? NextAuditDate { get; set; }
}
