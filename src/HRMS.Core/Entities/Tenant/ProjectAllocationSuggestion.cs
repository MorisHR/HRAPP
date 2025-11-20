using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// AI-generated project allocation suggestions
/// Created daily from work patterns, calendar, Git, Jira, etc.
/// Employee can accept/reject/modify suggestions
/// </summary>
public class ProjectAllocationSuggestion : BaseEntity
{
    /// <summary>
    /// Employee ID
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Date for this suggestion
    /// </summary>
    public DateTime SuggestionDate { get; set; }

    /// <summary>
    /// Project ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Suggested hours
    /// </summary>
    public decimal SuggestedHours { get; set; }

    /// <summary>
    /// Confidence score (0-100)
    /// </summary>
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// How was this suggestion generated?
    /// "WorkPattern", "Calendar", "Git", "Jira", "Manual", "Hybrid"
    /// </summary>
    public string SuggestionSource { get; set; } = "WorkPattern";

    /// <summary>
    /// Detailed reason for suggestion (for transparency)
    /// E.g., "You worked on this project every Monday for the last 4 weeks"
    /// </summary>
    public string? SuggestionReason { get; set; }

    /// <summary>
    /// Supporting evidence (JSON)
    /// E.g., {"calendar_events": 2, "git_commits": 5, "jira_tasks": 3}
    /// </summary>
    public string? Evidence { get; set; }

    /// <summary>
    /// Status: "Pending", "Accepted", "Rejected", "Modified", "Expired"
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// When was this suggestion acted upon?
    /// </summary>
    public DateTime? ActionedAt { get; set; }

    /// <summary>
    /// If accepted, link to the created allocation
    /// </summary>
    public Guid? TimesheetProjectAllocationId { get; set; }

    /// <summary>
    /// If modified, what were the final hours?
    /// </summary>
    public decimal? FinalHours { get; set; }

    /// <summary>
    /// If rejected, why?
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Employee feedback (for ML improvement)
    /// </summary>
    public string? EmployeeFeedback { get; set; }

    /// <summary>
    /// Expiry date (suggestions expire after 7 days)
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Tenant isolation
    /// </summary>
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual Project? Project { get; set; }
    public virtual TimesheetProjectAllocation? TimesheetProjectAllocation { get; set; }

    /// <summary>
    /// Accept this suggestion
    /// </summary>
    public Guid AcceptSuggestion()
    {
        if (Status != "Pending")
            throw new InvalidOperationException("Can only accept pending suggestions");

        Status = "Accepted";
        ActionedAt = DateTime.UtcNow;
        FinalHours = SuggestedHours;

        return Guid.NewGuid(); // Will be replaced with actual allocation ID
    }

    /// <summary>
    /// Reject this suggestion
    /// </summary>
    public void RejectSuggestion(string reason)
    {
        if (Status != "Pending")
            throw new InvalidOperationException("Can only reject pending suggestions");

        Status = "Rejected";
        ActionedAt = DateTime.UtcNow;
        RejectionReason = reason;
    }

    /// <summary>
    /// Modify and accept suggestion
    /// </summary>
    public void ModifySuggestion(decimal newHours)
    {
        if (Status != "Pending")
            throw new InvalidOperationException("Can only modify pending suggestions");

        Status = "Modified";
        ActionedAt = DateTime.UtcNow;
        FinalHours = newHours;
    }

    /// <summary>
    /// Check if suggestion is expired
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiryDate || Status == "Expired";
    }
}
