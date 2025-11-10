using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Application.Interfaces;
using HRMS.Core.Enums;

namespace HRMS.API.Controllers;

/// <summary>
/// Anomaly detection controller for security threat monitoring
/// FORTUNE 500 PATTERN: AWS GuardDuty Console, Splunk Security Dashboard
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,SecurityAdmin")]
public class AnomalyDetectionController : ControllerBase
{
    private readonly IAnomalyDetectionService _anomalyDetectionService;
    private readonly ILogger<AnomalyDetectionController> _logger;

    public AnomalyDetectionController(
        IAnomalyDetectionService anomalyDetectionService,
        ILogger<AnomalyDetectionController> logger)
    {
        _anomalyDetectionService = anomalyDetectionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets anomalies with filtering and pagination
    /// </summary>
    /// <param name="tenantId">Optional tenant filter</param>
    /// <param name="anomalyType">Optional anomaly type filter</param>
    /// <param name="riskLevel">Optional risk level filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size (default 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of anomalies</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAnomalies(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] AnomalyType? anomalyType = null,
        [FromQuery] AnomalyRiskLevel? riskLevel = null,
        [FromQuery] AnomalyStatus? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "Invalid pagination parameters" });
            }

            var (anomalies, totalCount) = await _anomalyDetectionService.GetAnomaliesAsync(
                tenantId, anomalyType, riskLevel, status, startDate, endDate,
                pageNumber, pageSize, cancellationToken);

            return Ok(new
            {
                data = anomalies,
                pagination = new
                {
                    pageNumber,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve anomalies");
            return StatusCode(500, new { error = "Failed to retrieve anomalies" });
        }
    }

    /// <summary>
    /// Gets a specific anomaly by ID
    /// </summary>
    /// <param name="id">Anomaly ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Anomaly details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAnomalyById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var anomaly = await _anomalyDetectionService.GetAnomalyByIdAsync(id, cancellationToken);

            if (anomaly == null)
            {
                return NotFound(new { error = $"Anomaly {id} not found" });
            }

            return Ok(anomaly);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve anomaly {AnomalyId}", id);
            return StatusCode(500, new { error = "Failed to retrieve anomaly" });
        }
    }

    /// <summary>
    /// Updates the investigation status of an anomaly
    /// </summary>
    /// <param name="id">Anomaly ID</param>
    /// <param name="request">Status update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated anomaly</returns>
    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateAnomalyStatus(
        Guid id,
        [FromBody] UpdateAnomalyStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get investigator ID from claims
            var investigatorIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid");
            if (investigatorIdClaim == null || !Guid.TryParse(investigatorIdClaim.Value, out var investigatorId))
            {
                return Unauthorized(new { error = "Invalid user claims" });
            }

            var anomaly = await _anomalyDetectionService.UpdateAnomalyStatusAsync(
                id,
                request.Status,
                investigatorId,
                request.InvestigationNotes,
                request.Resolution,
                cancellationToken);

            return Ok(anomaly);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update anomaly status for {AnomalyId}", id);
            return StatusCode(500, new { error = "Failed to update anomaly status" });
        }
    }

    /// <summary>
    /// Gets anomaly statistics for a date range
    /// </summary>
    /// <param name="tenantId">Optional tenant filter</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Anomaly statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            if (start > end)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            var statistics = await _anomalyDetectionService.GetAnomalyStatisticsAsync(
                tenantId, start, end, cancellationToken);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve anomaly statistics");
            return StatusCode(500, new { error = "Failed to retrieve statistics" });
        }
    }

    /// <summary>
    /// Gets top users with most anomalies
    /// </summary>
    /// <param name="tenantId">Optional tenant filter</param>
    /// <param name="daysBack">Number of days to analyze</param>
    /// <param name="topN">Number of top users to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top users with anomaly counts</returns>
    [HttpGet("top-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTopUsersWithAnomalies(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] int daysBack = 30,
        [FromQuery] int topN = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var topUsers = await _anomalyDetectionService.GetTopUsersWithAnomaliesAsync(
                tenantId, daysBack, topN, cancellationToken);

            return Ok(topUsers.Select(u => new
            {
                userId = u.userId,
                userEmail = u.userEmail,
                anomalyCount = u.anomalyCount
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve top users with anomalies");
            return StatusCode(500, new { error = "Failed to retrieve top users" });
        }
    }

    /// <summary>
    /// Manually triggers batch anomaly detection
    /// </summary>
    /// <param name="lookbackMinutes">Number of minutes to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of anomalies detected</returns>
    [HttpPost("batch-detection")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RunBatchDetection(
        [FromQuery] int lookbackMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var anomalyCount = await _anomalyDetectionService.RunBatchDetectionAsync(lookbackMinutes, cancellationToken);

            return Ok(new
            {
                message = "Batch anomaly detection completed",
                anomaliesDetected = anomalyCount,
                lookbackMinutes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run batch anomaly detection");
            return StatusCode(500, new { error = "Failed to run batch detection" });
        }
    }
}

/// <summary>
/// Request model for updating anomaly status
/// </summary>
public class UpdateAnomalyStatusRequest
{
    /// <summary>New status</summary>
    public AnomalyStatus Status { get; set; }

    /// <summary>Investigation notes</summary>
    public string? InvestigationNotes { get; set; }

    /// <summary>Resolution details (required for RESOLVED or FALSE_POSITIVE status)</summary>
    public string? Resolution { get; set; }
}
