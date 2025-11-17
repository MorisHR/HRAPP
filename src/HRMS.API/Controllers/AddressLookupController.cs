using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMS.Infrastructure.Data;
using HRMS.Application.DTOs.AddressDtos;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/address-lookup")]
[Authorize]
public class AddressLookupController : ControllerBase
{
    private readonly MasterDbContext _masterDbContext;
    private readonly ILogger<AddressLookupController> _logger;

    public AddressLookupController(MasterDbContext masterDbContext, ILogger<AddressLookupController> logger)
    {
        _masterDbContext = masterDbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all districts for dropdown population
    /// </summary>
    [HttpGet("districts")]
    [ProducesResponseType(typeof(IEnumerable<DistrictDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DistrictDto>>> GetDistricts()
    {
        try
        {
            var districts = await _masterDbContext.Districts
                .Where(d => d.IsActive && !d.IsDeleted)
                .OrderBy(d => d.DistrictName)
                .Select(d => new DistrictDto
                {
                    Id = d.Id,
                    DistrictCode = d.DistrictCode,
                    DistrictName = d.DistrictName,
                    DistrictNameFrench = d.DistrictNameFrench,
                    Region = d.Region,
                    AreaSqKm = d.AreaSqKm,
                    Population = d.Population
                })
                .ToListAsync();

            return Ok(districts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving districts");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving districts");
        }
    }

    /// <summary>
    /// Get villages by district ID for cascading dropdown
    /// </summary>
    [HttpGet("districts/{districtId}/villages")]
    [ProducesResponseType(typeof(IEnumerable<VillageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<VillageDto>>> GetVillagesByDistrict(int districtId)
    {
        try
        {
            var districtExists = await _masterDbContext.Districts
                .AnyAsync(d => d.Id == districtId && d.IsActive && !d.IsDeleted);

            if (!districtExists)
            {
                return NotFound($"District with ID {districtId} not found");
            }

            var villages = await _masterDbContext.Villages
                .Where(v => v.DistrictId == districtId && v.IsActive && !v.IsDeleted)
                .OrderBy(v => v.VillageName)
                .Select(v => new VillageDto
                {
                    Id = v.Id,
                    VillageCode = v.VillageCode,
                    VillageName = v.VillageName,
                    PostalCode = v.PostalCode,
                    DistrictId = v.DistrictId
                })
                .ToListAsync();

            return Ok(villages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving villages for district {DistrictId}", districtId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving villages");
        }
    }

    /// <summary>
    /// Get all villages for dropdown population
    /// </summary>
    [HttpGet("villages")]
    [ProducesResponseType(typeof(IEnumerable<VillageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<VillageDto>>> GetVillages()
    {
        try
        {
            var villages = await _masterDbContext.Villages
                .Where(v => v.IsActive && !v.IsDeleted)
                .OrderBy(v => v.VillageName)
                .Select(v => new VillageDto
                {
                    Id = v.Id,
                    VillageCode = v.VillageCode,
                    VillageName = v.VillageName,
                    PostalCode = v.PostalCode,
                    DistrictId = v.DistrictId
                })
                .ToListAsync();

            return Ok(villages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving villages");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving villages");
        }
    }

    /// <summary>
    /// Search postal codes by code for autocomplete
    /// </summary>
    [HttpGet("postal-codes/search")]
    [ProducesResponseType(typeof(IEnumerable<PostalCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PostalCodeDto>>> SearchPostalCodes([FromQuery] string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest("Postal code search parameter is required");
            }

            var postalCodes = await _masterDbContext.PostalCodes
                .Where(p => p.Code.StartsWith(code) && p.IsActive && !p.IsDeleted)
                .OrderBy(p => p.Code)
                .Select(p => new PostalCodeDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    VillageName = p.VillageName,
                    DistrictName = p.DistrictName,
                    Region = p.Region
                })
                .Take(20) // Limit results for autocomplete
                .ToListAsync();

            return Ok(postalCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching postal codes with code {Code}", code);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching postal codes");
        }
    }

    /// <summary>
    /// Get all postal codes for dropdown population
    /// </summary>
    [HttpGet("postal-codes")]
    [ProducesResponseType(typeof(IEnumerable<PostalCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PostalCodeDto>>> GetPostalCodes()
    {
        try
        {
            var postalCodes = await _masterDbContext.PostalCodes
                .Where(p => p.IsActive && !p.IsDeleted)
                .OrderBy(p => p.Code)
                .Select(p => new PostalCodeDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    VillageName = p.VillageName,
                    DistrictName = p.DistrictName,
                    Region = p.Region
                })
                .ToListAsync();

            return Ok(postalCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving postal codes");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving postal codes");
        }
    }
}
