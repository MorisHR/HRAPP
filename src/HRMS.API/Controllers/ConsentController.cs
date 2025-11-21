using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;

namespace HRMS.API.Controllers;

/// <summary>
/// GDPR Article 7 - Consent Management API
/// FORTUNE 500 PATTERN: OneTrust, TrustArc consent platforms
///
/// SECURITY:
/// - Role-based authorization (SuperAdmin, ComplianceOfficer, User)
/// - Users can only manage their own consents
/// - Admins can view all consents
///
/// ENDPOINTS:
/// - POST /api/consent - Record consent
/// - DELETE /api/consent/{id} - Withdraw consent
/// - GET /api/consent/user/{userId} - Get user consents
/// - GET /api/consent/check - Check if user has active consent
/// - GET /api/consent/statistics - Get consent statistics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConsentController : ControllerBase
{
    private readonly IConsentManagementService _consentService;
    private readonly ILogger<ConsentController> _logger;

    public ConsentController(
        IConsentManagementService consentService,
        ILogger<ConsentController> logger)
    {
        _consentService = consentService;
        _logger = logger;
    }

    /// <summary>
    /// Record user consent
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserConsent), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordConsent(
        [FromBody] RecordConsentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get IP address and user agent from request
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            var consent = await _consentService.RecordConsentAsync(
                request.UserId,
                request.UserEmail,
                request.ConsentType,
                request.ConsentCategory,
                request.ConsentText,
                request.ConsentVersion,
                request.IsExplicit,
                request.TenantId,
                ipAddress,
                userAgent,
                cancellationToken);

            return CreatedAtAction(nameof(GetUserConsents), new { userId = consent.UserId }, consent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record consent");
            return StatusCode(500, new { error = "Failed to record consent", details = ex.Message });
        }
    }

    /// <summary>
    /// Withdraw consent (GDPR: Must be as easy as giving)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WithdrawConsent(
        Guid id,
        [FromBody] WithdrawConsentRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _consentService.WithdrawConsentAsync(
                id,
                request?.WithdrawalReason,
                cancellationToken);

            if (!success)
            {
                return NotFound(new { error = "Consent not found" });
            }

            return Ok(new { message = "Consent withdrawn successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to withdraw consent {ConsentId}", id);
            return StatusCode(500, new { error = "Failed to withdraw consent", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all consents for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<UserConsent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserConsents(
        Guid userId,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var consents = await _consentService.GetUserConsentsAsync(userId, activeOnly, cancellationToken);
            return Ok(consents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get consents for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to get consents", details = ex.Message });
        }
    }

    /// <summary>
    /// Check if user has active consent
    /// </summary>
    [HttpGet("check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckConsent(
        [FromQuery] Guid userId,
        [FromQuery] ConsentType consentType,
        [FromQuery] string consentCategory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var hasConsent = await _consentService.HasActiveConsentAsync(
                userId,
                consentType,
                consentCategory,
                cancellationToken);

            return Ok(new { hasConsent });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check consent");
            return StatusCode(500, new { error = "Failed to check consent", details = ex.Message });
        }
    }

    /// <summary>
    /// Get consent statistics (Admin only)
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "SuperAdmin,ComplianceOfficer")]
    [ProducesResponseType(typeof(ConsentStatistics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = await _consentService.GetConsentStatisticsAsync(
                tenantId,
                from,
                to,
                cancellationToken);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get consent statistics");
            return StatusCode(500, new { error = "Failed to get statistics", details = ex.Message });
        }
    }

    /// <summary>
    /// Get expiring consents (Admin only)
    /// </summary>
    [HttpGet("expiring")]
    [Authorize(Roles = "SuperAdmin,ComplianceOfficer")]
    [ProducesResponseType(typeof(List<UserConsent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiringConsents(
        [FromQuery] int withinDays = 30,
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var consents = await _consentService.GetExpiringSoonConsentsAsync(
                withinDays,
                tenantId,
                cancellationToken);

            return Ok(consents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get expiring consents");
            return StatusCode(500, new { error = "Failed to get expiring consents", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate compliance report (Admin only)
    /// </summary>
    [HttpGet("compliance-report")]
    [Authorize(Roles = "SuperAdmin,ComplianceOfficer")]
    [ProducesResponseType(typeof(ConsentComplianceReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateComplianceReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _consentService.GenerateComplianceReportAsync(
                from,
                to,
                tenantId,
                cancellationToken);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate compliance report");
            return StatusCode(500, new { error = "Failed to generate report", details = ex.Message });
        }
    }

    /// <summary>
    /// Get consent audit trail for user
    /// </summary>
    [HttpGet("audit-trail/{userId}")]
    [ProducesResponseType(typeof(List<ConsentAuditEntry>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditTrail(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditTrail = await _consentService.GetConsentAuditTrailAsync(userId, cancellationToken);
            return Ok(auditTrail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get consent audit trail");
            return StatusCode(500, new { error = "Failed to get audit trail", details = ex.Message });
        }
    }
}

// ==========================================
// REQUEST DTOs
// ==========================================

public class RecordConsentRequest
{
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public ConsentType ConsentType { get; set; }
    public string ConsentCategory { get; set; } = string.Empty;
    public string ConsentText { get; set; } = string.Empty;
    public string ConsentVersion { get; set; } = "1.0";
    public bool IsExplicit { get; set; } = true;
    public Guid? TenantId { get; set; }
}

public class WithdrawConsentRequest
{
    public string? WithdrawalReason { get; set; }
}
