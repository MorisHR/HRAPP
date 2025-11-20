namespace HRMS.Application.DTOs.TimesheetIntelligenceDtos;

/// <summary>
/// Request to generate intelligent timesheet from attendance data
/// </summary>
public class GenerateTimesheetFromAttendanceDto
{
    public Guid? EmployeeId { get; set; } // null = all employees
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Should we auto-generate project allocation suggestions?
    /// </summary>
    public bool GenerateSuggestions { get; set; } = true;

    /// <summary>
    /// Should we auto-accept high-confidence suggestions?
    /// </summary>
    public bool AutoAcceptHighConfidence { get; set; } = false;

    /// <summary>
    /// Minimum confidence score to auto-accept (0-100)
    /// </summary>
    public int MinConfidenceForAutoAccept { get; set; } = 90;
}

/// <summary>
/// Response from timesheet generation
/// </summary>
public class GenerateTimesheetResponseDto
{
    public int TotalDaysProcessed { get; set; }
    public int EmployeesProcessed { get; set; }
    public int SuggestionsGenerated { get; set; }
    public int SuggestionsAutoAccepted { get; set; }
    public int AnomaliesDetected { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<TimesheetWithIntelligenceDto> GeneratedTimesheets { get; set; } = new();
}
