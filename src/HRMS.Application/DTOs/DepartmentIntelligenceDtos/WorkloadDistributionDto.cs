namespace HRMS.Application.DTOs.DepartmentIntelligenceDtos;

/// <summary>
/// Workload distribution analysis showing overloaded and underutilized employees
/// </summary>
public class WorkloadDistributionDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// Workload balance: excellent, good, fair, poor, critical
    /// </summary>
    public string WorkloadBalance { get; set; } = string.Empty;

    public WorkloadSummaryDto Summary { get; set; } = new();
    public List<OverloadedEmployeeDto> OverloadedEmployees { get; set; } = new();
    public List<UnderutilizedEmployeeDto> UnderutilizedEmployees { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();

    public DateTime ComputedAt { get; set; }
    public DateTime CacheExpiresAt { get; set; }
}

public class WorkloadSummaryDto
{
    public int TotalEmployees { get; set; }
    public int OverloadedCount { get; set; }
    public int UnderutilizedCount { get; set; }
    public int BalancedCount { get; set; }

    public decimal AvgWeeklyHours { get; set; }
    public decimal TargetWeeklyHours { get; set; }

    /// <summary>
    /// Workload imbalance score (0-100, higher = worse)
    /// </summary>
    public int ImbalanceScore { get; set; }
}

public class OverloadedEmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Average weekly hours
    /// </summary>
    public decimal AvgWeeklyHours { get; set; }

    /// <summary>
    /// Expected threshold (usually 40-45 hours)
    /// </summary>
    public decimal Threshold { get; set; }

    /// <summary>
    /// Percentage over threshold
    /// </summary>
    public decimal OverloadPercentage { get; set; }

    /// <summary>
    /// Number of consecutive weeks overloaded
    /// </summary>
    public int ConsecutiveWeeks { get; set; }

    /// <summary>
    /// Burnout risk: low, medium, high, critical
    /// </summary>
    public string BurnoutRisk { get; set; } = string.Empty;

    public string RecommendedAction { get; set; } = string.Empty;
}

public class UnderutilizedEmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;

    public decimal AvgWeeklyHours { get; set; }
    public decimal Threshold { get; set; }

    /// <summary>
    /// Utilization percentage (0-100)
    /// </summary>
    public decimal UtilizationPercentage { get; set; }

    /// <summary>
    /// Available hours per week
    /// </summary>
    public decimal AvailableHours { get; set; }

    public string Suggestion { get; set; } = string.Empty;
}
