using HRMS.Application.DTOs.LocationDtos;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Locations Management API
/// Handles CRUD operations for physical work locations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        ILocationService locationService,
        ILogger<LocationsController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all locations
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,HR,Manager,TenantEmployee")]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
    {
        try
        {
            var locations = await _locationService.GetAllLocationsAsync(activeOnly);
            return Ok(new { success = true, data = locations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving locations");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving locations" });
        }
    }

    /// <summary>
    /// Get a single location by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,HR,Manager,TenantEmployee")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var location = await _locationService.GetLocationByIdAsync(id);

            if (location == null)
            {
                return NotFound(new { success = false, error = "Location not found" });
            }

            return Ok(new { success = true, data = location });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving the location" });
        }
    }

    /// <summary>
    /// Get location by code
    /// </summary>
    [HttpGet("by-code/{code}")]
    [Authorize(Roles = "Admin,HR,Manager,TenantEmployee")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var location = await _locationService.GetLocationByCodeAsync(code);

            if (location == null)
            {
                return NotFound(new { success = false, error = "Location not found" });
            }

            return Ok(new { success = true, data = location });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location by code {Code}", code);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving the location" });
        }
    }

    /// <summary>
    /// Get locations for dropdown (lightweight)
    /// </summary>
    [HttpGet("dropdown")]
    [Authorize(Roles = "Admin,HR,Manager,TenantEmployee")]
    public async Task<IActionResult> GetDropdown([FromQuery] bool activeOnly = true)
    {
        try
        {
            var locations = await _locationService.GetLocationsForDropdownAsync(activeOnly);
            return Ok(new { success = true, data = locations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving locations dropdown");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving locations" });
        }
    }

    /// <summary>
    /// Create a new location
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Create([FromBody] CreateLocationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, error = "Invalid input data", errors = ModelState });
        }

        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            var locationId = await _locationService.CreateLocationAsync(dto, userEmail);

            return CreatedAtAction(
                nameof(GetById),
                new { id = locationId },
                new { success = true, message = "Location created successfully", id = locationId }
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create location: {Message}", ex.Message);
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location");
            return StatusCode(500, new { success = false, error = "An error occurred while creating the location" });
        }
    }

    /// <summary>
    /// Update an existing location
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLocationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, error = "Invalid input data", errors = ModelState });
        }

        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            await _locationService.UpdateLocationAsync(id, dto, userEmail);

            return Ok(new { success = true, message = "Location updated successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update location: {Message}", ex.Message);
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while updating the location" });
        }
    }

    /// <summary>
    /// Delete a location
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            await _locationService.DeleteLocationAsync(id, userEmail);

            return Ok(new { success = true, message = "Location deleted successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to delete location: {Message}", ex.Message);
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting location {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while deleting the location" });
        }
    }

    /// <summary>
    /// Get device count for a location
    /// </summary>
    [HttpGet("{id:guid}/device-count")]
    [Authorize(Roles = "Admin,HR,Manager,TenantEmployee")]
    public async Task<IActionResult> GetDeviceCount(Guid id)
    {
        try
        {
            var count = await _locationService.GetDeviceCountByLocationAsync(id);
            return Ok(new { success = true, data = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device count for location {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving device count" });
        }
    }

    /// <summary>
    /// Get employee count for a location
    /// </summary>
    [HttpGet("{id:guid}/employee-count")]
    [Authorize(Roles = "Admin,HR,Manager,TenantEmployee")]
    public async Task<IActionResult> GetEmployeeCount(Guid id)
    {
        try
        {
            var count = await _locationService.GetEmployeeCountByLocationAsync(id);
            return Ok(new { success = true, data = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee count for location {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving employee count" });
        }
    }
}
