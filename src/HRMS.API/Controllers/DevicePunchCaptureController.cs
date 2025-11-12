using HRMS.Application.DTOs.BiometricPunchDtos;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// Device Punch Capture API - Biometric Device Integration Endpoint
///
/// AUTHENTICATION: Uses DeviceApiKeyAuthenticationMiddleware (X-Device-API-Key header)
/// AUTHORIZATION: No role-based auth - devices don't have user roles
/// RATE LIMITING: 60 requests/minute per device
///
/// PURPOSE:
/// This controller receives real-time punch events from biometric attendance devices
/// (ZKTeco, Suprema, Anviz, etc.) and processes them into attendance records.
///
/// SECURITY FEATURES:
/// - Device API key authentication (middleware)
/// - Rate limiting (60 req/min)
/// - Duplicate punch detection
/// - Tamper-proof hash chain audit trail
/// - Comprehensive request/response logging
///
/// PRODUCTION-READY:
/// - Comprehensive error handling
/// - Detailed audit logging
/// - Health monitoring endpoints
/// - Performance optimized for high-volume device traffic
/// </summary>
[ApiController]
[Route("api/device")]
public class DevicePunchCaptureController : ControllerBase
{
    private readonly IBiometricPunchProcessingService _punchProcessingService;
    private readonly IBiometricDeviceService _deviceService;
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<DevicePunchCaptureController> _logger;

    public DevicePunchCaptureController(
        IBiometricPunchProcessingService punchProcessingService,
        IBiometricDeviceService deviceService,
        IRateLimitService rateLimitService,
        ILogger<DevicePunchCaptureController> logger)
    {
        _punchProcessingService = punchProcessingService;
        _deviceService = deviceService;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    /// <summary>
    /// PRIMARY ENDPOINT: Capture biometric punch from device
    ///
    /// FLOW:
    /// 1. Extract DeviceId and TenantId from HttpContext.Items (set by middleware)
    /// 2. Apply rate limiting (60 requests/minute per device)
    /// 3. Validate request model
    /// 4. Process punch (duplicate detection, employee mapping, attendance creation)
    /// 5. Return detailed result with success/warning/error information
    ///
    /// ERROR HANDLING:
    /// - 400 Bad Request: Invalid punch data
    /// - 401 Unauthorized: Invalid API key (handled by middleware)
    /// - 403 Forbidden: Device inactive or expired (handled by middleware)
    /// - 422 Unprocessable: Duplicate punch detected
    /// - 429 Too Many Requests: Rate limit exceeded
    /// - 500 Internal Server Error: Processing failed
    /// </summary>
    /// <remarks>
    /// Sample Request:
    /// POST /api/device/capture-punch
    /// Headers:
    ///   X-Device-API-Key: 1a2b3c4d5e6f7g8h9i0j
    ///
    /// Body:
    /// {
    ///   "deviceSerialNumber": "ZKT-F18-001",
    ///   "deviceUserId": "12345",
    ///   "punchTime": "2025-11-11T08:30:00Z",
    ///   "punchType": "CheckIn",
    ///   "verificationMethod": "Fingerprint",
    ///   "verificationQuality": 95,
    ///   "latitude": 40.7128,
    ///   "longitude": -74.0060
    /// }
    ///
    /// Response (200 OK):
    /// {
    ///   "success": true,
    ///   "message": "Punch processed successfully",
    ///   "punchRecordId": "guid",
    ///   "attendanceId": "guid",
    ///   "warnings": [],
    ///   "errors": []
    /// }
    /// </remarks>
    [HttpPost("capture-punch")]
    [ProducesResponseType(typeof(PunchProcessingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CapturePunch([FromBody] DevicePunchCaptureDto punchDto)
    {
        var requestStartTime = DateTime.UtcNow;
        Guid? deviceId = null;
        Guid? tenantId = null;

        try
        {
            // ============================================
            // STEP 1: Extract Device and Tenant Context
            // ============================================
            // Middleware sets these in HttpContext.Items after validating API key
            if (!HttpContext.Items.TryGetValue("DeviceId", out var deviceIdObj) || deviceIdObj is not Guid)
            {
                _logger.LogError("DeviceId not found in HttpContext.Items - middleware may have failed");
                return Unauthorized(new
                {
                    success = false,
                    message = "Device authentication failed. Please check your API key.",
                    error = "DEVICE_AUTH_FAILED"
                });
            }

            if (!HttpContext.Items.TryGetValue("TenantId", out var tenantIdObj) || tenantIdObj is not Guid)
            {
                _logger.LogError("TenantId not found in HttpContext.Items - middleware may have failed");
                return Unauthorized(new
                {
                    success = false,
                    message = "Tenant context not found. Please check your device configuration.",
                    error = "TENANT_CONTEXT_MISSING"
                });
            }

            deviceId = (Guid)deviceIdObj;
            tenantId = (Guid)tenantIdObj;

            // ============================================
            // STEP 2: Rate Limiting (60 requests/minute)
            // ============================================
            var rateLimitKey = $"device_punch:{deviceId}";
            var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(
                rateLimitKey,
                limit: 60,
                window: TimeSpan.FromMinutes(1));

            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for device {DeviceId}. Current: {Current}, Limit: {Limit}, Resets at: {ResetsAt}",
                    deviceId, rateLimitResult.CurrentCount, rateLimitResult.Limit, rateLimitResult.ResetsAt);

                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    success = false,
                    message = "Rate limit exceeded. Please slow down your requests.",
                    error = "RATE_LIMIT_EXCEEDED",
                    details = new
                    {
                        limit = rateLimitResult.Limit,
                        remaining = rateLimitResult.Remaining,
                        resetsAt = rateLimitResult.ResetsAt,
                        retryAfterSeconds = rateLimitResult.RetryAfterSeconds
                    }
                });
            }

            // ============================================
            // STEP 3: Validate Request Model
            // ============================================
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning(
                    "Invalid punch data from device {DeviceId}: {Errors}",
                    deviceId, string.Join("; ", errors));

                return BadRequest(new
                {
                    success = false,
                    message = "Invalid punch data",
                    error = "VALIDATION_FAILED",
                    errors = errors
                });
            }

            // ============================================
            // STEP 4: Process Punch
            // ============================================
            _logger.LogInformation(
                "Processing punch from device {DeviceId}, tenant {TenantId}: User {DeviceUserId}, Type {PunchType}, Time {PunchTime}",
                deviceId, tenantId, punchDto.DeviceUserId, punchDto.PunchType, punchDto.PunchTime);

            var result = await _punchProcessingService.ProcessPunchAsync(
                punchDto,
                deviceId.Value,
                tenantId.Value);

            var processingTimeMs = (DateTime.UtcNow - requestStartTime).TotalMilliseconds;

            // ============================================
            // STEP 5: Handle Processing Result
            // ============================================
            if (result.Success)
            {
                _logger.LogInformation(
                    "Punch processed successfully from device {DeviceId}: PunchRecordId {PunchRecordId}, AttendanceId {AttendanceId}, Processing time: {ProcessingTime}ms",
                    deviceId, result.PunchRecordId, result.AttendanceId, processingTimeMs);

                // Log warnings if any
                if (result.HasWarnings)
                {
                    _logger.LogWarning(
                        "Punch processed with warnings from device {DeviceId}: {Warnings}",
                        deviceId, string.Join("; ", result.Warnings));
                }

                return Ok(result);
            }
            else
            {
                // Processing failed - check if it's a duplicate
                if (result.Errors.Any(e => e.Contains("duplicate", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning(
                        "Duplicate punch detected from device {DeviceId}: {Errors}",
                        deviceId, string.Join("; ", result.Errors));

                    return StatusCode(StatusCodes.Status422UnprocessableEntity, new
                    {
                        success = false,
                        message = result.Message,
                        error = "DUPLICATE_PUNCH",
                        errors = result.Errors,
                        warnings = result.Warnings
                    });
                }

                // Other processing errors
                _logger.LogError(
                    "Punch processing failed from device {DeviceId}: {Errors}",
                    deviceId, string.Join("; ", result.Errors));

                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    error = "PROCESSING_FAILED",
                    errors = result.Errors,
                    warnings = result.Warnings
                });
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access attempt from device {DeviceId}", deviceId);
            return Unauthorized(new
            {
                success = false,
                message = "Device not authorized for this operation",
                error = "DEVICE_NOT_AUTHORIZED"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument from device {DeviceId}: {Message}", deviceId, ex.Message);
            return BadRequest(new
            {
                success = false,
                message = ex.Message,
                error = "INVALID_ARGUMENT"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Fatal error processing punch from device {DeviceId}, tenant {TenantId}: {Message}",
                deviceId, tenantId, ex.Message);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "An error occurred while processing the punch. Please try again.",
                error = "INTERNAL_SERVER_ERROR",
                errorId = Guid.NewGuid().ToString() // For support tracking
            });
        }
    }

    /// <summary>
    /// Health check endpoint for devices
    ///
    /// PURPOSE:
    /// - Verify API connectivity
    /// - Check server status
    /// - Get current server time for clock synchronization
    ///
    /// NO AUTHENTICATION REQUIRED
    /// This endpoint is public to allow devices to check connectivity before authentication
    /// </summary>
    /// <remarks>
    /// Sample Request:
    /// GET /api/device/health
    ///
    /// Response (200 OK):
    /// {
    ///   "status": "online",
    ///   "serverTime": "2025-11-11T08:30:00Z",
    ///   "version": "1.0.0",
    ///   "message": "Device API is operational"
    /// }
    /// </remarks>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        var response = new
        {
            status = "online",
            serverTime = DateTime.UtcNow,
            version = "1.0.0",
            message = "Device API is operational"
        };

        _logger.LogDebug("Health check request from {IpAddress}", GetClientIpAddress());

        return Ok(response);
    }

    /// <summary>
    /// Get sync status for authenticated device
    ///
    /// PURPOSE:
    /// - Check last sync time
    /// - Verify device configuration
    /// - Get sync interval settings
    ///
    /// REQUIRES: Device authentication (X-Device-API-Key)
    /// </summary>
    /// <remarks>
    /// Sample Request:
    /// GET /api/device/sync-status
    /// Headers:
    ///   X-Device-API-Key: 1a2b3c4d5e6f7g8h9i0j
    ///
    /// Response (200 OK):
    /// {
    ///   "deviceId": "guid",
    ///   "deviceCode": "ZKT-F18-001",
    ///   "machineName": "Main Entrance",
    ///   "syncEnabled": true,
    ///   "lastSyncTime": "2025-11-11T08:00:00Z",
    ///   "minutesSinceLastSync": 30,
    ///   "isOnline": true
    /// }
    /// </remarks>
    [HttpGet("sync-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSyncStatus()
    {
        try
        {
            // Extract device context from middleware
            if (!HttpContext.Items.TryGetValue("DeviceId", out var deviceIdObj) || deviceIdObj is not Guid deviceId)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Device authentication failed"
                });
            }

            var syncStatus = await _deviceService.GetDeviceSyncStatusByIdAsync(deviceId);

            if (syncStatus == null)
            {
                _logger.LogWarning("Sync status not found for device {DeviceId}", deviceId);
                return NotFound(new
                {
                    success = false,
                    message = "Device sync status not found"
                });
            }

            _logger.LogInformation("Sync status requested by device {DeviceId}", deviceId);

            return Ok(new
            {
                success = true,
                data = syncStatus,
                serverTime = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sync status");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Error retrieving sync status"
            });
        }
    }

    /// <summary>
    /// Get recent punch history for authenticated device
    ///
    /// PURPOSE:
    /// - Verify submitted punches
    /// - Check processing status
    /// - Debug punch issues
    ///
    /// REQUIRES: Device authentication (X-Device-API-Key)
    /// </summary>
    /// <param name="hours">Number of hours to look back (default: 24, max: 168)</param>
    /// <remarks>
    /// Sample Request:
    /// GET /api/device/punch-history?hours=24
    /// Headers:
    ///   X-Device-API-Key: 1a2b3c4d5e6f7g8h9i0j
    ///
    /// Response (200 OK):
    /// {
    ///   "success": true,
    ///   "data": [
    ///     {
    ///       "id": "guid",
    ///       "deviceUserId": "12345",
    ///       "punchTime": "2025-11-11T08:30:00Z",
    ///       "punchType": "CheckIn",
    ///       "processingStatus": "Processed",
    ///       "employeeName": "John Doe",
    ///       "attendanceId": "guid"
    ///     }
    ///   ],
    ///   "total": 15,
    ///   "hours": 24
    /// }
    /// </remarks>
    [HttpGet("punch-history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPunchHistory(
        [FromQuery] int hours = 24,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate hours parameter
            if (hours < 1 || hours > 168) // Max 7 days
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Hours parameter must be between 1 and 168 (7 days)"
                });
            }

            // Extract device context from middleware
            if (!HttpContext.Items.TryGetValue("DeviceId", out var deviceIdObj) || deviceIdObj is not Guid deviceId)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Device authentication failed"
                });
            }

            // Calculate date range
            var toDate = DateTime.UtcNow;
            var fromDate = toDate.AddHours(-hours);

            _logger.LogInformation(
                "Punch history requested by device {DeviceId}: {Hours} hours ({FromDate} to {ToDate}), Page: {Page}, PageSize: {PageSize}",
                deviceId, hours, fromDate, toDate, page, pageSize);

            // Get punches from service with pagination
            var result = await _punchProcessingService.GetPunchesByDeviceAsync(
                deviceId,
                fromDate,
                toDate,
                page,
                pageSize);

            _logger.LogInformation(
                "Retrieved {Count} punches (Total: {TotalCount}, Page: {Page}/{TotalPages}) for device {DeviceId}",
                result.Items.Count, result.TotalCount, result.PageNumber, result.TotalPages, deviceId);

            return Ok(new
            {
                success = true,
                data = result.Items,
                pagination = new
                {
                    currentPage = result.PageNumber,
                    pageSize = result.PageSize,
                    totalRecords = result.TotalCount,
                    totalPages = result.TotalPages,
                    hasNextPage = result.HasNextPage,
                    hasPreviousPage = result.HasPreviousPage
                },
                filters = new
                {
                    hours = hours,
                    startDate = fromDate,
                    endDate = toDate
                },
                message = $"Retrieved {result.Items.Count} punch records"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving punch history for device");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Error retrieving punch history"
            });
        }
    }

    // ============================================
    // PRIVATE HELPER METHODS
    // ============================================

    /// <summary>
    /// Get client IP address from request
    /// Handles X-Forwarded-For header for reverse proxies
    /// </summary>
    private string GetClientIpAddress()
    {
        // Check X-Forwarded-For header (set by reverse proxies/load balancers)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Take first IP (client IP)
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    return ips[0].Trim();
                }
            }
        }

        // Fallback to direct connection IP
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
