using HRMS.Application.DTOs.LocationDtos;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Location Management API - Production-Ready Endpoints for Mauritius Geography
///
/// This controller provides comprehensive REST API endpoints for managing physical work locations
/// in the HRMS system, with specialized support for Mauritius geography (districts, cities, towns, villages).
///
/// Features:
/// - CRUD operations with role-based authorization
/// - Advanced filtering and pagination
/// - District-based queries (9 Mauritius districts)
/// - Full-text search across location data
/// - Geographic data with latitude/longitude
/// - Audit logging for all modifications
/// - Input validation and sanitization
/// - Comprehensive error handling
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<LocationController> _logger;

    public LocationController(
        ILocationService locationService,
        IAuditLogService auditLogService,
        ILogger<LocationController> logger)
    {
        _locationService = locationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get all locations with advanced filtering and pagination
    /// </summary>
    /// <param name="filter">Filter criteria (district, type, region, search term, pagination)</param>
    /// <returns>Paginated list of locations with total count</returns>
    /// <response code="200">Returns the paginated list of locations</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [Authorize(Roles = "Admin,HR,Manager")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLocations([FromQuery] LocationFilterDto filter)
    {
        try
        {
            var (locations, totalCount) = await _locationService.GetLocationsWithFilterAsync(filter);

            return Ok(new
            {
                success = true,
                data = locations,
                pagination = new
                {
                    page = filter.Page ?? 1,
                    pageSize = filter.PageSize ?? 20,
                    totalCount = totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)(filter.PageSize ?? 20))
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving locations with filter: {@Filter}", filter);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving locations" });
        }
    }

    /// <summary>
    /// Get a single location by ID
    /// </summary>
    /// <param name="id">Location ID (GUID)</param>
    /// <returns>Location details with device and employee counts</returns>
    /// <response code="200">Returns the location details</response>
    /// <response code="404">Location not found</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLocationById(Guid id)
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
    /// Get all unique districts from locations
    /// </summary>
    /// <returns>List of unique district names</returns>
    /// <response code="200">Returns list of districts</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error</response>
    /// <example>
    /// Returns: ["Port Louis", "Pamplemousses", "Rivière du Rempart", "Flacq", "Grand Port", "Savanne", "Plaines Wilhems", "Moka", "Black River"]
    /// </example>
    [HttpGet("districts")]
    [Authorize(Roles = "Admin,HR,Manager")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDistricts()
    {
        try
        {
            var districts = await _locationService.GetDistrictsAsync();
            return Ok(new { success = true, data = districts, count = districts.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving districts");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving districts" });
        }
    }

    /// <summary>
    /// Get all locations in a specific district
    /// </summary>
    /// <param name="district">District name (e.g., "Port Louis", "Plaines Wilhems")</param>
    /// <param name="activeOnly">Filter for active locations only (default: true)</param>
    /// <returns>List of locations in the specified district</returns>
    /// <response code="200">Returns locations in the district</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error</response>
    /// <example>
    /// GET /api/location/district/Port Louis
    /// GET /api/location/district/Plaines Wilhems?activeOnly=false
    /// </example>
    [HttpGet("district/{district}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLocationsByDistrict(string district, [FromQuery] bool activeOnly = true)
    {
        try
        {
            var locations = await _locationService.GetLocationsByDistrictAsync(district, activeOnly);
            return Ok(new { success = true, data = locations, count = locations.Count, district = district });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving locations for district {District}", district);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving locations" });
        }
    }

    /// <summary>
    /// Search locations by name, code, city, district, or region
    /// </summary>
    /// <param name="q">Search query (minimum 2 characters, case-insensitive)</param>
    /// <param name="activeOnly">Filter for active locations only (default: true)</param>
    /// <returns>List of matching locations (max 50 results)</returns>
    /// <response code="200">Returns matching locations</response>
    /// <response code="400">Invalid search query (too short)</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error</response>
    /// <example>
    /// GET /api/location/search?q=Port
    /// GET /api/location/search?q=Curepipe&amp;activeOnly=false
    /// </example>
    [HttpGet("search")]
    [Authorize(Roles = "Admin,HR,Manager")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchLocations([FromQuery] string q, [FromQuery] bool activeOnly = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return BadRequest(new { success = false, error = "Search query must be at least 2 characters" });
            }

            var locations = await _locationService.SearchLocationsAsync(q, activeOnly);
            return Ok(new { success = true, data = locations, count = locations.Count, query = q });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching locations with query {Query}", q);
            return StatusCode(500, new { success = false, error = "An error occurred while searching locations" });
        }
    }

    /// <summary>
    /// Create a new location
    /// </summary>
    /// <param name="dto">Location creation data</param>
    /// <returns>Created location ID and success message</returns>
    /// <response code="201">Location created successfully</response>
    /// <response code="400">Invalid input data or duplicate location code</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="403">Forbidden - Requires Admin or HR role</response>
    /// <response code="500">Internal server error</response>
    /// <example>
    /// POST /api/location
    /// {
    ///   "locationCode": "PL-004",
    ///   "locationName": "New Office - Port Louis",
    ///   "locationType": "Office",
    ///   "city": "Port Louis",
    ///   "district": "Port Louis",
    ///   "region": "North",
    ///   "postalCode": "11102",
    ///   "country": "Mauritius",
    ///   "latitude": -20.1609,
    ///   "longitude": 57.5012,
    ///   "isActive": true
    /// }
    /// </example>
    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, error = "Invalid input data", errors = ModelState });
        }

        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            var locationId = await _locationService.CreateLocationAsync(dto, userEmail);

            // Audit logging
            await _auditLogService.LogDataChangeAsync(
                actionType: Core.Enums.AuditActionType.RECORD_CREATED,
                entityType: "Location",
                entityId: locationId,
                oldValues: (object?)null,
                newValues: dto,
                reason: $"Created location: {dto.LocationName} ({dto.LocationCode})"
            );

            _logger.LogInformation(
                "Location created: {LocationId} - {LocationName} by {User}",
                locationId, dto.LocationName, userEmail);

            return CreatedAtAction(
                nameof(GetLocationById),
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
    /// <param name="id">Location ID (GUID)</param>
    /// <param name="dto">Updated location data</param>
    /// <returns>Success message</returns>
    /// <response code="200">Location updated successfully</response>
    /// <response code="400">Invalid input data or duplicate location code</response>
    /// <response code="404">Location not found</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="403">Forbidden - Requires Admin or HR role</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,HR")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, error = "Invalid input data", errors = ModelState });
        }

        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            await _locationService.UpdateLocationAsync(id, dto, userEmail);

            // Audit logging
            await _auditLogService.LogDataChangeAsync(
                actionType: Core.Enums.AuditActionType.RECORD_UPDATED,
                entityType: "Location",
                entityId: id,
                oldValues: (object?)null,
                newValues: dto,
                reason: $"Updated location: {dto.LocationName} ({dto.LocationCode})"
            );

            _logger.LogInformation(
                "Location updated: {LocationId} - {LocationName} by {User}",
                id, dto.LocationName, userEmail);

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
    /// Delete a location (soft delete)
    /// </summary>
    /// <param name="id">Location ID (GUID)</param>
    /// <returns>Success message</returns>
    /// <response code="200">Location deleted successfully</response>
    /// <response code="400">Cannot delete - location has devices or employees assigned</response>
    /// <response code="404">Location not found</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="403">Forbidden - Requires Admin role only</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// This is a soft delete operation. The location will be marked as deleted but not removed from the database.
    /// Cannot delete locations that have biometric devices or employees assigned.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteLocation(Guid id)
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            // Get location details before deletion for audit log
            var location = await _locationService.GetLocationByIdAsync(id);
            if (location == null)
            {
                return NotFound(new { success = false, error = "Location not found" });
            }

            await _locationService.DeleteLocationAsync(id, userEmail);

            // Audit logging
            await _auditLogService.LogDataChangeAsync(
                actionType: Core.Enums.AuditActionType.RECORD_DELETED,
                entityType: "Location",
                entityId: id,
                oldValues: location,
                newValues: (object?)null,
                reason: $"Deleted location: {location.LocationName} ({location.LocationCode})"
            );

            _logger.LogWarning(
                "Location deleted: {LocationId} - {LocationName} by {User}",
                id, location.LocationName, userEmail);

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
    /// Seed Mauritius locations (9 districts with major cities/towns/villages)
    /// </summary>
    /// <returns>Success message with count of seeded locations</returns>
    /// <response code="200">Locations seeded successfully (30 locations)</response>
    /// <response code="400">Cannot seed - locations already exist</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="403">Forbidden - Requires Admin role only</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// Seeds the database with 30 locations covering all 9 Mauritius districts:
    /// - Port Louis (3 locations)
    /// - Pamplemousses (3 locations)
    /// - Rivière du Rempart (3 locations)
    /// - Flacq (3 locations)
    /// - Grand Port (3 locations)
    /// - Savanne (3 locations)
    /// - Plaines Wilhems (5 locations)
    /// - Moka (3 locations)
    /// - Black River (3 locations)
    ///
    /// Each location includes:
    /// - Location code (district prefix + number)
    /// - Name, Type (City/Town/Village)
    /// - District, Region, Postal Code
    /// - Geographic coordinates (latitude/longitude)
    /// - Timezone (Indian/Mauritius)
    ///
    /// This operation can only be performed once. If locations already exist, it will fail.
    /// </remarks>
    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SeedMauritiusLocations()
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            await _locationService.SeedMauritiusLocationsAsync(userEmail);

            // Audit logging
            await _auditLogService.LogActionAsync(
                actionType: Core.Enums.AuditActionType.RECORD_CREATED,
                category: Core.Enums.AuditCategory.SYSTEM_EVENT,
                severity: Core.Enums.AuditSeverity.INFO,
                entityType: "Location",
                entityId: null,
                oldValues: null,
                newValues: "{\"count\": 30, \"districts\": 9}",
                success: true,
                errorMessage: null,
                reason: "Seeded 30 Mauritius locations (9 districts)"
            );

            _logger.LogInformation(
                "Mauritius locations seeded successfully by {User}",
                userEmail);

            return Ok(new
            {
                success = true,
                message = "Mauritius locations seeded successfully",
                count = 30,
                districts = new[]
                {
                    "Port Louis", "Pamplemousses", "Rivière du Rempart",
                    "Flacq", "Grand Port", "Savanne",
                    "Plaines Wilhems", "Moka", "Black River"
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to seed locations: {Message}", ex.Message);
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding Mauritius locations");
            return StatusCode(500, new { success = false, error = "An error occurred while seeding locations" });
        }
    }
}
