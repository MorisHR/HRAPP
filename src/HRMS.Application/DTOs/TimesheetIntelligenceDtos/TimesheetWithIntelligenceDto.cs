namespace HRMS.Application.DTOs.TimesheetIntelligenceDtos;

/// <summary>
/// Timesheet converted from attendance data with intelligent project allocation
/// </summary>
public class TimesheetWithIntelligenceDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    // Clock times from attendance
    public DateTime? ClockInTime { get; set; }
    public DateTime? ClockOutTime { get; set; }
    public decimal TotalHours { get; set; }

    // Intelligent project allocation suggestions
    public List<ProjectAllocationDto> SuggestedAllocations { get; set; } = new();
    public List<ProjectAllocationDto> ConfirmedAllocations { get; set; } = new();

    // Anomalies detected
    public List<AttendanceAnomalyDto> Anomalies { get; set; } = new();

    // Status
    public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved
    public bool NeedsReview { get; set; }
    public string? ReviewReason { get; set; }
}

public class ProjectAllocationDto
{
    public Guid? Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public string? TaskDescription { get; set; }
    public bool IsBillable { get; set; }
    public decimal? BillingRate { get; set; }
    public decimal BillingAmount { get; set; }

    // Intelligence metadata
    public string AllocationSource { get; set; } = "Manual"; // Manual, AutoSuggested, ImportedFromCalendar, etc.
    public int? ConfidenceScore { get; set; }
    public string? SuggestionReason { get; set; }
    public bool? SuggestionAccepted { get; set; }
}

public class AttendanceAnomalyDto
{
    public string AnomalyType { get; set; } = string.Empty;
    public string AnomalySeverity { get; set; } = "Warning";
    public string Description { get; set; } = string.Empty;
    public DateTime AnomalyTime { get; set; }
    public string ResolutionStatus { get; set; } = "Pending";
}
