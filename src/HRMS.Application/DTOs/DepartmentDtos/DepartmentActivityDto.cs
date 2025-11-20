namespace HRMS.Application.DTOs.DepartmentDtos;

/// <summary>
/// DTO for department activity/audit log entries
/// </summary>
public class DepartmentActivityDto
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public string ActivityType { get; set; } = string.Empty; // Created, Updated, Activated, Deactivated, Merged, etc.
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string PerformedByName { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
