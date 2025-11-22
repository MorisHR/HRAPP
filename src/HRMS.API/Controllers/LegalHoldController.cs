using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Application.Interfaces;
using HRMS.Core.Enums;

namespace HRMS.API.Controllers;

/// <summary>
/// Legal hold controller for litigation and e-discovery
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,LegalAdmin")]
public class LegalHoldController : ControllerBase
{
    private readonly ILegalHoldService _legalHoldService;
    private readonly IEDiscoveryService _eDiscoveryService;
    private readonly ILogger<LegalHoldController> _logger;

    public LegalHoldController(
        ILegalHoldService legalHoldService,
        IEDiscoveryService eDiscoveryService,
        ILogger<LegalHoldController> logger)
    {
        _legalHoldService = legalHoldService;
        _eDiscoveryService = eDiscoveryService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateLegalHold([FromBody] CreateLegalHoldRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var legalHold = await _legalHoldService.CreateLegalHoldAsync(
                request.TenantId, request.CaseNumber, request.Description,
                request.StartDate, request.EndDate, request.UserIds, request.EntityTypes,
                userId, request.LegalRepresentative, request.CourtOrder, cancellationToken);

            return Ok(legalHold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create legal hold");
            return StatusCode(500, new { error = "Failed to create legal hold" });
        }
    }

    [HttpPost("{id}/release")]
    public async Task<IActionResult> ReleaseLegalHold(Guid id, [FromBody] ReleaseLegalHoldRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var legalHold = await _legalHoldService.ReleaseLegalHoldAsync(id, userId, request.ReleaseNotes, cancellationToken);
            return Ok(legalHold);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release legal hold {LegalHoldId}", id);
            return StatusCode(500, new { error = "Failed to release legal hold" });
        }
    }

    /// <summary>
    /// Get all legal holds (filters to active by default)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLegalHolds([FromQuery] Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var legalHolds = await _legalHoldService.GetActiveLegalHoldsAsync(tenantId, cancellationToken);
            return Ok(legalHolds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve legal holds");
            return StatusCode(500, new { error = "Failed to retrieve legal holds" });
        }
    }

    /// <summary>
    /// Get active legal holds only
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveLegalHolds([FromQuery] Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var legalHolds = await _legalHoldService.GetActiveLegalHoldsAsync(tenantId, cancellationToken);
            return Ok(legalHolds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active legal holds");
            return StatusCode(500, new { error = "Failed to retrieve active legal holds" });
        }
    }

    /// <summary>
    /// Get legal hold by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLegalHold(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var legalHold = await _legalHoldService.GetLegalHoldByIdAsync(id, cancellationToken);
            if (legalHold == null)
                return NotFound(new { error = $"Legal hold {id} not found" });
            return Ok(legalHold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve legal hold {LegalHoldId}", id);
            return StatusCode(500, new { error = "Failed to retrieve legal hold" });
        }
    }

    /// <summary>
    /// Update legal hold
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLegalHold(Guid id, [FromBody] UpdateLegalHoldRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var legalHold = await _legalHoldService.UpdateLegalHoldAsync(
                id,
                request.Description,
                request.EndDate,
                request.LegalRepresentative,
                request.LegalRepresentativeEmail,
                request.LawFirm,
                userId,
                cancellationToken);

            return Ok(legalHold);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update legal hold {LegalHoldId}", id);
            return StatusCode(500, new { error = "Failed to update legal hold" });
        }
    }

    /// <summary>
    /// Get audit logs affected by legal hold
    /// </summary>
    [HttpGet("{id}/audit-logs")]
    public async Task<IActionResult> GetAffectedAuditLogs(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLogs = await _legalHoldService.GetAffectedAuditLogsAsync(id, cancellationToken);
            return Ok(auditLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit logs for legal hold {LegalHoldId}", id);
            return StatusCode(500, new { error = "Failed to retrieve audit logs" });
        }
    }

    /// <summary>
    /// Export eDiscovery package (aligned with frontend: GET /ediscovery/{format})
    /// </summary>
    [HttpGet("{id}/ediscovery/{format}")]
    public async Task<IActionResult> ExportEDiscoveryPackage(Guid id, EDiscoveryFormat format, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting eDiscovery package for legal hold {LegalHoldId} in format {Format}", id, format);

            var data = await _eDiscoveryService.CreateEDiscoveryPackageAsync(id, format, cancellationToken);

            var contentType = format switch
            {
                EDiscoveryFormat.PDF => "application/pdf",
                EDiscoveryFormat.JSON => "application/json",
                EDiscoveryFormat.CSV => "text/csv",
                EDiscoveryFormat.EMLX => "message/rfc822",
                EDiscoveryFormat.NATIVE => "application/octet-stream",
                _ => "application/octet-stream"
            };

            var fileName = $"ediscovery_case_{id}_{DateTime.UtcNow:yyyyMMdd}.{format.ToString().ToLower()}";

            return File(data, contentType, fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Legal hold {LegalHoldId} not found for eDiscovery export", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export eDiscovery package for legal hold {LegalHoldId}", id);
            return StatusCode(500, new { error = "Failed to export eDiscovery package", details = ex.Message });
        }
    }
}

public class CreateLegalHoldRequest
{
    public Guid? TenantId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<Guid>? UserIds { get; set; }
    public List<string>? EntityTypes { get; set; }
    public string? LegalRepresentative { get; set; }
    public string? CourtOrder { get; set; }
}

public class UpdateLegalHoldRequest
{
    public string? Description { get; set; }
    public DateTime? EndDate { get; set; }
    public string? LegalRepresentative { get; set; }
    public string? LegalRepresentativeEmail { get; set; }
    public string? LawFirm { get; set; }
}

public class ReleaseLegalHoldRequest
{
    public string? ReleaseNotes { get; set; }
}
