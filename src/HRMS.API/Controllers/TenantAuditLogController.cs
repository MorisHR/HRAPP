using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Application.DTOs.AuditLog;
using HRMS.Application.Interfaces;
using HRMS.Core.Interfaces;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Middleware;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Tenant Admin Audit Log Controller - Views ONLY tenant-specific audit logs
/// Authorization: Tenant Admin role
/// Access: Tenant-scoped only (TenantId enforced by backend)
/// CRITICAL: Never trust frontend - always validate TenantId on backend
/// </summary>
[ApiController]
[Route("api/tenant/[controller]")]
[Authorize]
[ServiceFilter(typeof(TenantAuthorizationFilter))]
public class TenantAuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantAuditLogController> _logger;

    public TenantAuditLogController(
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<TenantAuditLogController> logger)
    {
        _auditLogService = auditLogService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated audit logs for current tenant ONLY
    /// Backend enforces TenantId filtering - cannot access other tenants
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantAuditLogs([FromQuery] AuditLogFilterDto filter)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;

            if (tenantId == null || tenantId == Guid.Empty)
            {
                _logger.LogWarning("Tenant audit log access attempted without valid tenant context");
                return BadRequest(new
                {
                    success = false,
                    message = "Tenant context not available"
                });
            }

            // CRITICAL: Force tenantId filter - never trust frontend
            filter.TenantId = tenantId.Value;

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
            _logger.LogError(ex, "Error retrieving tenant audit logs for tenant {TenantId}", _tenantContext.TenantId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving audit logs"
            });
        }
    }

    /// <summary>
    /// Get single audit log details - validates belongs to current tenant
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AuditLogDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditLogById(Guid id)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;

            if (tenantId == null || tenantId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Tenant context not available"
                });
            }

            var log = await _auditLogService.GetAuditLogByIdAsync(id);

            if (log == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Audit log not found"
                });
            }

            // CRITICAL: Validate the audit log belongs to the current tenant
            if (log.TenantId != tenantId.Value)
            {
                _logger.LogWarning(
                    "Tenant {TenantId} attempted to access audit log {LogId} belonging to tenant {LogTenantId}",
                    tenantId, id, log.TenantId);

                return Forbid();
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
            _logger.LogError(ex, "Error retrieving audit log {Id} for tenant {TenantId}", id, _tenantContext.TenantId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving the audit log"
            });
        }
    }

    /// <summary>
    /// Get audit log statistics for current tenant only
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AuditLogStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;

            if (tenantId == null || tenantId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Tenant context not available"
                });
            }

            var stats = await _auditLogService.GetStatisticsAsync(startDate, endDate, tenantId.Value);

            return Ok(new
            {
                success = true,
                data = stats,
                message = "Statistics retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log statistics for tenant {TenantId}", _tenantContext.TenantId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving statistics"
            });
        }
    }

    /// <summary>
    /// Export tenant audit logs to CSV (tenant-scoped only)
    /// </summary>
    [HttpPost("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLogs([FromBody] AuditLogFilterDto filter)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;

            if (tenantId == null || tenantId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Tenant context not available"
                });
            }

            // CRITICAL: Force tenantId filter - never trust frontend
            filter.TenantId = tenantId.Value;

            var csvData = await _auditLogService.ExportToCsvAsync(filter);

            var tenantName = _tenantContext.TenantName ?? "tenant";
            var fileName = $"audit_logs_{tenantName}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

            _logger.LogInformation(
                "Tenant {TenantId} exported audit logs. Filter: {Filter}",
                tenantId, System.Text.Json.JsonSerializer.Serialize(filter));

            return File(
                System.Text.Encoding.UTF8.GetBytes(csvData),
                "text/csv",
                fileName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs for tenant {TenantId}", _tenantContext.TenantId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while exporting audit logs"
            });
        }
    }

    /// <summary>
    /// Get failed login attempts for current tenant
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
            var tenantId = _tenantContext.TenantId;

            if (tenantId == null || tenantId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Tenant context not available"
                });
            }

            var filter = new AuditLogFilterDto
            {
                TenantId = tenantId.Value,
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
            _logger.LogError(ex, "Error retrieving failed logins for tenant {TenantId}", _tenantContext.TenantId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving failed logins"
            });
        }
    }

    /// <summary>
    /// Get sensitive data changes (salary updates, etc.) for current tenant
    /// </summary>
    [HttpGet("sensitive-changes")]
    [ProducesResponseType(typeof(List<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSensitiveChanges(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;

            if (tenantId == null || tenantId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Tenant context not available"
                });
            }

            var filter = new AuditLogFilterDto
            {
                TenantId = tenantId.Value,
                Severities = new List<AuditSeverity> { AuditSeverity.WARNING, AuditSeverity.CRITICAL },
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
                message = "Sensitive changes retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sensitive changes for tenant {TenantId}", _tenantContext.TenantId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving sensitive changes"
            });
        }
    }

    /// <summary>
    /// Get user activity report for current tenant
    /// </summary>
    [HttpGet("user-activity")]
    [ProducesResponseType(typeof(List<UserActivityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserActivity(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;

            if (tenantId == null || tenantId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Tenant context not available"
                });
            }

            var activity = await _auditLogService.GetUserActivityAsync(
                tenantId.Value,
                startDate ?? DateTime.UtcNow.AddDays(-30),
                endDate ?? DateTime.UtcNow);

            return Ok(new
            {
                success = true,
                data = activity,
                message = "User activity retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user activity for tenant {TenantId}", _tenantContext.TenantId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving user activity"
            });
        }
    }
}
