using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.AuditLog;

/// <summary>
/// Filter criteria for audit log queries
/// Supports comprehensive filtering for compliance and security investigations
/// </summary>
public class AuditLogFilterDto
{
    /// <summary>Tenant ID filter (enforced by backend for tenant admins)</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Start date for time range filter</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>End date for time range filter</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Filter by user email (partial match)</summary>
    public string? UserEmail { get; set; }

    /// <summary>Filter by specific action types</summary>
    public List<AuditActionType>? ActionTypes { get; set; }

    /// <summary>Filter by audit categories</summary>
    public List<AuditCategory>? Categories { get; set; }

    /// <summary>Filter by severity levels</summary>
    public List<AuditSeverity>? Severities { get; set; }

    /// <summary>Filter by entity type (e.g., "Employee", "Tenant")</summary>
    public string? EntityType { get; set; }

    /// <summary>Filter by specific entity ID</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Filter by success/failure</summary>
    public bool? Success { get; set; }

    /// <summary>Search in changed fields</summary>
    public string? ChangedFieldsSearch { get; set; }

    /// <summary>IP address filter</summary>
    public string? IpAddress { get; set; }

    /// <summary>Correlation ID for related actions</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Page number for pagination</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Page size for pagination</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Sort field (default: PerformedAt)</summary>
    public string SortBy { get; set; } = "PerformedAt";

    /// <summary>Sort direction (desc = newest first)</summary>
    public bool SortDescending { get; set; } = true;
}
