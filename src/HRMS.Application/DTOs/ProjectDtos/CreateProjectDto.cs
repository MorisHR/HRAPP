namespace HRMS.Application.DTOs.ProjectDtos;

public class CreateProjectDto
{
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ClientName { get; set; }
    public string ProjectType { get; set; } = "Client";
    public bool IsBillable { get; set; } = true;
    public decimal? BillingRate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? BudgetHours { get; set; }
    public decimal? BudgetAmount { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ProjectManagerId { get; set; }
    public bool AllowTimeEntry { get; set; } = true;
    public bool RequireApproval { get; set; } = false;
}
