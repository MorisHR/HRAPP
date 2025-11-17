using System;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 50: Per-tenant activity and resource utilization metrics
/// Multi-tenant isolation monitoring and usage analytics
/// BUSINESS VALUE: Identifies high-value tenants and optimization opportunities
/// </summary>
public class TenantActivityDto
{
    /// <summary>
    /// Tenant unique identifier
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Tenant subdomain (URL identifier)
    /// </summary>
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// Tenant company name
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Tenant subscription tier (Starter, Professional, Enterprise)
    /// </summary>
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    /// Total number of employees in this tenant
    /// </summary>
    public int TotalEmployees { get; set; }

    /// <summary>
    /// Number of active users (logged in last 24h)
    /// </summary>
    public int ActiveUsersLast24h { get; set; }

    /// <summary>
    /// Total API requests from this tenant in the measurement period
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Requests per second from this tenant
    /// </summary>
    public decimal RequestsPerSecond { get; set; }

    /// <summary>
    /// Average response time for this tenant's requests (ms)
    /// </summary>
    public decimal AvgResponseTimeMs { get; set; }

    /// <summary>
    /// Error rate for this tenant's requests (%)
    /// </summary>
    public decimal ErrorRate { get; set; }

    /// <summary>
    /// Database schema size in bytes
    /// </summary>
    public long SchemaSizeBytes { get; set; }

    /// <summary>
    /// Human-readable schema size (e.g., "125 MB")
    /// </summary>
    public string SchemaSizeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Number of database queries executed by this tenant
    /// </summary>
    public long DatabaseQueries { get; set; }

    /// <summary>
    /// Average database query time for this tenant (ms)
    /// </summary>
    public decimal AvgQueryTimeMs { get; set; }

    /// <summary>
    /// Storage utilization percentage (against tier limit)
    /// </summary>
    public decimal StorageUtilization { get; set; }

    /// <summary>
    /// Last login timestamp for any user in this tenant
    /// </summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>
    /// Tenant creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Tenant status: Active, Suspended, Trial, Churned
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Health score (0-100) based on activity and performance
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    /// Measurement period start
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Measurement period end
    /// </summary>
    public DateTime PeriodEnd { get; set; }
}
