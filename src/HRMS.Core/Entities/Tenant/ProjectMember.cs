using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Defines which employees can work on which projects
/// Used for access control and intelligent suggestions
/// </summary>
public class ProjectMember : BaseEntity
{
    /// <summary>
    /// Project ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Employee ID
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Role in project: "Developer", "Lead", "Tester", "Analyst", etc.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// When did this employee join the project?
    /// </summary>
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When did/will this employee leave the project?
    /// </summary>
    public DateTime? RemovedDate { get; set; }

    /// <summary>
    /// Is this member active on the project?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Expected hours per week for this employee on this project
    /// Used for capacity planning
    /// </summary>
    public decimal? ExpectedHoursPerWeek { get; set; }

    /// <summary>
    /// Billing rate override for this employee on this project
    /// If null, use employee's default rate
    /// </summary>
    public decimal? BillingRateOverride { get; set; }

    // Navigation properties
    public virtual Project? Project { get; set; }
    public virtual Employee? Employee { get; set; }
}
