using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Synced work log from Jira
/// Represents time logged by employee in Jira against an issue
/// </summary>
public class JiraWorkLog : BaseEntity
{
    public required Guid TenantId { get; set; }

    /// <summary>
    /// Jira's work log ID (for deduplication)
    /// </summary>
    public required string JiraWorkLogId { get; set; }

    /// <summary>
    /// Employee who logged the work
    /// </summary>
    public required Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>
    /// HRMS project (mapped from Jira project)
    /// </summary>
    public required Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    /// <summary>
    /// Jira issue key (e.g., "PROJ-123")
    /// </summary>
    public required string JiraIssueKey { get; set; }

    /// <summary>
    /// Issue summary/title
    /// </summary>
    public string? JiraIssueSummary { get; set; }

    /// <summary>
    /// Issue type (Story, Bug, Task, etc.)
    /// </summary>
    public string? JiraIssueType { get; set; }

    /// <summary>
    /// When work was started (from Jira)
    /// </summary>
    public required DateTime StartedAt { get; set; }

    /// <summary>
    /// Time spent in hours
    /// </summary>
    public required decimal TimeSpentHours { get; set; }

    /// <summary>
    /// Work description/comment from Jira
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Jira author username
    /// </summary>
    public string? JiraAuthorUsername { get; set; }

    /// <summary>
    /// When this was synced from Jira
    /// </summary>
    public required DateTime SyncedAt { get; set; }

    /// <summary>
    /// Whether this work log was used to create a timesheet allocation
    /// </summary>
    public bool WasConverted { get; set; }

    /// <summary>
    /// Reference to created allocation (if converted)
    /// </summary>
    public Guid? TimesheetProjectAllocationId { get; set; }
    public TimesheetProjectAllocation? TimesheetProjectAllocation { get; set; }
}
