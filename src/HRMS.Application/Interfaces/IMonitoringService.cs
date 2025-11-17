using HRMS.Application.DTOs.Monitoring;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRMS.Application.Interfaces;

/// <summary>
/// FORTUNE 50: Production monitoring service for SuperAdmin platform oversight
/// Provides real-time observability, performance tracking, and security monitoring
///
/// ARCHITECTURE PRINCIPLES:
/// - Read-only monitoring (zero impact on application performance)
/// - Multi-tenant aware (per-tenant and platform-wide metrics)
/// - Uses separate monitoring schema (monitoring.performance_metrics, etc.)
/// - All operations are async for optimal performance
/// - Caching layer for frequently accessed metrics (5-minute TTL)
///
/// COMPLIANCE: ISO 27001, SOC 2, NIST 800-53 (monitoring and alerting requirements)
/// SLA TARGETS: P95 <200ms, P99 <500ms, Cache Hit Rate >95%, Error Rate <0.1%
/// </summary>
public interface IMonitoringService
{
    // ============================================
    // DASHBOARD OVERVIEW METRICS
    // ============================================

    /// <summary>
    /// Get high-level dashboard metrics for SuperAdmin overview
    /// Aggregates data from all monitoring layers (infrastructure, API, security, tenants)
    /// CACHING: 5-minute TTL to reduce database load
    /// PERFORMANCE: Single query execution using materialized view
    /// </summary>
    /// <returns>Dashboard metrics snapshot</returns>
    Task<DashboardMetricsDto> GetDashboardMetricsAsync();

    /// <summary>
    /// Force refresh of dashboard metrics (bypasses cache)
    /// Use sparingly - triggers immediate database query
    /// </summary>
    /// <returns>Fresh dashboard metrics snapshot</returns>
    Task<DashboardMetricsDto> RefreshDashboardMetricsAsync();

    // ============================================
    // INFRASTRUCTURE HEALTH MONITORING
    // ============================================

    /// <summary>
    /// Get infrastructure layer health metrics
    /// Monitors database performance, connection pooling, cache hit rates, disk I/O
    /// CRITICAL METRICS: Cache hit rate (>95%), connection pool utilization (<80%)
    /// </summary>
    /// <returns>Infrastructure health snapshot</returns>
    Task<InfrastructureHealthDto> GetInfrastructureHealthAsync();

    /// <summary>
    /// Get list of slow queries requiring optimization
    /// Identifies queries exceeding performance thresholds (>200ms P95)
    /// Includes execution plans and optimization suggestions
    /// </summary>
    /// <param name="minExecutionTimeMs">Minimum average execution time to include (default: 200ms)</param>
    /// <param name="limit">Maximum number of slow queries to return (default: 20)</param>
    /// <returns>List of slow queries ordered by total execution time (impact)</returns>
    Task<List<SlowQueryDto>> GetSlowQueriesAsync(decimal minExecutionTimeMs = 200, int limit = 20);

    /// <summary>
    /// Get slow queries for a specific tenant
    /// Useful for identifying tenant-specific performance issues
    /// </summary>
    /// <param name="tenantSubdomain">Tenant subdomain</param>
    /// <param name="minExecutionTimeMs">Minimum average execution time to include (default: 200ms)</param>
    /// <param name="limit">Maximum number of slow queries to return (default: 20)</param>
    /// <returns>List of slow queries for this tenant</returns>
    Task<List<SlowQueryDto>> GetSlowQueriesByTenantAsync(
        string tenantSubdomain,
        decimal minExecutionTimeMs = 200,
        int limit = 20);

    // ============================================
    // API PERFORMANCE MONITORING
    // ============================================

    /// <summary>
    /// Get API endpoint performance metrics
    /// Tracks response times (P50, P95, P99), error rates, throughput
    /// SLA COMPLIANCE: P95 <200ms, P99 <500ms, Error Rate <0.1%
    /// </summary>
    /// <param name="periodStart">Start of measurement period (default: 1 hour ago)</param>
    /// <param name="periodEnd">End of measurement period (default: now)</param>
    /// <param name="tenantSubdomain">Filter by tenant (optional, null for all tenants)</param>
    /// <param name="limit">Maximum number of endpoints to return (default: 50)</param>
    /// <returns>List of API performance metrics ordered by request volume</returns>
    Task<List<ApiPerformanceDto>> GetApiPerformanceAsync(
        DateTime? periodStart = null,
        DateTime? periodEnd = null,
        string? tenantSubdomain = null,
        int limit = 50);

    /// <summary>
    /// Get performance metrics for a specific API endpoint
    /// Useful for drilling down into endpoint-specific issues
    /// </summary>
    /// <param name="endpoint">API endpoint path (e.g., "/api/employees")</param>
    /// <param name="httpMethod">HTTP method (GET, POST, PUT, DELETE, PATCH)</param>
    /// <param name="periodStart">Start of measurement period (default: 1 hour ago)</param>
    /// <param name="periodEnd">End of measurement period (default: now)</param>
    /// <returns>API performance metrics for this endpoint</returns>
    Task<ApiPerformanceDto?> GetEndpointPerformanceAsync(
        string endpoint,
        string httpMethod,
        DateTime? periodStart = null,
        DateTime? periodEnd = null);

    /// <summary>
    /// Get endpoints exceeding SLA targets
    /// Identifies endpoints requiring immediate attention
    /// </summary>
    /// <param name="periodStart">Start of measurement period (default: 1 hour ago)</param>
    /// <param name="periodEnd">End of measurement period (default: now)</param>
    /// <returns>List of endpoints with SLA violations</returns>
    Task<List<ApiPerformanceDto>> GetSlaViolationsAsync(
        DateTime? periodStart = null,
        DateTime? periodEnd = null);

    // ============================================
    // MULTI-TENANT ACTIVITY MONITORING
    // ============================================

    /// <summary>
    /// Get activity and resource utilization metrics for all tenants
    /// Provides visibility into tenant usage patterns, growth trends, health scores
    /// BUSINESS VALUE: Identifies high-value tenants and optimization opportunities
    /// </summary>
    /// <param name="periodStart">Start of measurement period (default: 24 hours ago)</param>
    /// <param name="periodEnd">End of measurement period (default: now)</param>
    /// <param name="minActiveUsers">Filter tenants with at least this many active users (optional)</param>
    /// <param name="status">Filter by tenant status: Active, Suspended, Trial, Churned (optional)</param>
    /// <param name="sortBy">Sort field: ActiveUsers, RequestVolume, ErrorRate, StorageUsage (default: ActiveUsers)</param>
    /// <param name="limit">Maximum number of tenants to return (default: 100)</param>
    /// <returns>List of tenant activity metrics</returns>
    Task<List<TenantActivityDto>> GetTenantActivityAsync(
        DateTime? periodStart = null,
        DateTime? periodEnd = null,
        int? minActiveUsers = null,
        string? status = null,
        string sortBy = "ActiveUsers",
        int limit = 100);

    /// <summary>
    /// Get activity metrics for a specific tenant
    /// Detailed view of single tenant performance and resource usage
    /// </summary>
    /// <param name="tenantSubdomain">Tenant subdomain</param>
    /// <param name="periodStart">Start of measurement period (default: 24 hours ago)</param>
    /// <param name="periodEnd">End of measurement period (default: now)</param>
    /// <returns>Tenant activity metrics or null if tenant not found</returns>
    Task<TenantActivityDto?> GetTenantActivityBySubdomainAsync(
        string tenantSubdomain,
        DateTime? periodStart = null,
        DateTime? periodEnd = null);

    /// <summary>
    /// Get tenants with health score below threshold
    /// Identifies at-risk tenants requiring attention (low activity, high errors)
    /// HEALTH SCORE: 0-100 based on activity, performance, error rate
    /// </summary>
    /// <param name="maxHealthScore">Maximum health score to include (default: 50)</param>
    /// <param name="limit">Maximum number of tenants to return (default: 20)</param>
    /// <returns>List of at-risk tenants</returns>
    Task<List<TenantActivityDto>> GetAtRiskTenantsAsync(
        int maxHealthScore = 50,
        int limit = 20);

    // ============================================
    // SECURITY EVENT MONITORING
    // ============================================

    /// <summary>
    /// Get security events and threat detection metrics
    /// Monitors authentication failures, IDOR attempts, rate limit violations
    /// COMPLIANCE: SOC 2, ISO 27001, PCI-DSS security logging requirements
    /// </summary>
    /// <param name="periodStart">Start of measurement period (default: 24 hours ago)</param>
    /// <param name="periodEnd">End of measurement period (default: now)</param>
    /// <param name="eventType">Filter by event type: FailedLogin, IdorAttempt, RateLimitExceeded, etc. (optional)</param>
    /// <param name="severity">Filter by severity: Critical, High, Medium, Low, Info (optional)</param>
    /// <param name="tenantSubdomain">Filter by tenant (optional)</param>
    /// <param name="isBlocked">Filter by blocked status (optional, true = blocked by security controls)</param>
    /// <param name="isReviewed">Filter by review status (optional, false = unreviewed events)</param>
    /// <param name="limit">Maximum number of events to return (default: 100)</param>
    /// <returns>List of security events ordered by severity and timestamp</returns>
    Task<List<SecurityEventDto>> GetSecurityEventsAsync(
        DateTime? periodStart = null,
        DateTime? periodEnd = null,
        string? eventType = null,
        string? severity = null,
        string? tenantSubdomain = null,
        bool? isBlocked = null,
        bool? isReviewed = null,
        int limit = 100);

    /// <summary>
    /// Get critical security events requiring immediate review
    /// Auto-filters for Critical/High severity unreviewed events
    /// </summary>
    /// <param name="periodStart">Start of measurement period (default: 24 hours ago)</param>
    /// <param name="periodEnd">End of measurement period (default: now)</param>
    /// <returns>List of critical security events</returns>
    Task<List<SecurityEventDto>> GetCriticalSecurityEventsAsync(
        DateTime? periodStart = null,
        DateTime? periodEnd = null);

    /// <summary>
    /// Mark security event as reviewed
    /// Records reviewer identity and notes for audit trail
    /// </summary>
    /// <param name="eventId">Security event ID</param>
    /// <param name="reviewedBy">Email of reviewer (SuperAdmin)</param>
    /// <param name="reviewNotes">Review notes and actions taken</param>
    /// <returns>True if event was found and updated</returns>
    Task<bool> MarkSecurityEventReviewedAsync(
        long eventId,
        string reviewedBy,
        string? reviewNotes = null);

    // ============================================
    // ALERT MANAGEMENT
    // ============================================

    /// <summary>
    /// Get active and historical alerts
    /// Monitors SLA violations, security incidents, capacity issues
    /// ALERT SEVERITY: Critical (immediate action), High (1h), Medium (4h), Low (24h)
    /// </summary>
    /// <param name="status">Filter by status: Active, Acknowledged, Resolved, Suppressed (optional, null for all)</param>
    /// <param name="severity">Filter by severity: Critical, High, Medium, Low, Info (optional)</param>
    /// <param name="alertType">Filter by type: Performance, Security, Availability, Capacity, Compliance (optional)</param>
    /// <param name="tenantSubdomain">Filter by tenant (optional, null for platform-wide alerts)</param>
    /// <param name="periodStart">Start of period (default: 7 days ago)</param>
    /// <param name="periodEnd">End of period (default: now)</param>
    /// <param name="limit">Maximum number of alerts to return (default: 50)</param>
    /// <returns>List of alerts ordered by severity and triggered timestamp</returns>
    Task<List<AlertDto>> GetAlertsAsync(
        string? status = null,
        string? severity = null,
        string? alertType = null,
        string? tenantSubdomain = null,
        DateTime? periodStart = null,
        DateTime? periodEnd = null,
        int limit = 50);

    /// <summary>
    /// Get active critical and high severity alerts
    /// Quick view of alerts requiring immediate attention
    /// </summary>
    /// <returns>List of active critical/high alerts</returns>
    Task<List<AlertDto>> GetActiveAlertsAsync();

    /// <summary>
    /// Acknowledge an alert (mark as being worked on)
    /// Records who acknowledged and when for accountability
    /// </summary>
    /// <param name="alertId">Alert ID</param>
    /// <param name="acknowledgedBy">Email of person acknowledging (SuperAdmin)</param>
    /// <returns>True if alert was found and acknowledged</returns>
    Task<bool> AcknowledgeAlertAsync(long alertId, string acknowledgedBy);

    /// <summary>
    /// Resolve an alert (mark as fixed)
    /// Records resolution details for post-mortem analysis
    /// </summary>
    /// <param name="alertId">Alert ID</param>
    /// <param name="resolvedBy">Email of person resolving (SuperAdmin)</param>
    /// <param name="resolutionNotes">Actions taken to resolve the alert</param>
    /// <returns>True if alert was found and resolved</returns>
    Task<bool> ResolveAlertAsync(long alertId, string resolvedBy, string? resolutionNotes = null);

    // ============================================
    // METRICS COLLECTION (Background Jobs)
    // ============================================

    /// <summary>
    /// Capture performance snapshot for time-series analysis
    /// Called by background job every 5 minutes
    /// Executes monitoring.capture_performance_snapshot() database function
    /// </summary>
    /// <returns>Number of metrics captured</returns>
    Task<int> CapturePerformanceSnapshotAsync();

    /// <summary>
    /// Log API performance metrics from middleware
    /// Called by AuditLoggingMiddleware for each API request
    /// Executes monitoring.log_api_performance() database function
    /// </summary>
    /// <param name="endpoint">API endpoint path</param>
    /// <param name="httpMethod">HTTP method</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="responseTimeMs">Response time in milliseconds</param>
    /// <param name="tenantSubdomain">Tenant subdomain (null for non-tenant requests)</param>
    /// <param name="requestSizeBytes">Request payload size in bytes (optional)</param>
    /// <param name="responseSizeBytes">Response payload size in bytes (optional)</param>
    /// <returns>Task completion</returns>
    Task LogApiPerformanceAsync(
        string endpoint,
        string httpMethod,
        int statusCode,
        decimal responseTimeMs,
        string? tenantSubdomain = null,
        long? requestSizeBytes = null,
        long? responseSizeBytes = null);

    /// <summary>
    /// Log security event for threat detection
    /// Called by security middleware when suspicious activity is detected
    /// Executes monitoring.log_security_event() database function
    /// </summary>
    /// <param name="eventType">Event type: FailedLogin, IdorAttempt, RateLimitExceeded, etc.</param>
    /// <param name="severity">Severity: Critical, High, Medium, Low, Info</param>
    /// <param name="description">Human-readable event description</param>
    /// <param name="userId">User ID involved (optional)</param>
    /// <param name="userEmail">User email (optional)</param>
    /// <param name="ipAddress">Source IP address (optional)</param>
    /// <param name="tenantSubdomain">Tenant subdomain (optional)</param>
    /// <param name="resourceId">Resource ID involved (optional)</param>
    /// <param name="endpoint">API endpoint involved (optional)</param>
    /// <param name="isBlocked">Whether event was blocked by security controls</param>
    /// <param name="details">Additional event details as JSON (optional)</param>
    /// <returns>Task completion</returns>
    Task LogSecurityEventAsync(
        string eventType,
        string severity,
        string description,
        string? userId = null,
        string? userEmail = null,
        string? ipAddress = null,
        string? tenantSubdomain = null,
        string? resourceId = null,
        string? endpoint = null,
        bool isBlocked = false,
        string? details = null);
}
