using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Comments/discussion thread on timesheets
/// Enables communication between employee and manager
/// </summary>
public class TimesheetComment : BaseEntity
{
    public Guid TimesheetId { get; set; }

    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;

    public DateTime CommentedAt { get; set; }

    // Navigation properties
    public virtual Timesheet? Timesheet { get; set; }
}
