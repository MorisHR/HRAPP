using HRMS.Core.Entities.Tenant;

namespace HRMS.Application.Interfaces;

/// <summary>
/// ML/Rules-based engine for intelligent project allocation suggestions
/// The "AI brain" that predicts which projects an employee worked on
/// </summary>
public interface IProjectAllocationEngine
{
    /// <summary>
    /// Generate project allocation suggestions for an employee on a specific date
    /// Based on work patterns, calendar, Git, Jira, etc.
    /// </summary>
    Task<List<AllocationSuggestion>> GenerateSuggestionsAsync(
        Guid employeeId,
        DateTime date,
        decimal totalHoursAvailable,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Learn from accepted/rejected suggestions to improve future predictions
    /// This is the ML feedback loop
    /// </summary>
    Task LearnFromFeedbackAsync(
        Guid suggestionId,
        bool wasAccepted,
        decimal? finalHours,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update work patterns based on confirmed timesheet allocations
    /// Called after timesheet approval
    /// </summary>
    Task UpdateWorkPatternsAsync(
        List<TimesheetProjectAllocation> confirmedAllocations,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get confidence score for a specific employee-project-date combination
    /// </summary>
    Task<int> GetConfidenceScoreAsync(
        Guid employeeId,
        Guid projectId,
        DateTime date,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect if work pattern has changed (e.g., employee moved to different project)
    /// </summary>
    Task<bool> HasPatternChangedAsync(
        Guid employeeId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A single allocation suggestion from the engine
/// </summary>
public class AllocationSuggestion
{
    public Guid ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal SuggestedHours { get; set; }
    public int ConfidenceScore { get; set; }
    public string Source { get; set; } = string.Empty; // "WorkPattern", "Calendar", "Git", "Jira", "Hybrid"
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Evidence { get; set; } = new();
}
