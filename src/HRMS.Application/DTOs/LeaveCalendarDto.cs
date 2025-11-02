using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class LeaveCalendarDto
{
    public Guid LeaveApplicationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public LeaveStatus Status { get; set; }
}
