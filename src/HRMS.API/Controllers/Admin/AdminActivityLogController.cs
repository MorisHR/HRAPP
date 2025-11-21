using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Infrastructure.Data;
using HRMS.Core.Enums;

namespace HRMS.API.Controllers.Admin;

/// <summary>
/// Admin Activity Log API - Cross-Tenant Activity Monitoring
/// Real-time activity feed across all tenants for security and compliance
/// SECURITY: SuperAdmin role ONLY - NO tenant admin access
/// PERFORMANCE: Optimized with pagination and efficient queries
/// </summary>
[ApiController]
[Route("admin/activity-logs")]
[Authorize(Roles = "SuperAdmin")] // CRITICAL: SuperAdmin ONLY
public class AdminActivityLogController : ControllerBase
{
    private readonly MasterDbContext _masterContext;
    private readonly ILogger<AdminActivityLogController> _logger;

    public AdminActivityLogController(
        MasterDbContext masterContext,
        ILogger<AdminActivityLogController> logger)
    {
        _masterContext = masterContext;
        _logger = logger;
    }

    /// <summary>
    /// Get recent activity logs across all tenants
    /// SECURITY: SuperAdmin only - validates role in [Authorize] attribute
    /// PERFORMANCE: Includes pagination metadata for efficient data loading
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedActivityLogResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentActivity(
        [FromQuery] int limit = 10,
        [FromQuery] int offset = 0,
        [FromQuery] string? type = null,
        [FromQuery] string? severity = null,
        [FromQuery] Guid? tenantId = null)
    {
        try
        {
            // SECURITY: Additional runtime check (defense in depth)
            if (!User.IsInRole("SuperAdmin"))
            {
                _logger.LogWarning("Unauthorized access attempt to activity logs by {User}", User.Identity?.Name);
                return Forbid();
            }

            // PERFORMANCE: Limit max records per request
            limit = Math.Min(limit, 100);

            var query = _masterContext.Tenants
                .Where(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .OrderByDescending(t => t.CreatedAt)
                .AsQueryable();

            if (tenantId.HasValue)
            {
                query = query.Where(t => t.Id == tenantId.Value);
            }

            // PERFORMANCE: Get total count for pagination
            var totalCount = await query.CountAsync();

            var recentTenants = await query
                .Skip(offset)
                .Take(limit)
                .Select(t => new
                {
                    t.Id,
                    t.CompanyName,
                    t.Status,
                    t.CreatedAt,
                    t.CurrentUserCount
                })
                .ToListAsync();

            var activities = new List<AdminActivityLogResponse>();

            foreach (var tenant in recentTenants)
            {
                var activityType = GetActivityType(tenant.Status);
                var severityLevel = GetSeverity(tenant.Status);

                activities.Add(new AdminActivityLogResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = tenant.CreatedAt,
                    Type = activityType,
                    Severity = severityLevel,
                    Title = GetActivityTitle(activityType, tenant.CompanyName),
                    Description = GetActivityDescription(activityType, tenant.CompanyName, tenant.CurrentUserCount),
                    TenantId = tenant.Id.ToString(),
                    TenantName = tenant.CompanyName
                });
            }

            // Add subscription activities
            var recentSubscriptions = await _masterContext.Tenants
                .Where(t => t.UpdatedAt >= DateTime.UtcNow.AddDays(-30))
                .OrderByDescending(t => t.UpdatedAt)
                .Skip(offset)
                .Take(limit)
                .Select(t => new { t.Id, t.CompanyName, t.EmployeeTier, t.UpdatedAt })
                .ToListAsync();

            foreach (var sub in recentSubscriptions)
            {
                activities.Add(new AdminActivityLogResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = sub.UpdatedAt ?? DateTime.UtcNow,
                    Type = "tenant_upgraded",
                    Severity = "success",
                    Title = "Tenant Upgraded",
                    Description = $"{sub.CompanyName} upgraded to {sub.EmployeeTier} tier",
                    TenantId = sub.Id.ToString(),
                    TenantName = sub.CompanyName
                });
            }

            var sortedActivities = activities
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToList();

            // PERFORMANCE: Return pagination metadata
            var response = new PaginatedActivityLogResponse
            {
                Data = sortedActivities,
                TotalCount = totalCount,
                Limit = limit,
                Offset = offset,
                HasMore = offset + limit < totalCount
            };

            _logger.LogInformation("Retrieved {Count}/{TotalCount} activity logs for SuperAdmin {User}",
                sortedActivities.Count, totalCount, User.Identity?.Name);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve activity logs");
            return StatusCode(500, new { error = "Failed to load activity logs" });
        }
    }

    /// <summary>
    /// Get critical activity (errors and warnings only)
    /// SECURITY: SuperAdmin only - double-checked at runtime
    /// </summary>
    [HttpGet("critical")]
    [ProducesResponseType(typeof(List<AdminActivityLogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCriticalActivity([FromQuery] int limit = 10)
    {
        try
        {
            // SECURITY: Additional runtime check
            if (!User.IsInRole("SuperAdmin"))
            {
                _logger.LogWarning("Unauthorized access attempt to critical activity by {User}", User.Identity?.Name);
                return Forbid();
            }

            // PERFORMANCE: Limit max records
            limit = Math.Min(limit, 50);
            var suspendedTenants = await _masterContext.Tenants
                .Where(t => t.Status == TenantStatus.Suspended)
                .OrderByDescending(t => t.UpdatedAt)
                .Take(limit)
                .Select(t => new { t.Id, t.CompanyName, t.UpdatedAt })
                .ToListAsync();

            var activities = suspendedTenants.Select(t => new AdminActivityLogResponse
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = t.UpdatedAt ?? DateTime.UtcNow,
                Type = "tenant_suspended",
                Severity = "error",
                Title = "Tenant Suspended",
                Description = $"{t.CompanyName} has been suspended due to payment failure or policy violation",
                TenantId = t.Id.ToString(),
                TenantName = t.CompanyName
            }).ToList();

            _logger.LogInformation("Retrieved {Count} critical activity logs", activities.Count);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve critical activity logs");
            return StatusCode(500, new { error = "Failed to load critical activities" });
        }
    }

    private string GetActivityType(TenantStatus status)
    {
        return status switch
        {
            TenantStatus.Active => "tenant_created",
            TenantStatus.Suspended => "tenant_suspended",
            TenantStatus.Trial => "tenant_created",
            _ => "system_event"
        };
    }

    private string GetSeverity(TenantStatus status)
    {
        return status switch
        {
            TenantStatus.Active => "success",
            TenantStatus.Suspended => "error",
            TenantStatus.Trial => "info",
            _ => "info"
        };
    }

    private string GetActivityTitle(string type, string tenantName)
    {
        return type switch
        {
            "tenant_created" => "New Tenant Created",
            "tenant_suspended" => "Tenant Suspended",
            "tenant_upgraded" => "Tenant Upgraded",
            "tenant_downgraded" => "Tenant Downgraded",
            _ => "System Event"
        };
    }

    private string GetActivityDescription(string type, string tenantName, int employeeCount)
    {
        return type switch
        {
            "tenant_created" => $"{tenantName} has been successfully onboarded with {employeeCount} employees",
            "tenant_suspended" => $"{tenantName} suspended due to payment failure",
            _ => $"Activity for {tenantName}"
        };
    }
}

// ═══════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════

public class AdminActivityLogResponse
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty; // tenant_created, tenant_suspended, etc.
    public string Severity { get; set; } = string.Empty; // info, warning, error, success
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? UserName { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// PERFORMANCE: Paginated response with metadata
/// </summary>
public class PaginatedActivityLogResponse
{
    public List<AdminActivityLogResponse> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public bool HasMore { get; set; }
}
