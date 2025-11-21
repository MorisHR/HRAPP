using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using HRMS.Infrastructure.Data;
using HRMS.Core.Enums;
using System.Text.Json;

namespace HRMS.API.Controllers.Admin;

/// <summary>
/// Admin Metrics API - Historical Data for Charts and Analytics
/// Fortune 500-grade metrics with caching and performance optimization
/// SECURITY: SuperAdmin role ONLY - NO tenant admin access
/// PERFORMANCE: Cached responses with efficient aggregation queries
/// </summary>
[ApiController]
[Route("admin/metrics")]
[Authorize(Roles = "SuperAdmin")] // CRITICAL: SuperAdmin ONLY
public class AdminMetricsController : ControllerBase
{
    private readonly MasterDbContext _masterContext;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AdminMetricsController> _logger;
    private const int CACHE_DURATION_SECONDS = 300; // 5 minutes

    public AdminMetricsController(
        MasterDbContext masterContext,
        IDistributedCache cache,
        ILogger<AdminMetricsController> logger)
    {
        _masterContext = masterContext;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get tenant growth data for charts
    /// Returns monthly tenant creation/activation data
    /// </summary>
    [HttpGet("tenant-growth")]
    [ProducesResponseType(typeof(List<TenantGrowthDataPoint>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantGrowthData([FromQuery] int months = 6)
    {
        try
        {
            // SECURITY: Runtime check (defense in depth)
            if (!User.IsInRole("SuperAdmin"))
            {
                _logger.LogWarning("Unauthorized tenant growth access by {User}", User.Identity?.Name);
                return Forbid();
            }

            // PERFORMANCE: Limit max months
            months = Math.Min(months, 12);
            var cacheKey = $"metrics:tenant-growth:{months}";

            // Try cache first
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cached = JsonSerializer.Deserialize<List<TenantGrowthDataPoint>>(cachedData);
                _logger.LogInformation("Returning cached tenant growth data for {Months} months", months);
                return Ok(cached);
            }

            var now = DateTime.UtcNow;
            var startDate = now.AddMonths(-months).Date;

            // PERFORMANCE: Calculate cumulative totals for each month
            var result = new List<TenantGrowthDataPoint>();
            for (int i = months - 1; i >= 0; i--)
            {
                var period = now.AddMonths(-i);
                var periodStart = new DateTime(period.Year, period.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var periodEnd = new DateTime(period.Year, period.Month, DateTime.DaysInMonth(period.Year, period.Month), 23, 59, 59, DateTimeKind.Utc);

                // Count all tenants created up to end of this month
                var totalTenants = await _masterContext.Tenants
                    .CountAsync(t => t.CreatedAt <= periodEnd);

                var activeTenants = await _masterContext.Tenants
                    .CountAsync(t => t.CreatedAt <= periodEnd && t.Status == TenantStatus.Active);

                // New tenants created during this month
                var newTenants = await _masterContext.Tenants
                    .CountAsync(t => t.CreatedAt >= periodStart && t.CreatedAt <= periodEnd);

                // Churned tenants (simplified - tenants that became inactive/suspended this month)
                // Note: This is an approximation as we don't track status change history
                var churnedTenants = await _masterContext.Tenants
                    .CountAsync(t => t.CreatedAt < periodStart &&
                                    (t.Status == TenantStatus.Suspended || t.Status == TenantStatus.SoftDeleted || t.Status == TenantStatus.Expired) &&
                                    t.UpdatedAt >= periodStart && t.UpdatedAt <= periodEnd);

                result.Add(new TenantGrowthDataPoint
                {
                    Period = periodStart,
                    TotalTenants = totalTenants,
                    ActiveTenants = activeTenants,
                    NewTenants = newTenants,
                    ChurnedTenants = churnedTenants
                });
            }

            // Cache for 5 minutes
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CACHE_DURATION_SECONDS)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), cacheOptions);

            _logger.LogInformation("Generated tenant growth data: {Months} months, {DataPoints} points",
                months, result.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate tenant growth data");
            return StatusCode(500, new { error = "Failed to load tenant growth data" });
        }
    }

    /// <summary>
    /// Get revenue growth data for charts
    /// Returns monthly recurring revenue (MRR) trends
    /// </summary>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(List<RevenueDataPoint>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueData([FromQuery] int months = 6)
    {
        try
        {
            // SECURITY: Runtime check
            if (!User.IsInRole("SuperAdmin"))
            {
                _logger.LogWarning("Unauthorized revenue data access by {User}", User.Identity?.Name);
                return Forbid();
            }

            // PERFORMANCE: Limit max months
            months = Math.Min(months, 12);
            var cacheKey = $"metrics:revenue:{months}";

            // Try cache first
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cached = JsonSerializer.Deserialize<List<RevenueDataPoint>>(cachedData);
                _logger.LogInformation("Returning cached revenue data for {Months} months", months);
                return Ok(cached);
            }

            var now = DateTime.UtcNow;
            var result = new List<RevenueDataPoint>();

            // Generate monthly data
            for (int i = months - 1; i >= 0; i--)
            {
                var period = now.AddMonths(-i);
                var periodStart = new DateTime(period.Year, period.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var periodEnd = periodStart.AddMonths(1);

                // Calculate MRR for tenants active during this period
                var totalRevenue = await _masterContext.Tenants
                    .Where(t => t.CreatedAt < periodEnd &&
                               (t.Status == TenantStatus.Active || t.Status == TenantStatus.Trial) &&
                               (!t.SubscriptionEndDate.HasValue || t.SubscriptionEndDate.Value >= periodStart))
                    .SumAsync(t => t.YearlyPriceMUR / 12);

                // Recurring revenue from existing active tenants (created before this period)
                var recurringRevenue = await _masterContext.Tenants
                    .Where(t => t.CreatedAt < periodStart &&
                               t.Status == TenantStatus.Active &&
                               (!t.SubscriptionEndDate.HasValue || t.SubscriptionEndDate.Value >= periodStart))
                    .SumAsync(t => t.YearlyPriceMUR / 12);

                // New revenue from tenants created during this period
                var newRevenue = await _masterContext.Tenants
                    .Where(t => t.CreatedAt >= periodStart && t.CreatedAt < periodEnd &&
                               (t.Status == TenantStatus.Active || t.Status == TenantStatus.Trial))
                    .SumAsync(t => t.YearlyPriceMUR / 12);

                // Churned revenue (tenants that were active before but suspended/expired this period)
                // Simplified approximation as we don't track historical revenue
                var churnRevenue = await _masterContext.Tenants
                    .Where(t => t.CreatedAt < periodStart &&
                               (t.Status == TenantStatus.Suspended || t.Status == TenantStatus.SoftDeleted || t.Status == TenantStatus.Expired) &&
                               t.UpdatedAt >= periodStart && t.UpdatedAt < periodEnd)
                    .SumAsync(t => t.YearlyPriceMUR / 12);

                result.Add(new RevenueDataPoint
                {
                    Period = periodStart,
                    TotalRevenue = totalRevenue,
                    RecurringRevenue = recurringRevenue,
                    NewRevenue = newRevenue,
                    ChurnRevenue = churnRevenue
                });
            }

            // Cache for 5 minutes
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CACHE_DURATION_SECONDS)
            };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), cacheOptions);

            _logger.LogInformation("Generated revenue data: {Months} months, MRR: {MRR:C} MUR",
                months, result.LastOrDefault()?.TotalRevenue ?? 0);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate revenue data");
            return StatusCode(500, new { error = "Failed to load revenue data" });
        }
    }

    /// <summary>
    /// Get employee growth data for charts
    /// Returns total employees across all tenants over time
    /// </summary>
    [HttpGet("employee-growth")]
    [ProducesResponseType(typeof(List<EmployeeGrowthDataPoint>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployeeGrowthData([FromQuery] int months = 6)
    {
        try
        {
            // SECURITY: Runtime check
            if (!User.IsInRole("SuperAdmin"))
            {
                _logger.LogWarning("Unauthorized employee growth access by {User}", User.Identity?.Name);
                return Forbid();
            }

            months = Math.Min(months, 12);
            var now = DateTime.UtcNow;
            var result = new List<EmployeeGrowthDataPoint>();

            // NOTE: This is simplified - ideally we'd have historical employee count snapshots
            // For now, we'll show current employee counts as if they were constant
            var currentEmployees = await _masterContext.Tenants
                .Where(t => t.Status == TenantStatus.Active)
                .SumAsync(t => t.CurrentUserCount);

            for (int i = months - 1; i >= 0; i--)
            {
                var period = now.AddMonths(-i);

                result.Add(new EmployeeGrowthDataPoint
                {
                    Period = new DateTime(period.Year, period.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    TotalEmployees = currentEmployees
                });
            }

            _logger.LogInformation("Generated employee growth data: {Months} months", months);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate employee growth data");
            return StatusCode(500, new { error = "Failed to load employee growth data" });
        }
    }
}

// ═══════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════

public class TenantGrowthDataPoint
{
    public DateTime Period { get; set; }
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int NewTenants { get; set; }
    public int ChurnedTenants { get; set; }
}

public class RevenueDataPoint
{
    public DateTime Period { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RecurringRevenue { get; set; }
    public decimal NewRevenue { get; set; }
    public decimal ChurnRevenue { get; set; }
}

public class EmployeeGrowthDataPoint
{
    public DateTime Period { get; set; }
    public int TotalEmployees { get; set; }
}
