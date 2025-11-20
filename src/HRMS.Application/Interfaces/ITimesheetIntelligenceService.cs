using HRMS.Application.DTOs.TimesheetIntelligenceDtos;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Core intelligence service that converts attendance data into smart timesheets
/// This is the "brain" of the intelligent timesheet system
/// </summary>
public interface ITimesheetIntelligenceService
{
    /// <summary>
    /// Generate intelligent timesheets from attendance data
    /// Converts BiometricPunchRecord → Attendance → Timesheet with project allocations
    /// </summary>
    Task<GenerateTimesheetResponseDto> GenerateTimesheetsFromAttendanceAsync(
        GenerateTimesheetFromAttendanceDto request,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get intelligent timesheet suggestions for an employee for a specific date range
    /// </summary>
    Task<List<TimesheetWithIntelligenceDto>> GetIntelligentTimesheetsAsync(
        Guid employeeId,
        DateTime startDate,
        DateTime endDate,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending project allocation suggestions for an employee
    /// </summary>
    Task<List<ProjectAllocationSuggestionDto>> GetPendingSuggestionsAsync(
        Guid employeeId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accept a project allocation suggestion
    /// </summary>
    Task<bool> AcceptSuggestionAsync(
        AcceptSuggestionDto request,
        Guid employeeId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch accept/reject suggestions
    /// </summary>
    Task<BatchActionResultDto> BatchAcceptSuggestionsAsync(
        BatchAcceptSuggestionsDto request,
        Guid employeeId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually allocate hours to a project (no suggestion)
    /// </summary>
    Task<Guid> ManuallyAllocateHoursAsync(
        ManualProjectAllocationDto request,
        Guid employeeId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get timesheet summary for approval
    /// </summary>
    Task<TimesheetApprovalSummaryDto> GetTimesheetForApprovalAsync(
        Guid timesheetId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit timesheet for approval
    /// </summary>
    Task<bool> SubmitTimesheetAsync(
        Guid timesheetId,
        Guid employeeId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

// Supporting DTOs
public class ProjectAllocationSuggestionDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateTime SuggestionDate { get; set; }
    public decimal SuggestedHours { get; set; }
    public int ConfidenceScore { get; set; }
    public string SuggestionSource { get; set; } = string.Empty;
    public string? SuggestionReason { get; set; }
    public string? Evidence { get; set; }
    public DateTime ExpiryDate { get; set; }
}

public class BatchActionResultDto
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ManualProjectAllocationDto
{
    public Guid ProjectId { get; set; }
    public DateTime Date { get; set; }
    public decimal Hours { get; set; }
    public string? TaskDescription { get; set; }
}

public class TimesheetApprovalSummaryDto
{
    public Guid TimesheetId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalHours { get; set; }
    public decimal TotalBillableHours { get; set; }
    public decimal TotalNonBillableHours { get; set; }
    public List<ProjectBreakdown> ProjectBreakdowns { get; set; } = new();
    public List<AttendanceAnomalyDto> Anomalies { get; set; } = new();
    public bool HasAnomalies { get; set; }
    public string RecommendedAction { get; set; } = "Approve"; // Approve, Review, Reject
    public string? RecommendationReason { get; set; }
}

public class ProjectBreakdown
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public bool IsBillable { get; set; }
    public decimal BillingAmount { get; set; }
}
