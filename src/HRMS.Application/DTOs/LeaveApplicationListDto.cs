using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class LeaveApplicationListDto
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public LeaveStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }
}
