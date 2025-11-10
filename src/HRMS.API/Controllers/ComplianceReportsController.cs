using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Application.Interfaces;

namespace HRMS.API.Controllers;

/// <summary>
/// Compliance reports controller for SOX, GDPR, and regulatory reporting
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,ComplianceOfficer")]
public class ComplianceReportsController : ControllerBase
{
    private readonly ISOXComplianceService _soxService;
    private readonly IGDPRComplianceService _gdprService;
    private readonly IAuditCorrelationService _correlationService;
    private readonly ILogger<ComplianceReportsController> _logger;

    public ComplianceReportsController(
        ISOXComplianceService soxService,
        IGDPRComplianceService gdprService,
        IAuditCorrelationService correlationService,
        ILogger<ComplianceReportsController> logger)
    {
        _soxService = soxService;
        _gdprService = gdprService;
        _correlationService = correlationService;
        _logger = logger;
    }

    [HttpGet("sox/full")]
    public async Task<IActionResult> GenerateSOXReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var report = await _soxService.GenerateFullSOXReportAsync(start, end, tenantId, cancellationToken);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SOX report");
            return StatusCode(500, new { error = "Failed to generate SOX report" });
        }
    }

    [HttpGet("sox/financial-access")]
    public async Task<IActionResult> GenerateFinancialAccessReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var report = await _soxService.GenerateFinancialDataAccessReportAsync(start, end, tenantId, cancellationToken);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate financial access report");
            return StatusCode(500, new { error = "Failed to generate report" });
        }
    }

    [HttpGet("gdpr/right-to-access/{userId}")]
    public async Task<IActionResult> GenerateRightToAccessReport(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _gdprService.GenerateRightToAccessReportAsync(userId, cancellationToken);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate right to access report");
            return StatusCode(500, new { error = "Failed to generate report" });
        }
    }

    [HttpGet("gdpr/right-to-be-forgotten/{userId}")]
    public async Task<IActionResult> GenerateRightToBeForgottenReport(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _gdprService.GenerateRightToBeForgettenReportAsync(userId, cancellationToken);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate right to be forgotten report");
            return StatusCode(500, new { error = "Failed to generate report" });
        }
    }

    [HttpGet("correlation/user-activity/{userId}")]
    public async Task<IActionResult> CorrelateUserActivity(
        Guid userId,
        [FromQuery] int hoursBack = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var correlation = await _correlationService.CorrelateUserActivityAsync(
                userId, TimeSpan.FromHours(hoursBack), cancellationToken);
            return Ok(correlation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to correlate user activity");
            return StatusCode(500, new { error = "Failed to correlate activity" });
        }
    }

    [HttpGet("correlation/patterns")]
    public async Task<IActionResult> DetectPatterns(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] int daysBack = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var patterns = await _correlationService.DetectPatternsAcrossUsersAsync(tenantId, daysBack, cancellationToken);
            return Ok(patterns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect patterns");
            return StatusCode(500, new { error = "Failed to detect patterns" });
        }
    }
}
