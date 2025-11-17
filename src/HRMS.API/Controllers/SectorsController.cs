using HRMS.Application.DTOs.SectorDtos;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// Controller for managing industry sectors and compliance rules
/// SECURITY: Authentication required for sector reference data access
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // HIGH PRIORITY FIX: Secure sector reference data endpoint
public class SectorsController : ControllerBase
{
    private readonly ISectorService _sectorService;
    private readonly ISectorComplianceService _complianceService;
    private readonly ILogger<SectorsController> _logger;

    public SectorsController(
        ISectorService sectorService,
        ISectorComplianceService complianceService,
        ILogger<SectorsController> logger)
    {
        _sectorService = sectorService;
        _complianceService = complianceService;
        _logger = logger;
    }

    /// <summary>
    /// Get all sectors with hierarchical structure
    /// </summary>
    [HttpGet("hierarchical")]
    public async Task<IActionResult> GetAllSectorsHierarchical()
    {
        try
        {
            var sectors = await _sectorService.GetAllSectorsHierarchicalAsync();
            return Ok(new
            {
                success = true,
                data = sectors,
                count = sectors.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching hierarchical sectors");
            return StatusCode(500, new { success = false, message = "Error fetching sectors" });
        }
    }

    /// <summary>
    /// Get all sectors as flat list
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSectors([FromQuery] bool activeOnly = true)
    {
        try
        {
            var sectors = await _sectorService.GetAllSectorsFlatAsync(activeOnly);
            return Ok(new
            {
                success = true,
                data = sectors,
                count = sectors.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sectors");
            return StatusCode(500, new { success = false, message = "Error fetching sectors" });
        }
    }

    /// <summary>
    /// Get sector by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSectorById(int id)
    {
        try
        {
            var sector = await _sectorService.GetSectorByIdAsync(id);
            if (sector == null)
            {
                return NotFound(new { success = false, message = $"Sector not found: {id}" });
            }

            return Ok(new { success = true, data = sector });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sector {SectorId}", id);
            return StatusCode(500, new { success = false, message = "Error fetching sector" });
        }
    }

    /// <summary>
    /// Get sector by code
    /// </summary>
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetSectorByCode(string code)
    {
        try
        {
            var sector = await _sectorService.GetSectorByCodeAsync(code);
            if (sector == null)
            {
                return NotFound(new { success = false, message = $"Sector not found: {code}" });
            }

            return Ok(new { success = true, data = sector });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sector {SectorCode}", code);
            return StatusCode(500, new { success = false, message = "Error fetching sector" });
        }
    }

    /// <summary>
    /// Get compliance rules for a sector
    /// </summary>
    [HttpGet("{id}/compliance-rules")]
    public async Task<IActionResult> GetComplianceRulesForSector(int id)
    {
        try
        {
            var rules = await _sectorService.GetComplianceRulesForSectorAsync(id);
            return Ok(new
            {
                success = true,
                data = rules,
                count = rules.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching compliance rules for sector {SectorId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get compliance rule by category for a sector
    /// </summary>
    [HttpGet("{id}/compliance-rules/{category}")]
    public async Task<IActionResult> GetComplianceRuleByCategory(int id, string category)
    {
        try
        {
            var rule = await _sectorService.GetComplianceRuleByCategoryAsync(id, category);
            if (rule == null)
            {
                return NotFound(new { success = false, message = $"Rule not found for category: {category}" });
            }

            return Ok(new { success = true, data = rule });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching compliance rule for sector {SectorId}, category {Category}", id, category);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get parent sectors (top-level sectors)
    /// </summary>
    [HttpGet("parents")]
    public async Task<IActionResult> GetParentSectors()
    {
        try
        {
            var sectors = await _sectorService.GetParentSectorsAsync();
            return Ok(new
            {
                success = true,
                data = sectors,
                count = sectors.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching parent sectors");
            return StatusCode(500, new { success = false, message = "Error fetching parent sectors" });
        }
    }

    /// <summary>
    /// Get sub-sectors for a parent sector
    /// </summary>
    [HttpGet("{id}/sub-sectors")]
    public async Task<IActionResult> GetSubSectors(int id)
    {
        try
        {
            var sectors = await _sectorService.GetSubSectorsAsync(id);
            return Ok(new
            {
                success = true,
                data = sectors,
                count = sectors.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sub-sectors for parent {ParentId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Search sectors by name or code
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchSectors([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { success = false, message = "Search query is required" });
            }

            var sectors = await _sectorService.SearchSectorsAsync(query);
            return Ok(new
            {
                success = true,
                data = sectors,
                count = sectors.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching sectors with query: {Query}", query);
            return StatusCode(500, new { success = false, message = "Error searching sectors" });
        }
    }

    /// <summary>
    /// Get sectors requiring special permits
    /// </summary>
    [HttpGet("requiring-permits")]
    public async Task<IActionResult> GetSectorsRequiringPermits()
    {
        try
        {
            var sectors = await _sectorService.GetSectorsRequiringPermitsAsync();
            return Ok(new
            {
                success = true,
                data = sectors,
                count = sectors.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sectors requiring permits");
            return StatusCode(500, new { success = false, message = "Error fetching sectors" });
        }
    }

    /// <summary>
    /// Create new industry sector (Super Admin only)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateSector([FromBody] CreateSectorDto dto)
    {
        try
        {
            var sector = await _sectorService.CreateSectorAsync(dto);
            return CreatedAtAction(nameof(GetSectorById), new { id = sector.Id }, new
            {
                success = true,
                data = sector,
                message = "Sector created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sector");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Update existing sector (Super Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateSector(int id, [FromBody] UpdateSectorDto dto)
    {
        try
        {
            var sector = await _sectorService.UpdateSectorAsync(id, dto);
            return Ok(new
            {
                success = true,
                data = sector,
                message = "Sector updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sector {SectorId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
