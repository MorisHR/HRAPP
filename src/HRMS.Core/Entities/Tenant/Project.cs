using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Project entity for time tracking and billing
/// Employees allocate their timesheet hours to projects
/// </summary>
public class Project : BaseEntity
{
    /// <summary>
    /// Project code (e.g., "PROJ-2024-001")
    /// </summary>
    public string ProjectCode { get; set; } = string.Empty;

    /// <summary>
    /// Project name
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Project description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Client/Customer name
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Project type: "Internal", "Client", "Training", "Support", "Overhead"
    /// </summary>
    public string ProjectType { get; set; } = "Client";

    /// <summary>
    /// Is this project billable to client?
    /// </summary>
    public bool IsBillable { get; set; } = true;

    /// <summary>
    /// Billing rate per hour (if applicable)
    /// </summary>
    public decimal? BillingRate { get; set; }

    /// <summary>
    /// Project status: "Active", "OnHold", "Completed", "Cancelled"
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Project start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Project end date (deadline)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Actual completion date
    /// </summary>
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// Budget in hours (if applicable)
    /// </summary>
    public decimal? BudgetHours { get; set; }

    /// <summary>
    /// Budget in currency (if applicable)
    /// </summary>
    public decimal? BudgetAmount { get; set; }

    /// <summary>
    /// Department responsible for this project
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Project manager/owner
    /// </summary>
    public Guid? ProjectManagerId { get; set; }

    /// <summary>
    /// Is this project active for time logging?
    /// </summary>
    public bool AllowTimeEntry { get; set; } = true;

    /// <summary>
    /// Require manager approval for time entries?
    /// </summary>
    public bool RequireApproval { get; set; } = false;

    /// <summary>
    /// Tenant isolation
    /// </summary>
    public Guid TenantId { get; set; }

    // Navigation properties
    public virtual Department? Department { get; set; }
    public virtual Employee? ProjectManager { get; set; }
    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
    public virtual ICollection<TimesheetProjectAllocation> TimesheetAllocations { get; set; } = new List<TimesheetProjectAllocation>();

    /// <summary>
    /// Calculate total hours logged to this project
    /// </summary>
    public decimal GetTotalHoursLogged()
    {
        return TimesheetAllocations.Sum(a => a.Hours);
    }

    /// <summary>
    /// Calculate remaining budget hours
    /// </summary>
    public decimal? GetRemainingBudgetHours()
    {
        if (!BudgetHours.HasValue) return null;
        return BudgetHours.Value - GetTotalHoursLogged();
    }

    /// <summary>
    /// Check if project is over budget
    /// </summary>
    public bool IsOverBudget()
    {
        if (!BudgetHours.HasValue) return false;
        return GetTotalHoursLogged() > BudgetHours.Value;
    }

    /// <summary>
    /// Calculate budget utilization percentage
    /// </summary>
    public decimal? GetBudgetUtilizationPercent()
    {
        if (!BudgetHours.HasValue || BudgetHours.Value == 0) return null;
        return (GetTotalHoursLogged() / BudgetHours.Value) * 100;
    }
}
