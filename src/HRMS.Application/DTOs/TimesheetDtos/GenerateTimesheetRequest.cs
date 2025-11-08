using HRMS.Core.Enums;

namespace HRMS.Application.DTOs.TimesheetDtos;

public class GenerateTimesheetRequest
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PeriodType PeriodType { get; set; }
    public List<Guid>? EmployeeIds { get; set; } // Null = all employees
}
