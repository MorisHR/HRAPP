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
}
