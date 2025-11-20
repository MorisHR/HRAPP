using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// How an employee's daily hours are allocated across projects
/// This is the KEY entity for intelligent timesheet
/// Links: TimesheetEntry (1 day) → Multiple Projects
/// </summary>
public class TimesheetProjectAllocation : BaseEntity
{
    /// <summary>
    /// Link to the timesheet entry (one day)
    /// </summary>
    public Guid TimesheetEntryId { get; set; }

    /// <summary>
    /// Link to the project
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Employee ID (denormalized for faster querying)
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Date of work (denormalized for faster querying)
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Hours allocated to this project on this day
    /// </summary>
    public decimal Hours { get; set; }

    /// <summary>
    /// Task/activity description (optional)
    /// E.g., "Bug fixing", "Code review", "Client meeting"
    /// </summary>
    public string? TaskDescription { get; set; }

    /// <summary>
    /// Is this allocation billable?
    /// (Derived from project, but can be overridden)
    /// </summary>
    public bool IsBillable { get; set; } = true;

    /// <summary>
    /// Billing rate applied (snapshot at time of entry)
    /// </summary>
    public decimal? BillingRate { get; set; }

    /// <summary>
    /// Total billing amount (Hours × BillingRate)
    /// </summary>
    public decimal BillingAmount => Hours * (BillingRate ?? 0);

    /// <summary>
    /// How was this allocation created?
    /// "Manual", "AutoSuggested", "ImportedFromCalendar", "ImportedFromGit", "ImportedFromJira"
    /// </summary>
    public string AllocationSource { get; set; } = "Manual";

    /// <summary>
    /// Confidence score if auto-suggested (0-100)
    /// </summary>
    public int? ConfidenceScore { get; set; }

    /// <summary>
    /// Was this suggestion accepted by the employee?
    /// null = not suggested, true = accepted, false = rejected
    /// </summary>
    public bool? SuggestionAccepted { get; set; }

    /// <summary>
    /// Notes/comments for this allocation
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Tenant isolation
    /// </summary>
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual TimesheetEntry? TimesheetEntry { get; set; }
    public virtual Project? Project { get; set; }
    public virtual Employee? Employee { get; set; }
}
