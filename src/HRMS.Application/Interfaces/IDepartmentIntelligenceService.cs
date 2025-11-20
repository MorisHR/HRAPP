using HRMS.Application.DTOs.DepartmentIntelligenceDtos;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Department intelligence service providing analytics and insights
/// </summary>
public interface IDepartmentIntelligenceService
{
    /// <summary>
    /// Get department health score with metrics and risk factors
    /// </summary>
    Task<DepartmentHealthScoreDto> GetHealthScoreAsync(Guid departmentId);

    /// <summary>
    /// Get turnover risk analysis with at-risk employees
    /// </summary>
    Task<TurnoverRiskAnalysisDto> GetTurnoverRiskAsync(Guid departmentId);

    /// <summary>
    /// Get workload distribution showing overloaded and underutilized employees
    /// </summary>
    Task<WorkloadDistributionDto> GetWorkloadDistributionAsync(Guid departmentId);
}
