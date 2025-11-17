using System;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 50: API endpoint performance metrics
/// Tracks response times, throughput, and error rates per endpoint
/// SLA TARGETS: P95 <200ms, P99 <500ms, Error Rate <0.1%
/// </summary>
public class ApiPerformanceDto
{
    /// <summary>
    /// API endpoint path (e.g., "/api/employees")
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE, PATCH)
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// Tenant subdomain (for multi-tenant analysis)
    /// NULL for non-tenant endpoints (SuperAdmin, auth, etc.)
    /// </summary>
    public string? TenantSubdomain { get; set; }

    /// <summary>
    /// Total number of requests in the measurement period
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Number of successful requests (2xx status codes)
    /// </summary>
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// Number of failed requests (4xx + 5xx status codes)
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// Error rate percentage (failed / total * 100)
    /// </summary>
    public decimal ErrorRate { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public decimal AvgResponseTimeMs { get; set; }

    /// <summary>
    /// Median (P50) response time in milliseconds
    /// </summary>
    public decimal P50ResponseTimeMs { get; set; }

    /// <summary>
    /// P95 response time in milliseconds (SLA target: <200ms)
    /// </summary>
    public decimal P95ResponseTimeMs { get; set; }

    /// <summary>
    /// P99 response time in milliseconds (SLA target: <500ms)
    /// </summary>
    public decimal P99ResponseTimeMs { get; set; }

    /// <summary>
    /// Minimum response time observed
    /// </summary>
    public decimal MinResponseTimeMs { get; set; }

    /// <summary>
    /// Maximum response time observed
    /// </summary>
    public decimal MaxResponseTimeMs { get; set; }

    /// <summary>
    /// Requests per second (throughput)
    /// </summary>
    public decimal RequestsPerSecond { get; set; }

    /// <summary>
    /// Average request payload size in bytes
    /// </summary>
    public long? AvgRequestSizeBytes { get; set; }

    /// <summary>
    /// Average response payload size in bytes
    /// </summary>
    public long? AvgResponseSizeBytes { get; set; }

    /// <summary>
    /// Performance status: Excellent, Good, Warning, Critical
    /// Based on P95 response time vs SLA
    /// </summary>
    public string PerformanceStatus { get; set; } = "Unknown";

    /// <summary>
    /// Start of measurement period
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End of measurement period
    /// </summary>
    public DateTime PeriodEnd { get; set; }
}
