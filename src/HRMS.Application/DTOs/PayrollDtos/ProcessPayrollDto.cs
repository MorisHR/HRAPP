namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// DTO for processing a payroll cycle
/// </summary>
public class ProcessPayrollDto
{
    /// <summary>
    /// List of employee IDs to include in payroll processing
    /// If null or empty, all active employees will be processed
    /// </summary>
    public List<Guid>? EmployeeIds { get; set; }

    /// <summary>
    /// Optional notes for this processing
    /// </summary>
    public string? Notes { get; set; }
}
