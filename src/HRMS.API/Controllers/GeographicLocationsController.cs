using HRMS.Application.DTOs.GeographicLocationDtos;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// API Controller for Mauritius geographic locations (Districts, Villages, Postal Codes)
/// Provides reference data for address autocomplete, dropdowns, and validation
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Secure by default - geographic reference data requires authentication
public class GeographicLocationsController : ControllerBase
{
    private readonly IGeographicLocationService _service;
    private readonly ILogger<GeographicLocationsController> _logger;

    public GeographicLocationsController(
        IGeographicLocationService service,
        ILogger<GeographicLocationsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ============================================
    // DISTRICTS
    // ============================================

    /// <summary>
    /// Get all Mauritius districts (9 districts)
    /// </summary>
    /// <param name="activeOnly">Include only active districts (default: true)</param>
    /// <returns>List of districts</returns>
    [HttpGet("districts")]
    [ProducesResponseType(typeof(List<DistrictDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DistrictDto>>> GetAllDistricts([FromQuery] bool activeOnly = true)
    {
        try
        {
            var districts = await _service.GetAllDistrictsAsync(activeOnly);
            return Ok(districts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving districts");
            return StatusCode(500, "An error occurred while retrieving districts");
        }
    }

    /// <summary>
    /// Get district by ID
    /// </summary>
    /// <param name="id">District ID</param>
    [HttpGet("districts/{id}")]
    [ProducesResponseType(typeof(DistrictDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DistrictDto>> GetDistrictById(int id)
    {
        try
        {
            var district = await _service.GetDistrictByIdAsync(id);
            if (district == null)
            {
                return NotFound($"District with ID {id} not found");
            }

            return Ok(district);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving district {DistrictId}", id);
            return StatusCode(500, "An error occurred while retrieving the district");
        }
    }

    /// <summary>
    /// Get district by code (e.g., "PL" for Port Louis)
    /// </summary>
    /// <param name="code">District code</param>
    [HttpGet("districts/by-code/{code}")]
    [ProducesResponseType(typeof(DistrictDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DistrictDto>> GetDistrictByCode(string code)
    {
        try
        {
            var district = await _service.GetDistrictByCodeAsync(code);
            if (district == null)
            {
                return NotFound($"District with code '{code}' not found");
            }

            return Ok(district);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving district by code {Code}", code);
            return StatusCode(500, "An error occurred while retrieving the district");
        }
    }

    /// <summary>
    /// Get districts by region (North, South, East, West, Central)
    /// </summary>
    /// <param name="region">Region name</param>
    [HttpGet("districts/by-region/{region}")]
    [ProducesResponseType(typeof(List<DistrictDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DistrictDto>>> GetDistrictsByRegion(string region)
    {
        try
        {
            var districts = await _service.GetDistrictsByRegionAsync(region);
            return Ok(districts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving districts by region {Region}", region);
            return StatusCode(500, "An error occurred while retrieving districts");
        }
    }

    // ============================================
    // VILLAGES (Cities, Towns, Villages)
    // ============================================

    /// <summary>
    /// Get all villages/towns/cities across Mauritius
    /// </summary>
    /// <param name="activeOnly">Include only active locations (default: true)</param>
    [HttpGet("villages")]
    [ProducesResponseType(typeof(List<VillageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<VillageDto>>> GetAllVillages([FromQuery] bool activeOnly = true)
    {
        try
        {
            var villages = await _service.GetAllVillagesAsync(activeOnly);
            return Ok(villages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving villages");
            return StatusCode(500, "An error occurred while retrieving villages");
        }
    }

    /// <summary>
    /// Get village by ID
    /// </summary>
    /// <param name="id">Village ID</param>
    [HttpGet("villages/{id}")]
    [ProducesResponseType(typeof(VillageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VillageDto>> GetVillageById(int id)
    {
        try
        {
            var village = await _service.GetVillageByIdAsync(id);
            if (village == null)
            {
                return NotFound($"Village with ID {id} not found");
            }

            return Ok(village);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving village {VillageId}", id);
            return StatusCode(500, "An error occurred while retrieving the village");
        }
    }

    /// <summary>
    /// Get village by code (e.g., "PLOU" for Port Louis)
    /// </summary>
    /// <param name="code">Village code</param>
    [HttpGet("villages/by-code/{code}")]
    [ProducesResponseType(typeof(VillageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VillageDto>> GetVillageByCode(string code)
    {
        try
        {
            var village = await _service.GetVillageByCodeAsync(code);
            if (village == null)
            {
                return NotFound($"Village with code '{code}' not found");
            }

            return Ok(village);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving village by code {Code}", code);
            return StatusCode(500, "An error occurred while retrieving the village");
        }
    }

    /// <summary>
    /// Get all villages/towns/cities in a specific district
    /// </summary>
    /// <param name="districtId">District ID</param>
    /// <param name="activeOnly">Include only active locations (default: true)</param>
    [HttpGet("villages/by-district/{districtId}")]
    [ProducesResponseType(typeof(List<VillageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<VillageDto>>> GetVillagesByDistrict(int districtId, [FromQuery] bool activeOnly = true)
    {
        try
        {
            var villages = await _service.GetVillagesByDistrictIdAsync(districtId, activeOnly);
            return Ok(villages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving villages for district {DistrictId}", districtId);
            return StatusCode(500, "An error occurred while retrieving villages");
        }
    }

    /// <summary>
    /// Get villages by district code (e.g., "PL" for Port Louis)
    /// </summary>
    /// <param name="districtCode">District code</param>
    /// <param name="activeOnly">Include only active locations (default: true)</param>
    [HttpGet("villages/by-district-code/{districtCode}")]
    [ProducesResponseType(typeof(List<VillageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<VillageDto>>> GetVillagesByDistrictCode(string districtCode, [FromQuery] bool activeOnly = true)
    {
        try
        {
            var villages = await _service.GetVillagesByDistrictCodeAsync(districtCode, activeOnly);
            return Ok(villages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving villages for district code {DistrictCode}", districtCode);
            return StatusCode(500, "An error occurred while retrieving villages");
        }
    }

    /// <summary>
    /// Get villages by locality type (City, Town, Village)
    /// </summary>
    /// <param name="type">Locality type</param>
    /// <param name="activeOnly">Include only active locations (default: true)</param>
    [HttpGet("villages/by-type/{type}")]
    [ProducesResponseType(typeof(List<VillageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<VillageDto>>> GetVillagesByType(string type, [FromQuery] bool activeOnly = true)
    {
        try
        {
            var villages = await _service.GetVillagesByLocalityTypeAsync(type, activeOnly);
            return Ok(villages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving villages by type {Type}", type);
            return StatusCode(500, "An error occurred while retrieving villages");
        }
    }

    /// <summary>
    /// Search villages by name (fuzzy search for autocomplete)
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="maxResults">Maximum number of results (default: 20)</param>
    [HttpGet("villages/search")]
    [ProducesResponseType(typeof(List<VillageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<VillageDto>>> SearchVillages(
        [FromQuery] string searchTerm,
        [FromQuery] int maxResults = 20)
    {
        try
        {
            var villages = await _service.SearchVillagesAsync(searchTerm, maxResults);
            return Ok(villages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching villages with term {SearchTerm}", searchTerm);
            return StatusCode(500, "An error occurred while searching villages");
        }
    }

    // ============================================
    // POSTAL CODES
    // ============================================

    /// <summary>
    /// Get all postal codes across Mauritius
    /// </summary>
    /// <param name="activeOnly">Include only active postal codes (default: true)</param>
    [HttpGet("postal-codes")]
    [ProducesResponseType(typeof(List<PostalCodeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PostalCodeDto>>> GetAllPostalCodes([FromQuery] bool activeOnly = true)
    {
        try
        {
            var postalCodes = await _service.GetAllPostalCodesAsync(activeOnly);
            return Ok(postalCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving postal codes");
            return StatusCode(500, "An error occurred while retrieving postal codes");
        }
    }

    /// <summary>
    /// Get postal code by code (e.g., "11302" for Port Louis)
    /// </summary>
    /// <param name="code">Postal code</param>
    [HttpGet("postal-codes/{code}")]
    [ProducesResponseType(typeof(PostalCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostalCodeDto>> GetPostalCodeByCode(string code)
    {
        try
        {
            var postalCode = await _service.GetPostalCodeByCodeAsync(code);
            if (postalCode == null)
            {
                return NotFound($"Postal code '{code}' not found");
            }

            return Ok(postalCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving postal code {Code}", code);
            return StatusCode(500, "An error occurred while retrieving the postal code");
        }
    }

    /// <summary>
    /// Search postal codes by partial code or village name
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="maxResults">Maximum number of results (default: 20)</param>
    [HttpGet("postal-codes/search")]
    [ProducesResponseType(typeof(List<PostalCodeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PostalCodeDto>>> SearchPostalCodes(
        [FromQuery] string searchTerm,
        [FromQuery] int maxResults = 20)
    {
        try
        {
            var postalCodes = await _service.SearchPostalCodesAsync(searchTerm, maxResults);
            return Ok(postalCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching postal codes with term {SearchTerm}", searchTerm);
            return StatusCode(500, "An error occurred while searching postal codes");
        }
    }

    // ============================================
    // ADDRESS AUTOCOMPLETE & VALIDATION
    // ============================================

    /// <summary>
    /// Get complete address hierarchy for cascading dropdowns
    /// Returns districts with their villages grouped
    /// </summary>
    [HttpGet("address-hierarchy")]
    [ProducesResponseType(typeof(List<DistrictWithVillagesDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DistrictWithVillagesDto>>> GetAddressHierarchy()
    {
        try
        {
            var hierarchy = await _service.GetAddressHierarchyAsync();
            return Ok(hierarchy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving address hierarchy");
            return StatusCode(500, "An error occurred while retrieving address hierarchy");
        }
    }

    /// <summary>
    /// Validate a Mauritius address (district, village, postal code)
    /// </summary>
    /// <param name="districtCode">District code (optional)</param>
    /// <param name="villageCode">Village code (optional)</param>
    /// <param name="postalCode">Postal code (optional)</param>
    [HttpGet("validate-address")]
    [ProducesResponseType(typeof(AddressValidationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AddressValidationResult>> ValidateAddress(
        [FromQuery] string? districtCode,
        [FromQuery] string? villageCode,
        [FromQuery] string? postalCode)
    {
        try
        {
            var result = await _service.ValidateAddressAsync(districtCode, villageCode, postalCode);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating address");
            return StatusCode(500, "An error occurred while validating the address");
        }
    }

    /// <summary>
    /// Get address suggestions for autocomplete
    /// Combines district, village, and postal code for comprehensive search
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="maxResults">Maximum number of results (default: 10)</param>
    [HttpGet("address-suggestions")]
    [ProducesResponseType(typeof(List<AddressSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AddressSuggestionDto>>> GetAddressSuggestions(
        [FromQuery] string searchTerm,
        [FromQuery] int maxResults = 10)
    {
        try
        {
            var suggestions = await _service.GetAddressSuggestionsAsync(searchTerm, maxResults);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting address suggestions for term {SearchTerm}", searchTerm);
            return StatusCode(500, "An error occurred while getting address suggestions");
        }
    }

    // ============================================
    // STATISTICS & ANALYTICS
    // ============================================

    /// <summary>
    /// Get location statistics (total districts, villages, postal codes)
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(LocationStatisticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LocationStatisticsDto>> GetStatistics()
    {
        try
        {
            var stats = await _service.GetLocationStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location statistics");
            return StatusCode(500, "An error occurred while retrieving statistics");
        }
    }
}
