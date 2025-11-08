namespace HRMS.Application.DTOs.PayrollDtos;

/// <summary>
/// Request DTO for calculating payroll from approved timesheets
/// </summary>
public class CalculateFromTimesheetsRequest
{
    /// <summary>
    /// Employee ID to calculate payroll for
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Start date of the payroll period
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End date of the payroll period
    /// </summary>
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// Request DTO for batch calculating payroll from timesheets for multiple employees
/// </summary>
public class BatchCalculateFromTimesheetsRequest
{
    /// <summary>
    /// List of employee IDs to process
    /// </summary>
    public List<Guid> EmployeeIds { get; set; } = new();

    /// <summary>
    /// Start date of the payroll period
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End date of the payroll period
    /// </summary>
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// Result DTO for batch payroll processing
/// </summary>
public class BatchPayrollResult
{
    /// <summary>
    /// Successfully calculated payroll results
    /// </summary>
    public List<PayrollResult> Results { get; set; } = new();

    /// <summary>
    /// Total number of employees successfully processed
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Total number of employees that failed processing
    /// </summary>
    public int TotalFailed { get; set; }

    /// <summary>
    /// List of error messages for failed employees
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Timestamp when batch processing completed
    /// </summary>
    public DateTime ProcessedAt { get; set; }
}
