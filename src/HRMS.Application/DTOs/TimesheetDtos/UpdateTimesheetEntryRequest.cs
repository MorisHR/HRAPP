namespace HRMS.Application.DTOs.TimesheetDtos;

public class UpdateTimesheetEntryRequest
{
    public DateTime? ClockInTime { get; set; }
    public DateTime? ClockOutTime { get; set; }
    public int? BreakDuration { get; set; }
    public string? Notes { get; set; }
}
