namespace HRMS.Application.DTOs.ProjectDtos;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ClientName { get; set; }
    public string ProjectType { get; set; } = "Client";
    public bool IsBillable { get; set; }
    public decimal? BillingRate { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public decimal? BudgetHours { get; set; }
    public decimal? BudgetAmount { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? ProjectManagerId { get; set; }
    public string? ProjectManagerName { get; set; }
    public bool AllowTimeEntry { get; set; }
    public bool RequireApproval { get; set; }

    // Calculated fields
    public decimal TotalHoursLogged { get; set; }
    public decimal? RemainingBudgetHours { get; set; }
    public decimal? BudgetUtilizationPercent { get; set; }
    public bool IsOverBudget { get; set; }

    public DateTime CreatedAt { get; set; }
}
