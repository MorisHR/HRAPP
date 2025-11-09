using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Application.DTOs.AuditLog;
using HRMS.Core.Interfaces;
using HRMS.Core.Enums;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// SuperAdmin Audit Log Controller - Views ALL system audit logs
/// Authorization: SuperAdmin role only
/// Access: System-wide audit trail across all tenants
/// </summary>
[ApiController]
[Route("api/superadmin/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(
        IAuditLogService auditLogService,
        ILogger<AuditLogController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated audit logs with filters (SuperAdmin - ALL tenants)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilterDto filter)
    {
        try
        {
            var result = await _auditLogService.GetAuditLogsAsync(filter);
            return Ok(new
            {
                success = true,
                data = result,
                message = "Audit logs retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving audit logs"
            });
        }
    }

    /// <summary>
    /// Get single audit log details by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AuditLogDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditLogById(Guid id)
    {
        try
        {
            var log = await _auditLogService.GetAuditLogByIdAsync(id);

            if (log == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Audit log not found"
                });
            }

            return Ok(new
            {
                success = true,
                data = log,
                message = "Audit log retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log {Id}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving the audit log"
            });
        }
    }

    /// <summary>
    /// Get audit log statistics (SuperAdmin - system-wide)
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AuditLogStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var stats = await _auditLogService.GetStatisticsAsync(startDate, endDate, tenantId: null);

            return Ok(new
            {
                success = true,
                data = stats,
                message = "Statistics retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log statistics");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving statistics"
            });
        }
    }

    /// <summary>
    /// Export audit logs to CSV (SuperAdmin - ALL data)
    /// </summary>
    [HttpPost("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLogs([FromBody] AuditLogFilterDto filter)
    {
        try
        {
            var csvData = await _auditLogService.ExportToCsvAsync(filter);

            var fileName = $"audit_logs_system_wide_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

            return File(
                System.Text.Encoding.UTF8.GetBytes(csvData),
                "text/csv",
                fileName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while exporting audit logs"
            });
        }
    }

    /// <summary>
    /// Get failed login attempts (security monitoring)
    /// </summary>
    [HttpGet("failed-logins")]
    [ProducesResponseType(typeof(List<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailedLogins(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var filter = new AuditLogFilterDto
            {
                ActionTypes = new List<AuditActionType> { AuditActionType.LOGIN_FAILED },
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-7),
                EndDate = endDate ?? DateTime.UtcNow,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _auditLogService.GetAuditLogsAsync(filter);

            return Ok(new
            {
                success = true,
                data = result,
                message = "Failed login attempts retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failed logins");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving failed logins"
            });
        }
    }

    /// <summary>
    /// Get critical security events
    /// </summary>
    [HttpGet("critical-events")]
    [ProducesResponseType(typeof(List<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCriticalEvents(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var filter = new AuditLogFilterDto
            {
                Severities = new List<AuditSeverity> { AuditSeverity.CRITICAL, AuditSeverity.EMERGENCY },
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _auditLogService.GetAuditLogsAsync(filter);

            return Ok(new
            {
                success = true,
                data = result,
                message = "Critical events retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving critical events");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving critical events"
            });
        }
    }
}
