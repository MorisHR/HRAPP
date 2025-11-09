using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.AuditLog;

/// <summary>
/// Audit log list item DTO for table display
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFullName { get; set; }
    public string? UserRole { get; set; }
    public AuditActionType ActionType { get; set; }
    public string ActionTypeName { get; set; } = string.Empty;
    public AuditCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public AuditSeverity Severity { get; set; }
    public string SeverityName { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public bool Success { get; set; }
    public string? ChangedFields { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Detailed audit log DTO with full information
/// </summary>
public class AuditLogDetailDto : AuditLogDto
{
    public Dictionary<string, object?>? OldValues { get; set; }
    public Dictionary<string, object?>? NewValues { get; set; }
    public Dictionary<string, object?>? AdditionalMetadata { get; set; }
    public string? Reason { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public int? ResponseCode { get; set; }
    public int? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SessionId { get; set; }
    public string? DeviceInfo { get; set; }
    public string? Geolocation { get; set; }
    public Guid? ParentActionId { get; set; }
}

/// <summary>
/// Paginated result wrapper
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Audit log statistics DTO
/// </summary>
public class AuditLogStatisticsDto
{
    public int TotalActions { get; set; }
    public int ActionsToday { get; set; }
    public int ActionsThisWeek { get; set; }
    public int ActionsThisMonth { get; set; }
    public int FailedLogins { get; set; }
    public int CriticalEvents { get; set; }
    public int WarningEvents { get; set; }
    public Dictionary<string, int> ActionsByCategory { get; set; } = new();
    public Dictionary<string, int> ActionsBySeverity { get; set; } = new();
    public List<TopUserActivityDto> MostActiveUsers { get; set; } = new();
    public List<TopEntityActivityDto> MostModifiedEntities { get; set; } = new();
}

/// <summary>
/// Top user activity summary
/// </summary>
public class TopUserActivityDto
{
    public string UserEmail { get; set; } = string.Empty;
    public string? UserFullName { get; set; }
    public int ActionCount { get; set; }
    public DateTime LastActivity { get; set; }
}

/// <summary>
/// Top entity activity summary
/// </summary>
public class TopEntityActivityDto
{
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public int ChangeCount { get; set; }
    public DateTime LastModified { get; set; }
}

/// <summary>
/// User activity summary DTO
/// </summary>
public class UserActivityDto
{
    public string UserEmail { get; set; } = string.Empty;
    public string? UserFullName { get; set; }
    public int TotalActions { get; set; }
    public int UniqueActionTypes { get; set; }
    public DateTime FirstActivity { get; set; }
    public DateTime LastActivity { get; set; }
    public Dictionary<string, int> ActionBreakdown { get; set; } = new();
}
