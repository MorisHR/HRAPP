namespace HRMS.Application.DTOs.DepartmentIntelligenceDtos;

/// <summary>
/// Turnover risk analysis with at-risk employees and predictions
/// </summary>
public class TurnoverRiskAnalysisDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// Overall turnover risk: none, low, medium, high, critical
    /// </summary>
    public string TurnoverRisk { get; set; } = string.Empty;

    /// <summary>
    /// Risk score (0-100)
    /// </summary>
    public int RiskScore { get; set; }

    public int AtRiskEmployees { get; set; }
    public int TotalEmployees { get; set; }

    /// <summary>
    /// Percentage of employees at risk
    /// </summary>
    public decimal RiskPercentage { get; set; }

    public TurnoverPredictionDto Predictions { get; set; } = new();
    public List<TurnoverRiskFactorDto> RiskFactors { get; set; } = new();
    public List<AtRiskEmployeeDto> AtRiskEmployeesList { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();

    public DateTime ComputedAt { get; set; }
    public DateTime CacheExpiresAt { get; set; }
}

public class TurnoverPredictionDto
{
    /// <summary>
    /// Expected exits in next 90 days
    /// </summary>
    public int ExpectedExitsNext90Days { get; set; }

    /// <summary>
    /// Confidence level (0.0 - 1.0)
    /// </summary>
    public decimal ConfidenceLevel { get; set; }

    /// <summary>
    /// Estimated cost of turnover
    /// </summary>
    public decimal EstimatedCost { get; set; }
}

public class TurnoverRiskFactorDto
{
    public string Factor { get; set; } = string.Empty;

    /// <summary>
    /// Impact: low, medium, high
    /// </summary>
    public string Impact { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public int AffectedEmployees { get; set; }
}

public class AtRiskEmployeeDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public List<string> RiskReasons { get; set; } = new();
    public string RecommendedAction { get; set; } = string.Empty;
}
