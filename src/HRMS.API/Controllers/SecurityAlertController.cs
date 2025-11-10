using HRMS.Application.Interfaces;
using HRMS.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Security Alert Management API
/// Real-time threat detection and security alert management
/// Supports Fortune 500 compliance requirements (SOX, GDPR, ISO 27001, PCI-DSS)
/// </summary>
[ApiController]
[Route("api/security-alerts")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class SecurityAlertController : ControllerBase
{
    private readonly ISecurityAlertingService _securityAlertingService;
    private readonly ILogger<SecurityAlertController> _logger;

    public SecurityAlertController(
        ISecurityAlertingService securityAlertingService,
        ILogger<SecurityAlertController> logger)
    {
        _securityAlertingService = securityAlertingService;
        _logger = logger;
    }

    /// <summary>
    /// Get all security alerts with optional filtering and pagination
    /// </summary>
    /// <param name="tenantId">Filter by tenant ID</param>
    /// <param name="status">Filter by alert status</param>
    /// <param name="severity">Filter by severity level</param>
    /// <param name="alertType">Filter by alert type</param>
    /// <param name="startDate">Filter by start date</param>
    /// <param name="endDate">Filter by end date</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    [HttpGet]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] SecurityAlertStatus? status = null,
        [FromQuery] AuditSeverity? severity = null,
        [FromQuery] SecurityAlertType? alertType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;

            var (alerts, totalCount) = await _securityAlertingService.GetAlertsAsync(
                tenantId, status, severity, alertType, startDate, endDate, pageNumber, pageSize);

            return Ok(new
            {
                success = true,
                data = alerts,
                pagination = new
                {
                    currentPage = pageNumber,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security alerts");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving security alerts" });
        }
    }

    /// <summary>
    /// Get a single security alert by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAlertById(Guid id)
    {
        try
        {
            var alert = await _securityAlertingService.GetAlertByIdAsync(id);

            if (alert == null)
            {
                return NotFound(new { success = false, error = "Security alert not found" });
            }

            return Ok(new { success = true, data = alert });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security alert {AlertId}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving the security alert" });
        }
    }

    /// <summary>
    /// Get active alert counts grouped by severity
    /// </summary>
    [HttpGet("counts/by-severity")]
    public async Task<IActionResult> GetActiveAlertCountsBySeverity([FromQuery] Guid? tenantId = null)
    {
        try
        {
            var counts = await _securityAlertingService.GetActiveAlertCountsBySeverityAsync(tenantId);
            return Ok(new { success = true, data = counts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active alert counts");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving alert counts" });
        }
    }

    /// <summary>
    /// Get recent critical alerts (last 24 hours by default)
    /// </summary>
    [HttpGet("critical/recent")]
    public async Task<IActionResult> GetRecentCriticalAlerts(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] int hours = 24)
    {
        try
        {
            if (hours < 1) hours = 24;
            if (hours > 168) hours = 168; // Max 7 days

            var alerts = await _securityAlertingService.GetRecentCriticalAlertsAsync(tenantId, hours);
            return Ok(new { success = true, data = alerts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent critical alerts");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving recent critical alerts" });
        }
    }

    /// <summary>
    /// Get alert statistics for a time period
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetAlertStatistics(
        [FromQuery] Guid? tenantId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate == default || endDate == default)
            {
                return BadRequest(new { success = false, error = "Start date and end date are required" });
            }

            if (endDate < startDate)
            {
                return BadRequest(new { success = false, error = "End date must be after start date" });
            }

            var statistics = await _securityAlertingService.GetAlertStatisticsAsync(tenantId, startDate, endDate);
            return Ok(new { success = true, data = statistics });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert statistics");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving alert statistics" });
        }
    }

    /// <summary>
    /// Acknowledge a security alert
    /// </summary>
    [HttpPost("{id:guid}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            var alert = await _securityAlertingService.AcknowledgeAlertAsync(id, userId, userEmail);

            _logger.LogInformation("Security alert {AlertId} acknowledged by {UserEmail}", id, userEmail);

            return Ok(new { success = true, data = alert, message = "Alert acknowledged successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, error = "Security alert not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { success = false, error = "User authentication failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging security alert {AlertId}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while acknowledging the alert" });
        }
    }

    /// <summary>
    /// Assign a security alert to a user for investigation
    /// </summary>
    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> AssignAlert(Guid id, [FromBody] AssignAlertRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, error = "Invalid request data", errors = ModelState });
            }

            var assignedBy = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            var alert = await _securityAlertingService.AssignAlertAsync(
                id,
                request.AssignedTo,
                request.AssignedToEmail,
                assignedBy);

            _logger.LogInformation("Security alert {AlertId} assigned to {AssignedToEmail}", id, request.AssignedToEmail);

            return Ok(new { success = true, data = alert, message = "Alert assigned successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, error = "Security alert not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { success = false, error = "User authentication failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning security alert {AlertId}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while assigning the alert" });
        }
    }

    /// <summary>
    /// Mark a security alert as in progress
    /// </summary>
    [HttpPost("{id:guid}/in-progress")]
    public async Task<IActionResult> MarkAlertInProgress(Guid id)
    {
        try
        {
            var alert = await _securityAlertingService.MarkAlertInProgressAsync(id);

            _logger.LogInformation("Security alert {AlertId} marked as in progress", id);

            return Ok(new { success = true, data = alert, message = "Alert marked as in progress" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, error = "Security alert not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking security alert {AlertId} as in progress", id);
            return StatusCode(500, new { success = false, error = "An error occurred while updating the alert" });
        }
    }

    /// <summary>
    /// Resolve a security alert
    /// </summary>
    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> ResolveAlert(Guid id, [FromBody] ResolveAlertRequest request)
    {
        try
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.ResolutionNotes))
            {
                return BadRequest(new { success = false, error = "Resolution notes are required" });
            }

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            var alert = await _securityAlertingService.ResolveAlertAsync(id, userId, userEmail, request.ResolutionNotes);

            _logger.LogInformation("Security alert {AlertId} resolved by {UserEmail}", id, userEmail);

            return Ok(new { success = true, data = alert, message = "Alert resolved successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, error = "Security alert not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { success = false, error = "User authentication failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving security alert {AlertId}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while resolving the alert" });
        }
    }

    /// <summary>
    /// Mark a security alert as false positive
    /// </summary>
    [HttpPost("{id:guid}/false-positive")]
    public async Task<IActionResult> MarkAlertAsFalsePositive(Guid id, [FromBody] FalsePositiveRequest request)
    {
        try
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { success = false, error = "Reason is required" });
            }

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            var alert = await _securityAlertingService.MarkAlertAsFalsePositiveAsync(id, userId, userEmail, request.Reason);

            _logger.LogInformation("Security alert {AlertId} marked as false positive by {UserEmail}", id, userEmail);

            return Ok(new { success = true, data = alert, message = "Alert marked as false positive" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, error = "Security alert not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { success = false, error = "User authentication failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking security alert {AlertId} as false positive", id);
            return StatusCode(500, new { success = false, error = "An error occurred while updating the alert" });
        }
    }

    /// <summary>
    /// Escalate a security alert to senior security team
    /// </summary>
    [HttpPost("{id:guid}/escalate")]
    public async Task<IActionResult> EscalateAlert(Guid id, [FromBody] EscalateAlertRequest request)
    {
        try
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.EscalatedTo))
            {
                return BadRequest(new { success = false, error = "Escalation recipient is required" });
            }

            var escalatedBy = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            var alert = await _securityAlertingService.EscalateAlertAsync(id, request.EscalatedTo, escalatedBy);

            _logger.LogWarning("Security alert {AlertId} escalated to {EscalatedTo}", id, request.EscalatedTo);

            return Ok(new { success = true, data = alert, message = "Alert escalated successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, error = "Security alert not found" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { success = false, error = "User authentication failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating security alert {AlertId}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while escalating the alert" });
        }
    }
}

// ============================================
// REQUEST DTOs
// ============================================

public class AssignAlertRequest
{
    public Guid AssignedTo { get; set; }
    public string AssignedToEmail { get; set; } = string.Empty;
}

public class ResolveAlertRequest
{
    public string ResolutionNotes { get; set; } = string.Empty;
}

public class FalsePositiveRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class EscalateAlertRequest
{
    public string EscalatedTo { get; set; } = string.Empty;
}
