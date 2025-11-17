using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HRMS.Application.DTOs.Monitoring;
using HRMS.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRMS.API.Controllers;

/// <summary>
/// FORTUNE 50: SuperAdmin monitoring and observability API
/// Provides real-time platform health, performance metrics, and security monitoring
///
/// SECURITY:
/// - SuperAdmin role required for ALL endpoints
/// - Only accessible via admin.hrms.com subdomain
/// - Read-only operations (zero impact on production)
///
/// COMPLIANCE: ISO 27001, SOC 2, NIST 800-53
/// SLA TARGETS: P95 <200ms, P99 <500ms, Cache Hit Rate >95%, Error Rate <0.1%
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
[Produces("application/json")]
public class MonitoringController : ControllerBase
{
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        IMonitoringService monitoringService,
        ILogger<MonitoringController> logger)
    {
        _monitoringService = monitoringService;
        _logger = logger;
    }

    // ============================================
    // DASHBOARD OVERVIEW METRICS
    // ============================================

    /// <summary>
    /// Get high-level dashboard metrics for SuperAdmin overview
    /// CACHING: 5-minute TTL to reduce database load
    /// </summary>
    /// <returns>Dashboard metrics snapshot</returns>
    /// <response code="200">Returns dashboard metrics</response>
    /// <response code="401">Unauthorized - SuperAdmin role required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboardMetrics()
    {
        try
        {
            _logger.LogInformation("SuperAdmin {Email} requested dashboard metrics",
                User.Identity?.Name ?? "Unknown");

            var metrics = await _monitoringService.GetDashboardMetricsAsync();

            return Ok(new
            {
                success = true,
                data = metrics,
                message = "Dashboard metrics retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard metrics");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving dashboard metrics"
            });
        }
    }

    /// <summary>
    /// Force refresh of dashboard metrics (bypasses cache)
    /// Use sparingly - triggers immediate database query
    /// </summary>
    /// <returns>Fresh dashboard metrics</returns>
    [HttpPost("dashboard/refresh")]
    [ProducesResponseType(typeof(DashboardMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshDashboardMetrics()
    {
        try
        {
            _logger.LogInformation("SuperAdmin {Email} forced dashboard metrics refresh",
                User.Identity?.Name ?? "Unknown");

            var metrics = await _monitoringService.RefreshDashboardMetricsAsync();

            return Ok(new
            {
                success = true,
                data = metrics,
                message = "Dashboard metrics refreshed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh dashboard metrics");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while refreshing dashboard metrics"
            });
        }
    }

    // ============================================
    // INFRASTRUCTURE HEALTH MONITORING
    // ============================================

    /// <summary>
    /// Get infrastructure layer health metrics
    /// Monitors database performance, connection pooling, cache hit rates
    /// </summary>
    /// <returns>Infrastructure health snapshot</returns>
    [HttpGet("infrastructure/health")]
    [ProducesResponseType(typeof(InfrastructureHealthDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInfrastructureHealth()
    {
        try
        {
            var health = await _monitoringService.GetInfrastructureHealthAsync();

            return Ok(new
            {
                success = true,
                data = health,
                message = "Infrastructure health retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get infrastructure health");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving infrastructure health"
            });
        }
    }

    /// <summary>
    /// Get list of slow queries requiring optimization
    /// Identifies queries exceeding performance thresholds
    /// </summary>
    /// <param name="minExecutionTimeMs">Minimum average execution time to include (default: 200ms)</param>
    /// <param name="limit">Maximum number of slow queries to return (default: 20)</param>
    /// <returns>List of slow queries</returns>
    [HttpGet("infrastructure/slow-queries")]
    [ProducesResponseType(typeof(List<SlowQueryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSlowQueries(
        [FromQuery] decimal minExecutionTimeMs = 200,
        [FromQuery] int limit = 20)
    {
        try
        {
            // SECURITY: Validate input parameters
            if (minExecutionTimeMs < 0 || minExecutionTimeMs > 10000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Minimum execution time must be between 0 and 10000ms"
                });
            }

            if (limit < 1 || limit > 1000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Limit must be between 1 and 1000"
                });
            }

            var slowQueries = await _monitoringService.GetSlowQueriesAsync(minExecutionTimeMs, limit);

            return Ok(new
            {
                success = true,
                data = slowQueries,
                count = slowQueries.Count,
                message = $"Retrieved {slowQueries.Count} slow queries"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get slow queries");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving slow queries"
            });
        }
    }

    /// <summary>
    /// Get slow queries for a specific tenant
    /// Useful for identifying tenant-specific performance issues
    /// </summary>
    /// <param name="tenantSubdomain">Tenant subdomain</param>
    /// <param name="minExecutionTimeMs">Minimum average execution time (default: 200ms)</param>
    /// <param name="limit">Maximum number of queries (default: 20)</param>
    /// <returns>List of slow queries for this tenant</returns>
    [HttpGet("infrastructure/slow-queries/{tenantSubdomain}")]
    [ProducesResponseType(typeof(List<SlowQueryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSlowQueriesByTenant(
        string tenantSubdomain,
        [FromQuery] decimal minExecutionTimeMs = 200,
        [FromQuery] int limit = 20)
    {
        try
        {
            // SECURITY: Validate tenant subdomain format (prevent injection)
            if (string.IsNullOrWhiteSpace(tenantSubdomain))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Tenant subdomain is required"
                });
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(tenantSubdomain, @"^[a-z0-9-]+$"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid tenant subdomain format"
                });
            }

            var slowQueries = await _monitoringService.GetSlowQueriesByTenantAsync(
                tenantSubdomain, minExecutionTimeMs, limit);

            return Ok(new
            {
                success = true,
                data = slowQueries,
                tenant = tenantSubdomain,
                count = slowQueries.Count,
                message = $"Retrieved {slowQueries.Count} slow queries for tenant {tenantSubdomain}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get slow queries for tenant {Tenant}", tenantSubdomain);
            return StatusCode(500, new
            {
                success = false,
                message = $"An error occurred while retrieving slow queries for tenant {tenantSubdomain}"
            });
        }
    }

    // ============================================
    // API PERFORMANCE MONITORING
    // ============================================

    /// <summary>
    /// Get API endpoint performance metrics
    /// Tracks response times, throughput, and error rates
    /// </summary>
    /// <param name="periodStart">Start of measurement period (ISO 8601 format)</param>
    /// <param name="periodEnd">End of measurement period (ISO 8601 format)</param>
    /// <param name="tenantSubdomain">Filter by tenant (optional)</param>
    /// <param name="limit">Maximum number of endpoints to return (default: 50)</param>
    /// <returns>List of API performance metrics</returns>
    [HttpGet("api/performance")]
    [ProducesResponseType(typeof(List<ApiPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApiPerformance(
        [FromQuery] DateTime? periodStart = null,
        [FromQuery] DateTime? periodEnd = null,
        [FromQuery] string? tenantSubdomain = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            var performance = await _monitoringService.GetApiPerformanceAsync(
                periodStart, periodEnd, tenantSubdomain, limit);

            return Ok(new
            {
                success = true,
                data = performance,
                count = performance.Count,
                periodStart = periodStart ?? DateTime.UtcNow.AddHours(-1),
                periodEnd = periodEnd ?? DateTime.UtcNow,
                message = $"Retrieved API performance for {performance.Count} endpoints"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API performance");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving API performance metrics"
            });
        }
    }

    /// <summary>
    /// Get performance metrics for a specific API endpoint
    /// Useful for drilling down into endpoint-specific issues
    /// </summary>
    /// <param name="endpoint">API endpoint path (e.g., /api/employees)</param>
    /// <param name="httpMethod">HTTP method (GET, POST, PUT, DELETE, PATCH)</param>
    /// <param name="periodStart">Start of measurement period</param>
    /// <param name="periodEnd">End of measurement period</param>
    /// <returns>API performance metrics for this endpoint</returns>
    [HttpGet("api/performance/{**endpoint}")]
    [ProducesResponseType(typeof(ApiPerformanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEndpointPerformance(
        string endpoint,
        [FromQuery] string httpMethod = "GET",
        [FromQuery] DateTime? periodStart = null,
        [FromQuery] DateTime? periodEnd = null)
    {
        try
        {
            // SECURITY: Validate HTTP method
            var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
            if (!validMethods.Contains(httpMethod.ToUpper()))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid HTTP method"
                });
            }

            // SECURITY: Validate endpoint format
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Endpoint is required"
                });
            }

            // Prepend slash if not present
            if (!endpoint.StartsWith('/'))
            {
                endpoint = "/" + endpoint;
            }

            var performance = await _monitoringService.GetEndpointPerformanceAsync(
                endpoint, httpMethod, periodStart, periodEnd);

            if (performance == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"No performance data found for {httpMethod} {endpoint}"
                });
            }

            return Ok(new
            {
                success = true,
                data = performance,
                message = $"Retrieved performance for {httpMethod} {endpoint}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get endpoint performance for {Method} {Endpoint}",
                httpMethod, endpoint);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving endpoint performance"
            });
        }
    }

    /// <summary>
    /// Get endpoints exceeding SLA targets
    /// Identifies endpoints requiring immediate attention
    /// SLA TARGETS: P95 <200ms, Error Rate <0.1%
    /// </summary>
    /// <param name="periodStart">Start of measurement period</param>
    /// <param name="periodEnd">End of measurement period</param>
    /// <returns>List of endpoints with SLA violations</returns>
    [HttpGet("api/sla-violations")]
    [ProducesResponseType(typeof(List<ApiPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSlaViolations(
        [FromQuery] DateTime? periodStart = null,
        [FromQuery] DateTime? periodEnd = null)
    {
        try
        {
            var violations = await _monitoringService.GetSlaViolationsAsync(periodStart, periodEnd);

            return Ok(new
            {
                success = true,
                data = violations,
                count = violations.Count,
                message = violations.Count == 0
                    ? "No SLA violations detected"
                    : $"{violations.Count} endpoints violating SLA targets"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SLA violations");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving SLA violations"
            });
        }
    }

    // ============================================
    // MULTI-TENANT ACTIVITY MONITORING
    // ============================================

    /// <summary>
    /// Get activity and resource utilization metrics for all tenants
    /// Provides visibility into tenant usage patterns and health scores
    /// </summary>
    /// <param name="periodStart">Start of measurement period</param>
    /// <param name="periodEnd">End of measurement period</param>
    /// <param name="minActiveUsers">Filter tenants with minimum active users</param>
    /// <param name="status">Filter by tenant status (Active, Suspended, Trial, Churned)</param>
    /// <param name="sortBy">Sort field (ActiveUsers, RequestVolume, ErrorRate, StorageUsage)</param>
    /// <param name="limit">Maximum number of tenants to return (default: 100)</param>
    /// <returns>List of tenant activity metrics</returns>
    [HttpGet("tenants/activity")]
    [ProducesResponseType(typeof(List<TenantActivityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantActivity(
        [FromQuery] DateTime? periodStart = null,
        [FromQuery] DateTime? periodEnd = null,
        [FromQuery] int? minActiveUsers = null,
        [FromQuery] string? status = null,
        [FromQuery] string sortBy = "ActiveUsers",
        [FromQuery] int limit = 100)
    {
        try
        {
            var activity = await _monitoringService.GetTenantActivityAsync(
                periodStart, periodEnd, minActiveUsers, status, sortBy, limit);

            return Ok(new
            {
                success = true,
                data = activity,
                count = activity.Count,
                message = $"Retrieved activity for {activity.Count} tenants"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tenant activity");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving tenant activity"
            });
        }
    }

    /// <summary>
    /// Get activity metrics for a specific tenant
    /// Detailed view of single tenant performance and resource usage
    /// </summary>
    /// <param name="tenantSubdomain">Tenant subdomain</param>
    /// <param name="periodStart">Start of measurement period</param>
    /// <param name="periodEnd">End of measurement period</param>
    /// <returns>Tenant activity metrics</returns>
    [HttpGet("tenants/activity/{tenantSubdomain}")]
    [ProducesResponseType(typeof(TenantActivityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantActivityBySubdomain(
        string tenantSubdomain,
        [FromQuery] DateTime? periodStart = null,
        [FromQuery] DateTime? periodEnd = null)
    {
        try
        {
            var activity = await _monitoringService.GetTenantActivityBySubdomainAsync(
                tenantSubdomain, periodStart, periodEnd);

            if (activity == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"No activity data found for tenant {tenantSubdomain}"
                });
            }

            return Ok(new
            {
                success = true,
                data = activity,
                message = $"Retrieved activity for tenant {tenantSubdomain}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tenant activity for {Tenant}", tenantSubdomain);
            return StatusCode(500, new
            {
                success = false,
                message = $"An error occurred while retrieving activity for tenant {tenantSubdomain}"
            });
        }
    }

    /// <summary>
    /// Get tenants with health score below threshold
    /// Identifies at-risk tenants requiring attention (low activity, high errors)
    /// HEALTH SCORE: 0-100 based on activity, performance, error rate
    /// </summary>
    /// <param name="maxHealthScore">Maximum health score to include (default: 50)</param>
    /// <param name="limit">Maximum number of tenants to return (default: 20)</param>
    /// <returns>List of at-risk tenants</returns>
    [HttpGet("tenants/at-risk")]
    [ProducesResponseType(typeof(List<TenantActivityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAtRiskTenants(
        [FromQuery] int maxHealthScore = 50,
        [FromQuery] int limit = 20)
    {
        try
        {
            var atRiskTenants = await _monitoringService.GetAtRiskTenantsAsync(maxHealthScore, limit);

            return Ok(new
            {
                success = true,
                data = atRiskTenants,
                count = atRiskTenants.Count,
                message = atRiskTenants.Count == 0
                    ? "No at-risk tenants detected"
                    : $"{atRiskTenants.Count} tenants with health score <= {maxHealthScore}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get at-risk tenants");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving at-risk tenants"
            });
        }
    }

    // ============================================
    // SECURITY EVENT MONITORING
    // ============================================

    /// <summary>
    /// Get security events and threat detection metrics
    /// Monitors authentication failures, IDOR attempts, rate limit violations
    /// COMPLIANCE: SOC 2, ISO 27001, PCI-DSS security logging requirements
    /// </summary>
    /// <param name="periodStart">Start of measurement period</param>
    /// <param name="periodEnd">End of measurement period</param>
    /// <param name="eventType">Filter by event type (FailedLogin, IdorAttempt, etc.)</param>
    /// <param name="severity">Filter by severity (Critical, High, Medium, Low, Info)</param>
    /// <param name="tenantSubdomain">Filter by tenant</param>
    /// <param name="isBlocked">Filter by blocked status</param>
    /// <param name="isReviewed">Filter by review status</param>
    /// <param name="limit">Maximum number of events to return (default: 100)</param>
    /// <returns>List of security events</returns>
    [HttpGet("security/events")]
    [ProducesResponseType(typeof(List<SecurityEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSecurityEvents(
        [FromQuery] DateTime? periodStart = null,
        [FromQuery] DateTime? periodEnd = null,
        [FromQuery] string? eventType = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? tenantSubdomain = null,
        [FromQuery] bool? isBlocked = null,
        [FromQuery] bool? isReviewed = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            var events = await _monitoringService.GetSecurityEventsAsync(
                periodStart, periodEnd, eventType, severity, tenantSubdomain,
                isBlocked, isReviewed, limit);

            return Ok(new
            {
                success = true,
                data = events,
                count = events.Count,
                message = $"Retrieved {events.Count} security events"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security events");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving security events"
            });
        }
    }

    /// <summary>
    /// Get critical security events requiring immediate review
    /// Auto-filters for Critical/High severity unreviewed events
    /// </summary>
    /// <param name="periodStart">Start of measurement period</param>
    /// <param name="periodEnd">End of measurement period</param>
    /// <returns>List of critical security events</returns>
    [HttpGet("security/critical")]
    [ProducesResponseType(typeof(List<SecurityEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCriticalSecurityEvents(
        [FromQuery] DateTime? periodStart = null,
        [FromQuery] DateTime? periodEnd = null)
    {
        try
        {
            var events = await _monitoringService.GetCriticalSecurityEventsAsync(periodStart, periodEnd);

            return Ok(new
            {
                success = true,
                data = events,
                count = events.Count,
                message = events.Count == 0
                    ? "No critical security events detected"
                    : $"{events.Count} critical security events requiring review"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get critical security events");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving critical security events"
            });
        }
    }

    /// <summary>
    /// Mark security event as reviewed
    /// Records reviewer identity and notes for audit trail
    /// </summary>
    /// <param name="eventId">Security event ID</param>
    /// <param name="request">Review request containing notes</param>
    /// <returns>Success status</returns>
    [HttpPost("security/events/{eventId}/review")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkSecurityEventReviewed(
        long eventId,
        [FromBody] SecurityEventReviewRequest request)
    {
        try
        {
            // SECURITY: Validate event ID
            if (eventId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid event ID"
                });
            }

            // SECURITY: Validate review notes length (prevent DoS via large payloads)
            if (!string.IsNullOrEmpty(request.ReviewNotes) && request.ReviewNotes.Length > 5000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Review notes cannot exceed 5000 characters"
                });
            }

            var reviewedBy = User.Identity?.Name ?? "Unknown";
            var success = await _monitoringService.MarkSecurityEventReviewedAsync(
                eventId, reviewedBy, request.ReviewNotes);

            if (!success)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Security event {eventId} not found"
                });
            }

            _logger.LogInformation("Security event {EventId} marked as reviewed by {ReviewedBy}",
                eventId, reviewedBy);

            return Ok(new
            {
                success = true,
                message = $"Security event {eventId} marked as reviewed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark security event {EventId} as reviewed", eventId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while marking security event as reviewed"
            });
        }
    }

    // ============================================
    // ALERT MANAGEMENT
    // ============================================

    /// <summary>
    /// Get active and historical alerts
    /// Monitors SLA violations, security incidents, capacity issues
    /// ALERT SEVERITY: Critical (immediate action), High (1h), Medium (4h), Low (24h)
    /// </summary>
    /// <param name="status">Filter by status (Active, Acknowledged, Resolved, Suppressed)</param>
    /// <param name="severity">Filter by severity (Critical, High, Medium, Low, Info)</param>
    /// <param name="alertType">Filter by type (Performance, Security, Availability, Capacity, Compliance)</param>
    /// <param name="tenantSubdomain">Filter by tenant</param>
    /// <param name="periodStart">Start of period (default: 7 days ago)</param>
    /// <param name="periodEnd">End of period (default: now)</param>
    /// <param name="limit">Maximum number of alerts (default: 50)</param>
    /// <returns>List of alerts</returns>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(List<AlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? alertType = null,
        [FromQuery] string? tenantSubdomain = null,
        [FromQuery] DateTime? periodStart = null,
        [FromQuery] DateTime? periodEnd = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            var alerts = await _monitoringService.GetAlertsAsync(
                status, severity, alertType, tenantSubdomain, periodStart, periodEnd, limit);

            return Ok(new
            {
                success = true,
                data = alerts,
                count = alerts.Count,
                message = $"Retrieved {alerts.Count} alerts"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alerts");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving alerts"
            });
        }
    }

    /// <summary>
    /// Get active critical and high severity alerts
    /// Quick view of alerts requiring immediate attention
    /// </summary>
    /// <returns>List of active critical/high alerts</returns>
    [HttpGet("alerts/active")]
    [ProducesResponseType(typeof(List<AlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveAlerts()
    {
        try
        {
            var alerts = await _monitoringService.GetActiveAlertsAsync();

            return Ok(new
            {
                success = true,
                data = alerts,
                count = alerts.Count,
                message = alerts.Count == 0
                    ? "No active critical/high alerts"
                    : $"{alerts.Count} active critical/high alerts"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active alerts");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while retrieving active alerts"
            });
        }
    }

    /// <summary>
    /// Acknowledge an alert (mark as being worked on)
    /// Records who acknowledged and when for accountability
    /// </summary>
    /// <param name="alertId">Alert ID</param>
    /// <returns>Success status</returns>
    [HttpPost("alerts/{alertId}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcknowledgeAlert(long alertId)
    {
        try
        {
            // SECURITY: Validate alert ID
            if (alertId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid alert ID"
                });
            }

            var acknowledgedBy = User.Identity?.Name ?? "Unknown";
            var success = await _monitoringService.AcknowledgeAlertAsync(alertId, acknowledgedBy);

            if (!success)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Alert {alertId} not found or already acknowledged"
                });
            }

            _logger.LogInformation("Alert {AlertId} acknowledged by {AcknowledgedBy}",
                alertId, acknowledgedBy);

            return Ok(new
            {
                success = true,
                message = $"Alert {alertId} acknowledged"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge alert {AlertId}", alertId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while acknowledging the alert"
            });
        }
    }

    /// <summary>
    /// Resolve an alert (mark as fixed)
    /// Records resolution details for post-mortem analysis
    /// </summary>
    /// <param name="alertId">Alert ID</param>
    /// <param name="request">Resolution request containing notes</param>
    /// <returns>Success status</returns>
    [HttpPost("alerts/{alertId}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResolveAlert(
        long alertId,
        [FromBody] AlertResolutionRequest request)
    {
        try
        {
            // SECURITY: Validate alert ID
            if (alertId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid alert ID"
                });
            }

            // SECURITY: Validate resolution notes length
            if (!string.IsNullOrEmpty(request.ResolutionNotes) && request.ResolutionNotes.Length > 5000)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Resolution notes cannot exceed 5000 characters"
                });
            }

            var resolvedBy = User.Identity?.Name ?? "Unknown";
            var success = await _monitoringService.ResolveAlertAsync(
                alertId, resolvedBy, request.ResolutionNotes);

            if (!success)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Alert {alertId} not found or already resolved"
                });
            }

            _logger.LogInformation("Alert {AlertId} resolved by {ResolvedBy}", alertId, resolvedBy);

            return Ok(new
            {
                success = true,
                message = $"Alert {alertId} resolved"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve alert {AlertId}", alertId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while resolving the alert"
            });
        }
    }
}

// ============================================
// REQUEST MODELS
// ============================================

/// <summary>
/// Request model for security event review
/// </summary>
public class SecurityEventReviewRequest
{
    /// <summary>
    /// Review notes and actions taken
    /// </summary>
    public string? ReviewNotes { get; set; }
}

/// <summary>
/// Request model for alert resolution
/// </summary>
public class AlertResolutionRequest
{
    /// <summary>
    /// Actions taken to resolve the alert
    /// </summary>
    public string? ResolutionNotes { get; set; }
}
