using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.TimesheetDtos;

public class TimesheetDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeCode { get; set; }

    public PeriodType PeriodType { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal TotalRegularHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal TotalHolidayHours { get; set; }
    public decimal TotalSickLeaveHours { get; set; }
    public decimal TotalAnnualLeaveHours { get; set; }
    public decimal TotalAbsentHours { get; set; }
    public decimal TotalPayableHours { get; set; }

    public TimesheetStatus Status { get; set; }
    public bool IsLocked { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public string? SubmittedByName { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }

    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<TimesheetEntryDto> Entries { get; set; } = new();
    public List<TimesheetCommentDto> Comments { get; set; } = new();
}
