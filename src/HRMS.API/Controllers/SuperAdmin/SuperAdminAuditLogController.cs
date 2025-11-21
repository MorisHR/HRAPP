using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Infrastructure.Data;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;

namespace HRMS.API.Controllers.SuperAdmin;

/// <summary>
/// SuperAdmin Audit Log API - System-Wide Audit Trail
/// Fortune 500-grade audit logging for compliance and security
/// SECURITY: SuperAdmin role ONLY - NO tenant admin access
/// COMPLIANCE: Immutable audit records with 10+ year retention
/// </summary>
[ApiController]
[Route("superadmin/AuditLog")]
[Authorize(Roles = "SuperAdmin")] // CRITICAL: SuperAdmin ONLY
public class SuperAdminAuditLogController : ControllerBase
{
    private readonly MasterDbContext _masterContext;
    private readonly ILogger<SuperAdminAuditLogController> _logger;

    public SuperAdminAuditLogController(
        MasterDbContext masterContext,
        ILogger<SuperAdminAuditLogController> logger)
    {
        _masterContext = masterContext;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated audit log entries
    /// SECURITY: SuperAdmin only - validates role at runtime
    /// PERFORMANCE: Includes pagination and filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedAuditLogResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? severity = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // SECURITY: Additional runtime check (defense in depth)
            if (!User.IsInRole("SuperAdmin"))
            {
                _logger.LogWarning("Unauthorized audit log access attempt by {User}", User.Identity?.Name);
                return Forbid();
            }

            // PERFORMANCE: Limit max records per request
            limit = Math.Min(limit, 200);

            // Build query
            var query = _masterContext.AuditLogs
                .OrderByDescending(a => a.PerformedAt)
                .AsQueryable();

            // Apply filters
            if (tenantId.HasValue)
            {
                query = query.Where(a => a.TenantId == tenantId.Value);
            }

            if (!string.IsNullOrEmpty(severity) && Enum.TryParse<AuditSeverity>(severity, true, out var severityEnum))
            {
                query = query.Where(a => a.Severity == severityEnum);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.PerformedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.PerformedAt <= endDate.Value);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Get paginated results
            var logs = await query
                .Skip(offset)
                .Take(limit)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    PerformedAt = a.PerformedAt,
                    TenantId = a.TenantId,
                    TenantName = a.TenantName,
                    UserId = a.UserId,
                    UserEmail = a.UserEmail,
                    UserFullName = a.UserFullName,
                    UserRole = a.UserRole,
                    ActionType = a.ActionType.ToString(),
                    Category = a.Category.ToString(),
                    Severity = a.Severity.ToString(),
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    Success = a.Success,
                    ErrorMessage = a.ErrorMessage,
                    IpAddress = a.IpAddress,
                    HttpMethod = a.HttpMethod,
                    RequestPath = a.RequestPath,
                    ResponseCode = a.ResponseCode
                })
                .ToListAsync();

            var response = new PaginatedAuditLogResponse
            {
                Data = logs,
                TotalCount = totalCount,
                Limit = limit,
                Offset = offset,
                HasMore = offset + limit < totalCount
            };

            _logger.LogInformation("Retrieved {Count}/{TotalCount} audit logs for SuperAdmin {User}",
                logs.Count, totalCount, User.Identity?.Name);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit logs");
            return StatusCode(500, new { error = "Failed to load audit logs" });
        }
    }

    /// <summary>
    /// Get audit log statistics
    /// SECURITY: SuperAdmin only - double-checked at runtime
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AuditLogStatisticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] int days = 30)
    {
        try
        {
            // SECURITY: Additional runtime check
            if (!User.IsInRole("SuperAdmin"))
            {
                _logger.LogWarning("Unauthorized audit statistics access by {User}", User.Identity?.Name);
                return Forbid();
            }

            var startDate = DateTime.UtcNow.AddDays(-days);

            // Get statistics
            var totalLogs = await _masterContext.AuditLogs
                .CountAsync(a => a.PerformedAt >= startDate);

            var failedActions = await _masterContext.AuditLogs
                .CountAsync(a => a.PerformedAt >= startDate && !a.Success);

            var criticalEvents = await _masterContext.AuditLogs
                .CountAsync(a => a.PerformedAt >= startDate && a.Severity == AuditSeverity.CRITICAL);

            var highSeverityEvents = await _masterContext.AuditLogs
                .CountAsync(a => a.PerformedAt >= startDate && a.Severity == AuditSeverity.WARNING);

            // Get top users by activity
            var topUsers = await _masterContext.AuditLogs
                .Where(a => a.PerformedAt >= startDate && a.UserEmail != null)
                .GroupBy(a => new { a.UserEmail, a.UserFullName })
                .Select(g => new
                {
                    g.Key.UserEmail,
                    g.Key.UserFullName,
                    Count = g.Count()
                })
                .OrderByDescending(u => u.Count)
                .Take(5)
                .ToListAsync();

            // Get activity by category
            var categoryBreakdown = await _masterContext.AuditLogs
                .Where(a => a.PerformedAt >= startDate)
                .GroupBy(a => a.Category)
                .Select(g => new
                {
                    Category = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            var statistics = new AuditLogStatisticsResponse
            {
                TotalLogs = totalLogs,
                FailedActions = failedActions,
                CriticalEvents = criticalEvents,
                HighSeverityEvents = highSeverityEvents,
                FailureRate = totalLogs > 0 ? Math.Round((double)failedActions / totalLogs * 100, 2) : 0,
                TopUsersByActivity = topUsers.Select(u => new UserActivity
                {
                    UserEmail = u.UserEmail!,
                    UserFullName = u.UserFullName,
                    ActionCount = u.Count
                }).ToList(),
                CategoryBreakdown = categoryBreakdown.ToDictionary(c => c.Category, c => c.Count),
                PeriodDays = days,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Generated audit statistics: {TotalLogs} logs over {Days} days",
                totalLogs, days);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate audit statistics");
            return StatusCode(500, new { error = "Failed to load audit statistics" });
        }
    }
}

// ═══════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════

public class AuditLogDto
{
    public Guid Id { get; set; }
    public DateTime PerformedAt { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFullName { get; set; }
    public string? UserRole { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? IpAddress { get; set; }
    public string? HttpMethod { get; set; }
    public string? RequestPath { get; set; }
    public int? ResponseCode { get; set; }
}

public class PaginatedAuditLogResponse
{
    public List<AuditLogDto> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public bool HasMore { get; set; }
}

public class AuditLogStatisticsResponse
{
    public int TotalLogs { get; set; }
    public int FailedActions { get; set; }
    public int CriticalEvents { get; set; }
    public int HighSeverityEvents { get; set; }
    public double FailureRate { get; set; }
    public List<UserActivity> TopUsersByActivity { get; set; } = new();
    public Dictionary<string, int> CategoryBreakdown { get; set; } = new();
    public int PeriodDays { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class UserActivity
{
    public string UserEmail { get; set; } = string.Empty;
    public string? UserFullName { get; set; }
    public int ActionCount { get; set; }
}
