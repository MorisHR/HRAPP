using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.TimesheetDtos;

public class TimesheetEntryDto
{
    public Guid Id { get; set; }
    public Guid TimesheetId { get; set; }
    public DateTime Date { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;

    public Guid? AttendanceId { get; set; }

    public DateTime? ClockInTime { get; set; }
    public DateTime? ClockOutTime { get; set; }
    public int BreakDuration { get; set; }

    public decimal ActualHours { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal HolidayHours { get; set; }
    public decimal SickLeaveHours { get; set; }
    public decimal AnnualLeaveHours { get; set; }

    public bool IsAbsent { get; set; }
    public bool IsHoliday { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsOnLeave { get; set; }

    public DayType DayType { get; set; }
    public string? Notes { get; set; }

    public List<TimesheetAdjustmentDto> Adjustments { get; set; } = new();
}
