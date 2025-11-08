using HRMS.Application.DTOs.DepartmentDtos;
using HRMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Department Management API
/// Handles CRUD operations for organizational departments
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly DepartmentService _departmentService;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(
        DepartmentService departmentService,
        ILogger<DepartmentController> logger)
    {
        _departmentService = departmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all departments
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var departments = await _departmentService.GetAllAsync();
            return Ok(new { success = true, data = departments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving departments");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving departments" });
        }
    }

    /// <summary>
    /// Get a single department by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var department = await _departmentService.GetByIdAsync(id);

            if (department == null)
            {
                return NotFound(new { success = false, error = "Department not found" });
            }

            return Ok(new { success = true, data = department });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving the department" });
        }
    }

    /// <summary>
    /// Create a new department
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                error = "Invalid request data",
                errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
            });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

            var result = await _departmentService.CreateAsync(dto, userName);

            if (!result.Success)
            {
                return BadRequest(new { success = false, error = result.Message });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.DepartmentId },
                new { success = true, message = result.Message, departmentId = result.DepartmentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department");
            return StatusCode(500, new { success = false, error = "An error occurred while creating the department" });
        }
    }

    /// <summary>
    /// Update an existing department
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                error = "Invalid request data",
                errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
            });
        }

        try
        {
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

            var result = await _departmentService.UpdateAsync(id, dto, userName);

            if (!result.Success)
            {
                return BadRequest(new { success = false, error = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while updating the department" });
        }
    }

    /// <summary>
    /// Delete a department (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

            var result = await _departmentService.DeleteAsync(id, userName);

            if (!result.Success)
            {
                return BadRequest(new { success = false, error = result.Message });
            }

            return Ok(new { success = true, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while deleting the department" });
        }
    }

    /// <summary>
    /// Get department hierarchy tree structure
    /// </summary>
    [HttpGet("hierarchy")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetHierarchy()
    {
        try
        {
            var hierarchy = await _departmentService.GetHierarchyAsync();
            return Ok(new { success = true, data = hierarchy });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department hierarchy");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving the department hierarchy" });
        }
    }

    /// <summary>
    /// Get simplified department list for dropdowns
    /// </summary>
    [HttpGet("dropdown")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<IActionResult> GetDropdown()
    {
        try
        {
            var departments = await _departmentService.GetDropdownAsync();
            return Ok(new { success = true, data = departments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department dropdown list");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving the dropdown list" });
        }
    }
}
