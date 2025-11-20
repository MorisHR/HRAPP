using HRMS.Application.DTOs.DepartmentIntelligenceDtos;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// Department Intelligence API - Analytics and insights
/// </summary>
[ApiController]
[Route("api/department/{departmentId}/intelligence")]
[Authorize]
public class DepartmentIntelligenceController : ControllerBase
{
    private readonly IDepartmentIntelligenceService _intelligenceService;
    private readonly ILogger<DepartmentIntelligenceController> _logger;

    public DepartmentIntelligenceController(
        IDepartmentIntelligenceService intelligenceService,
        ILogger<DepartmentIntelligenceController> logger)
    {
        _intelligenceService = intelligenceService;
        _logger = logger;
    }

    /// <summary>
    /// Get department health score with metrics and risk factors
    /// </summary>
    /// <remarks>
    /// Returns overall health score (0-100) based on:
    /// - Turnover rate
    /// - Average tenure
    /// - Employee satisfaction
    /// - Budget variance
    /// - Performance metrics
    ///
    /// Cached for 15 minutes for optimal performance.
    /// </remarks>
    [HttpGet("health-score")]
    [ProducesResponseType(typeof(DepartmentHealthScoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DepartmentHealthScoreDto>> GetHealthScore(Guid departmentId)
    {
        try
        {
            _logger.LogInformation("Getting health score for department {DepartmentId}", departmentId);

            var result = await _intelligenceService.GetHealthScoreAsync(departmentId);

            if (result.HealthStatus == "not_found")
            {
                return NotFound($"Department {departmentId} not found");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health score for department {DepartmentId}", departmentId);
            return StatusCode(500, "An error occurred while computing health score");
        }
    }

    /// <summary>
    /// Get turnover risk analysis with at-risk employees
    /// </summary>
    /// <remarks>
    /// Predicts turnover risk based on:
    /// - Recent terminations
    /// - Employee tenure patterns
    /// - Salary vs market
    /// - Workload indicators
    ///
    /// Returns list of at-risk employees with risk scores and recommended actions.
    /// Cached for 15 minutes.
    /// </remarks>
    [HttpGet("turnover-risk")]
    [ProducesResponseType(typeof(TurnoverRiskAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TurnoverRiskAnalysisDto>> GetTurnoverRisk(Guid departmentId)
    {
        try
        {
            _logger.LogInformation("Getting turnover risk for department {DepartmentId}", departmentId);

            var result = await _intelligenceService.GetTurnoverRiskAsync(departmentId);

            if (result.TurnoverRisk == "not_found")
            {
                return NotFound($"Department {departmentId} not found");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting turnover risk for department {DepartmentId}", departmentId);
            return StatusCode(500, "An error occurred while computing turnover risk");
        }
    }

    /// <summary>
    /// Get workload distribution showing overloaded and underutilized employees
    /// </summary>
    /// <remarks>
    /// Analyzes workload distribution based on timesheet data (last 8 weeks):
    /// - Overloaded employees (>45 hours/week)
    /// - Underutilized employees (<32 hours/week)
    /// - Burnout risk indicators
    /// - Workload rebalancing recommendations
    ///
    /// Cached for 5 minutes (more dynamic data).
    /// </remarks>
    [HttpGet("workload")]
    [ProducesResponseType(typeof(WorkloadDistributionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorkloadDistributionDto>> GetWorkloadDistribution(Guid departmentId)
    {
        try
        {
            _logger.LogInformation("Getting workload distribution for department {DepartmentId}", departmentId);

            var result = await _intelligenceService.GetWorkloadDistributionAsync(departmentId);

            if (result.WorkloadBalance == "not_found")
            {
                return NotFound($"Department {departmentId} not found");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workload distribution for department {DepartmentId}", departmentId);
            return StatusCode(500, "An error occurred while computing workload distribution");
        }
    }
}
