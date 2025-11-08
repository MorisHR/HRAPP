using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.TimesheetDtos;

/// <summary>
/// Lightweight DTO for listing timesheets
/// </summary>
public class TimesheetListDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeCode { get; set; }

    public PeriodType PeriodType { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal TotalPayableHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }

    public TimesheetStatus Status { get; set; }
    public bool IsLocked { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByName { get; set; }

    public DateTime CreatedAt { get; set; }
}
