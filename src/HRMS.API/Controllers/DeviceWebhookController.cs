using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HRMS.Application.DTOs.DeviceWebhookDtos;
using HRMS.Application.Interfaces;

namespace HRMS.API.Controllers;

/// <summary>
/// Device Webhook Controller - Receives push data from biometric devices
/// This controller is designed for IoT devices to push attendance data to the backend
/// No JWT authentication required - uses device API keys instead
/// </summary>
[ApiController]
[Route("api/device-webhook")]
[Authorize] // Default secure - specific endpoints allow anonymous access
public class DeviceWebhookController : ControllerBase
{
    private readonly IDeviceWebhookService _webhookService;
    private readonly ILogger<DeviceWebhookController> _logger;

    public DeviceWebhookController(
        IDeviceWebhookService webhookService,
        ILogger<DeviceWebhookController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Receive attendance data from biometric devices
    /// POST /api/device-webhook/attendance
    /// </summary>
    /// <remarks>
    /// This endpoint is called by biometric devices to push attendance punch data.
    ///
    /// Sample Request:
    /// ```json
    /// {
    ///   "deviceId": "ZK001",
    ///   "apiKey": "device-api-key-here",
    ///   "timestamp": "2025-11-13T12:00:00Z",
    ///   "records": [
    ///     {
    ///       "employeeId": "EMP001",
    ///       "punchTime": "2025-11-13T08:30:00Z",
    ///       "punchType": 0,
    ///       "verifyMode": 1,
    ///       "deviceRecordId": "12345"
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    [HttpPost("attendance")]
    [AllowAnonymous] // Biometric devices use API key authentication
    [ProducesResponseType(typeof(DeviceWebhookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeviceWebhookResponseDto>> ReceiveAttendance(
        [FromBody] DevicePushAttendanceDto dto)
    {
        try
        {
            _logger.LogInformation(
                "📥 Webhook: Received attendance data from device {DeviceId} with {RecordCount} records",
                dto.DeviceId,
                dto.Records.Count);

            // Validate and process the attendance data
            var result = await _webhookService.ProcessAttendanceDataAsync(dto);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "⚠️ Webhook: Failed to process attendance from device {DeviceId}: {Message}",
                    dto.DeviceId,
                    result.Message);

                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Failed to process attendance data",
                    Detail = result.Message
                });
            }

            _logger.LogInformation(
                "✅ Webhook: Successfully processed {Processed} records from device {DeviceId} ({Skipped} skipped)",
                result.RecordsProcessed,
                dto.DeviceId,
                result.RecordsSkipped);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "🔒 Webhook: Unauthorized device access attempt - DeviceId: {DeviceId}, Error: {Error}",
                dto.DeviceId,
                ex.Message);

            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid device credentials",
                Detail = "Device API key is invalid or device is not authorized"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Webhook: Error processing attendance from device {DeviceId}",
                dto.DeviceId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal server error",
                Detail = "An error occurred while processing attendance data"
            });
        }
    }

    /// <summary>
    /// Receive heartbeat/status updates from biometric devices
    /// POST /api/device-webhook/heartbeat
    /// </summary>
    /// <remarks>
    /// Devices can send periodic heartbeat signals to update their online status
    /// and provide device health information.
    /// </remarks>
    [HttpPost("heartbeat")]
    [AllowAnonymous] // Biometric devices use API key authentication
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ReceiveHeartbeat([FromBody] DeviceHeartbeatDto dto)
    {
        try
        {
            _logger.LogInformation(
                "💓 Webhook: Heartbeat from device {DeviceId} - Online: {IsOnline}, Users: {UserCount}",
                dto.DeviceId,
                dto.Status.IsOnline,
                dto.Status.UserCount);

            await _webhookService.ProcessHeartbeatAsync(dto);

            return Ok(new
            {
                success = true,
                message = "Heartbeat received",
                timestamp = DateTime.UtcNow
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                "🔒 Webhook: Unauthorized heartbeat from device {DeviceId}: {Error}",
                dto.DeviceId,
                ex.Message);

            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid device credentials",
                Detail = "Device API key is invalid or device is not authorized"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Webhook: Error processing heartbeat from device {DeviceId}",
                dto.DeviceId);

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal server error",
                Detail = "An error occurred while processing heartbeat"
            });
        }
    }

    /// <summary>
    /// Test endpoint for device webhook connectivity
    /// GET /api/device-webhook/ping
    /// </summary>
    [HttpGet("ping")]
    [AllowAnonymous] // Health check endpoint for device connectivity testing
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Ping()
    {
        return Ok(new
        {
            success = true,
            message = "Device webhook endpoint is reachable",
            timestamp = DateTime.UtcNow,
            serverTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }
}
