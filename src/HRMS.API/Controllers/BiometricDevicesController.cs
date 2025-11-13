using HRMS.Application.DTOs.BiometricDeviceDtos;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Biometric Devices Management API
/// Handles CRUD operations for biometric attendance devices
/// </summary>
[ApiController]
[Route("api/biometric-devices")]
[Authorize]
public class BiometricDevicesController : ControllerBase
{
    private readonly IBiometricDeviceService _deviceService;
    private readonly ILogger<BiometricDevicesController> _logger;

    public BiometricDevicesController(
        IBiometricDeviceService deviceService,
        ILogger<BiometricDevicesController> logger)
    {
        _deviceService = deviceService;
        _logger = logger;
    }

    /// <summary>
    /// Get all biometric devices
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
    {
        try
        {
            var devices = await _deviceService.GetAllDevicesAsync(activeOnly);
            return Ok(new { success = true, data = devices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving biometric devices");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving devices" });
        }
    }

    /// <summary>
    /// Get a single biometric device by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);

            if (device == null)
            {
                return NotFound(new { success = false, error = "Device not found" });
            }

            return Ok(new { success = true, data = device });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving the device" });
        }
    }

    /// <summary>
    /// Get device by code
    /// </summary>
    [HttpGet("by-code/{code}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var device = await _deviceService.GetDeviceByCodeAsync(code);

            if (device == null)
            {
                return NotFound(new { success = false, error = "Device not found" });
            }

            return Ok(new { success = true, data = device });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device by code {Code}", code);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving the device" });
        }
    }

    /// <summary>
    /// Get devices by location
    /// </summary>
    [HttpGet("by-location/{locationId:guid}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetByLocation(Guid locationId, [FromQuery] bool activeOnly = true)
    {
        try
        {
            var devices = await _deviceService.GetDevicesByLocationAsync(locationId, activeOnly);
            return Ok(new { success = true, data = devices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving devices for location {LocationId}", locationId);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving devices" });
        }
    }

    /// <summary>
    /// Get devices for dropdown (lightweight)
    /// </summary>
    [HttpGet("dropdown")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetDropdown([FromQuery] bool activeOnly = true)
    {
        try
        {
            var devices = await _deviceService.GetDevicesForDropdownAsync(activeOnly);
            return Ok(new { success = true, data = devices });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving devices dropdown");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving devices" });
        }
    }

    /// <summary>
    /// Create a new biometric device
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Create([FromBody] CreateBiometricDeviceDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, error = "Invalid input data", errors = ModelState });
        }

        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            var deviceId = await _deviceService.CreateDeviceAsync(dto, userEmail);

            return CreatedAtAction(
                nameof(GetById),
                new { id = deviceId },
                new { success = true, message = "Device created successfully", id = deviceId }
            );
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create device: {Message}", ex.Message);
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device");
            return StatusCode(500, new { success = false, error = "An error occurred while creating the device" });
        }
    }

    /// <summary>
    /// Update an existing biometric device
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBiometricDeviceDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, error = "Invalid input data", errors = ModelState });
        }

        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            await _deviceService.UpdateDeviceAsync(id, dto, userEmail);

            return Ok(new { success = true, message = "Device updated successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update device: {Message}", ex.Message);
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while updating the device" });
        }
    }

    /// <summary>
    /// Delete a biometric device
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            await _deviceService.DeleteDeviceAsync(id, userEmail);

            return Ok(new { success = true, message = "Device deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while deleting the device" });
        }
    }

    /// <summary>
    /// Get sync status for all devices
    /// </summary>
    [HttpGet("sync-status")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetSyncStatus()
    {
        try
        {
            var syncStatus = await _deviceService.GetDeviceSyncStatusAsync();
            return Ok(new { success = true, data = syncStatus });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device sync status");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving sync status" });
        }
    }

    /// <summary>
    /// Get sync status for a specific device
    /// </summary>
    [HttpGet("{id:guid}/sync-status")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetSyncStatusById(Guid id)
    {
        try
        {
            var syncStatus = await _deviceService.GetDeviceSyncStatusByIdAsync(id);

            if (syncStatus == null)
            {
                return NotFound(new { success = false, error = "Device not found" });
            }

            return Ok(new { success = true, data = syncStatus });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sync status for device {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving sync status" });
        }
    }

    /// <summary>
    /// Test connection to a biometric device
    /// </summary>
    /// <remarks>
    /// POST /api/biometric-devices/test-connection
    /// {
    ///   "ipAddress": "192.168.1.100",
    ///   "port": 4370,
    ///   "connectionMethod": "TCP/IP",
    ///   "connectionTimeoutSeconds": 30
    /// }
    /// </remarks>
    [HttpPost("test-connection")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> TestConnection([FromBody] TestConnectionDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, error = "Invalid input data", errors = ModelState });
        }

        try
        {
            var result = await _deviceService.TestConnectionAsync(dto);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection to {IpAddress}:{Port}", dto.IpAddress, dto.Port);
            return StatusCode(500, new { success = false, error = "An error occurred while testing connection" });
        }
    }

    /// <summary>
    /// Manually trigger device sync
    /// </summary>
    /// <remarks>
    /// POST /api/biometric-devices/{id}/sync
    /// Triggers a background job to sync attendance data from the device
    /// </remarks>
    [HttpPost("{id:guid}/sync")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> SyncDevice(Guid id)
    {
        try
        {
            var result = await _deviceService.TriggerManualSyncAsync(id);
            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to trigger sync for device {Id}: {Message}", id, ex.Message);
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering sync for device {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while triggering device sync" });
        }
    }

    // ==========================================
    // API KEY MANAGEMENT ENDPOINTS
    // ==========================================

    /// <summary>
    /// Get all API keys for a device
    /// </summary>
    /// <remarks>
    /// GET /api/biometric-devices/{deviceId}/api-keys
    /// Returns all API keys with hashed values only (never plaintext)
    /// Shows: id, description, isActive, expiresAt, lastUsedAt, usageCount, createdAt
    /// </remarks>
    [HttpGet("{deviceId:guid}/api-keys")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetApiKeys(Guid deviceId)
    {
        try
        {
            var apiKeys = await _deviceService.GetDeviceApiKeysAsync(deviceId);
            return Ok(new { success = true, data = apiKeys });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API keys for device {DeviceId}", deviceId);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving API keys" });
        }
    }

    /// <summary>
    /// Generate a new API key for a device
    /// </summary>
    /// <remarks>
    /// POST /api/biometric-devices/{deviceId}/generate-api-key
    ///
    /// Request body:
    /// {
    ///   "description": "Production Device Key",
    ///   "expiresAt": "2025-12-31T23:59:59Z",
    ///   "allowedIpAddresses": "[\"192.168.1.100\"]",
    ///   "rateLimitPerMinute": 60
    /// }
    ///
    /// CRITICAL: The plaintext API key is returned ONLY ONCE
    /// It cannot be retrieved later - save it securely
    ///
    /// Response includes:
    /// - apiKeyId: Unique identifier for the key
    /// - plaintextKey: 64-character base64url string (SAVE THIS!)
    /// - description, expiresAt, isActive, createdAt
    /// - securityWarning: Reminder to save the key
    /// </remarks>
    [HttpPost("{deviceId:guid}/generate-api-key")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateApiKey(Guid deviceId, [FromBody] GenerateApiKeyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, error = "Invalid input data", errors = ModelState });
        }

        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            var response = await _deviceService.GenerateApiKeyAsync(
                deviceId,
                request.Description,
                userEmail);

            _logger.LogInformation(
                "API key {ApiKeyId} generated for device {DeviceId} by {User}",
                response.ApiKeyId,
                deviceId,
                userEmail);

            return Ok(new
            {
                success = true,
                message = "API key generated successfully. IMPORTANT: Save the plaintext key - it will not be shown again.",
                data = response
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating API key for device {DeviceId}", deviceId);
            return StatusCode(500, new { success = false, error = "An error occurred while generating the API key" });
        }
    }

    /// <summary>
    /// Revoke an API key
    /// </summary>
    /// <remarks>
    /// DELETE /api/biometric-devices/{deviceId}/api-keys/{apiKeyId}
    ///
    /// Marks the API key as inactive (isActive = false)
    /// The key is immediately rejected during authentication
    /// Does not delete from database (maintains audit trail)
    ///
    /// Use cases:
    /// - Key compromise suspected
    /// - Device decommissioned
    /// - Routine key rotation
    /// </remarks>
    [HttpDelete("{deviceId:guid}/api-keys/{apiKeyId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RevokeApiKey(Guid deviceId, Guid apiKeyId)
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            await _deviceService.RevokeApiKeyAsync(deviceId, apiKeyId, userEmail);

            _logger.LogInformation(
                "API key {ApiKeyId} revoked for device {DeviceId} by {User}",
                apiKeyId,
                deviceId,
                userEmail);

            return Ok(new
            {
                success = true,
                message = "API key revoked successfully. The key can no longer be used for authentication."
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {ApiKeyId} for device {DeviceId}", apiKeyId, deviceId);
            return StatusCode(500, new { success = false, error = "An error occurred while revoking the API key" });
        }
    }

    /// <summary>
    /// Rotate an API key (revoke old, generate new)
    /// </summary>
    /// <remarks>
    /// POST /api/biometric-devices/{deviceId}/api-keys/{apiKeyId}/rotate
    ///
    /// Atomic operation:
    /// 1. Generates a new API key with same settings
    /// 2. Marks the old key as inactive
    /// 3. Returns the new plaintext key (ONLY ONCE)
    ///
    /// Best practices:
    /// - Rotate keys every 90 days
    /// - Rotate immediately if compromise suspected
    /// - Update device configuration with new key
    ///
    /// CRITICAL: The new plaintext API key is returned ONLY ONCE
    /// Save it securely - it cannot be retrieved later
    /// </remarks>
    [HttpPost("{deviceId:guid}/api-keys/{apiKeyId:guid}/rotate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RotateApiKey(Guid deviceId, Guid apiKeyId)
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            var response = await _deviceService.RotateApiKeyAsync(deviceId, apiKeyId, userEmail);

            _logger.LogInformation(
                "API key {OldApiKeyId} rotated for device {DeviceId} by {User}. New key: {NewApiKeyId}",
                apiKeyId,
                deviceId,
                userEmail,
                response.ApiKeyId);

            return Ok(new
            {
                success = true,
                message = "API key rotated successfully. Old key revoked, new key generated. IMPORTANT: Save the new key - it will not be shown again.",
                data = response
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating API key {ApiKeyId} for device {DeviceId}", apiKeyId, deviceId);
            return StatusCode(500, new { success = false, error = "An error occurred while rotating the API key" });
        }
    }
}
