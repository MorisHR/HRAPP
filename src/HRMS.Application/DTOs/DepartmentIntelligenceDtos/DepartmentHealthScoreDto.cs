namespace HRMS.Application.DTOs.DepartmentIntelligenceDtos;

/// <summary>
/// Department health score with metrics and risk factors
/// </summary>
public class DepartmentHealthScoreDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// Overall health score (0-100)
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    /// Health status: excellent (80-100), good (60-79), moderate (40-59), poor (20-39), critical (0-19)
    /// </summary>
    public string HealthStatus { get; set; } = string.Empty;

    public DepartmentMetricsDto Metrics { get; set; } = new();
    public List<RiskFactorDto> RiskFactors { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();

    public DateTime ComputedAt { get; set; }
    public DateTime CacheExpiresAt { get; set; }
}

public class DepartmentMetricsDto
{
    /// <summary>
    /// Turnover rate (% per year)
    /// </summary>
    public decimal TurnoverRate { get; set; }

    /// <summary>
    /// Average tenure in years
    /// </summary>
    public decimal AvgTenureYears { get; set; }

    /// <summary>
    /// Employee satisfaction proxy (0-100)
    /// </summary>
    public decimal EmployeeSatisfaction { get; set; }

    /// <summary>
    /// Budget variance (% over/under)
    /// </summary>
    public decimal BudgetVariance { get; set; }

    /// <summary>
    /// Average performance rating
    /// </summary>
    public decimal AvgPerformance { get; set; }

    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
}

public class RiskFactorDto
{
    public string Factor { get; set; } = string.Empty;

    /// <summary>
    /// Severity: low, medium, high, critical
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    public decimal Value { get; set; }
    public decimal Benchmark { get; set; }
    public string Description { get; set; } = string.Empty;
}
