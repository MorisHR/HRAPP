using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Synced issue assignment from Jira
/// Tracks which Jira issues are assigned to which employees
/// Used for suggesting project allocations when employee has active issues
/// </summary>
public class JiraIssueAssignment : BaseEntity
{
    public required Guid TenantId { get; set; }

    /// <summary>
    /// Jira issue key (e.g., "PROJ-123")
    /// </summary>
    public required string JiraIssueKey { get; set; }

    /// <summary>
    /// Issue summary/title
    /// </summary>
    public string? JiraIssueSummary { get; set; }

    /// <summary>
    /// Assigned employee
    /// </summary>
    public required Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>
    /// HRMS project (mapped from Jira project)
    /// </summary>
    public required Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    /// <summary>
    /// Jira project key
    /// </summary>
    public string? JiraProjectKey { get; set; }

    /// <summary>
    /// Issue type (Story, Bug, Task, Epic, etc.)
    /// </summary>
    public string? IssueType { get; set; }

    /// <summary>
    /// Issue status (To Do, In Progress, Done, etc.)
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Priority (Highest, High, Medium, Low, Lowest)
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Original estimate in hours
    /// </summary>
    public decimal? EstimateHours { get; set; }

    /// <summary>
    /// Remaining estimate in hours
    /// </summary>
    public decimal? RemainingHours { get; set; }

    /// <summary>
    /// Time spent so far (from work logs)
    /// </summary>
    public decimal? TimeSpentHours { get; set; }

    /// <summary>
    /// Issue due date
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Sprint name (if in sprint)
    /// </summary>
    public string? SprintName { get; set; }

    /// <summary>
    /// Whether issue is currently active (not Done/Closed)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this assignment was created in Jira
    /// </summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// Last time issue was updated in Jira
    /// </summary>
    public DateTime LastUpdatedInJira { get; set; }

    /// <summary>
    /// When this was synced from Jira
    /// </summary>
    public required DateTime SyncedAt { get; set; }

    /// <summary>
    /// Jira issue URL for reference
    /// </summary>
    public string? JiraIssueUrl { get; set; }
}
