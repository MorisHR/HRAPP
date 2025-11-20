using HRMS.Application.DTOs.DepartmentDtos;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Fortune 500-Grade Department Management API
///
/// Features:
/// - Comprehensive CRUD operations with validation
/// - Advanced search with filtering and pagination
/// - Bulk operations (activate, deactivate, delete)
/// - Department merge for reorganizations
/// - Employee reassignment
/// - Hierarchical view with role-based filtering
/// - Rate limiting for DoS protection
/// - Full audit trail
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(
        IDepartmentService departmentService,
        ILogger<DepartmentController> logger)
    {
        _departmentService = departmentService;
        _logger = logger;
    }

    #region Read Operations

    /// <summary>
    /// Get all departments with basic information
    /// </summary>
    /// <returns>List of all departments</returns>
    /// <response code="200">Departments retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [Authorize(Roles = "Admin,HR,Manager")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
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
    /// Search departments with advanced filtering, sorting, and pagination
    /// </summary>
    /// <param name="searchDto">Search criteria including filters, pagination, and sorting</param>
    /// <returns>Paginated and filtered department list</returns>
    /// <response code="200">Search completed successfully</response>
    /// <response code="400">Invalid search criteria</response>
    [HttpPost("search")]
    [Authorize(Roles = "Admin,HR,Manager")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromBody] DepartmentSearchDto searchDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                error = "Invalid search criteria",
                errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
            });
        }

        try
        {
            var result = await _departmentService.SearchAsync(searchDto);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching departments with criteria: {Criteria}", searchDto);
            return StatusCode(500, new { success = false, error = "An error occurred while searching departments" });
        }
    }

    /// <summary>
    /// Get a single department by ID
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <returns>Department details</returns>
    /// <response code="200">Department found</response>
    /// <response code="404">Department not found</response>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
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
    /// Get detailed department information including audit trail and hierarchy
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <returns>Detailed department information</returns>
    /// <response code="200">Department details retrieved successfully</response>
    /// <response code="404">Department not found</response>
    [HttpGet("{id:guid}/detail")]
    [Authorize(Roles = "Admin,HR,Manager")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        try
        {
            var detail = await _departmentService.GetDetailAsync(id);

            if (detail == null)
            {
                return NotFound(new { success = false, error = "Department not found" });
            }

            return Ok(new { success = true, data = detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department detail {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving department details" });
        }
    }

    /// <summary>
    /// Get department hierarchy tree structure with role-based filtering
    /// </summary>
    /// <returns>Hierarchical department tree</returns>
    /// <response code="200">Hierarchy retrieved successfully</response>
    [HttpGet("hierarchy")]
    [Authorize(Roles = "Admin,HR,Manager")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHierarchy()
    {
        try
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            Guid? userId = null;

            if (Guid.TryParse(userIdStr, out var parsedId))
            {
                userId = parsedId;
            }

            var hierarchy = await _departmentService.GetHierarchyAsync(userId, userRole);
            return Ok(new { success = true, data = hierarchy });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department hierarchy");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving the department hierarchy" });
        }
    }

    /// <summary>
    /// Get simplified department list for dropdowns (cached)
    /// </summary>
    /// <param name="activeOnly">Include only active departments (default: true)</param>
    /// <returns>Simplified department list</returns>
    /// <response code="200">Dropdown list retrieved successfully</response>
    [HttpGet("dropdown")]
    [Authorize(Roles = "Admin,HR,Manager,Employee")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDropdown([FromQuery] bool activeOnly = true)
    {
        try
        {
            var departments = await _departmentService.GetDropdownAsync(activeOnly);
            return Ok(new { success = true, data = departments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department dropdown list");
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving the dropdown list" });
        }
    }

    #endregion

    #region Create/Update Operations

    /// <summary>
    /// Create a new department with comprehensive validation
    /// </summary>
    /// <param name="dto">Department creation data</param>
    /// <returns>Created department ID and success message</returns>
    /// <response code="201">Department created successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="409">Department code already exists</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
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
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

            var result = await _departmentService.CreateAsync(dto, userName);

            if (!result.Success)
            {
                // Determine appropriate status code based on error message
                if (result.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new { success = false, error = result.Message });
                }
                else if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return UnprocessableEntity(new { success = false, error = result.Message });
                }
                else
                {
                    return BadRequest(new { success = false, error = result.Message });
                }
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
    /// <param name="id">Department ID</param>
    /// <param name="dto">Updated department data</param>
    /// <returns>Success message</returns>
    /// <response code="200">Department updated successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="404">Department not found</response>
    /// <response code="409">Department code conflict</response>
    /// <response code="422">Business rule violation (e.g., circular reference)</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,HR")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
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
                // Determine appropriate status code
                if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { success = false, error = result.Message });
                }
                else if (result.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new { success = false, error = result.Message });
                }
                else if (result.Message.Contains("circular", StringComparison.OrdinalIgnoreCase) ||
                         result.Message.Contains("cannot be", StringComparison.OrdinalIgnoreCase))
                {
                    return UnprocessableEntity(new { success = false, error = result.Message });
                }
                else
                {
                    return BadRequest(new { success = false, error = result.Message });
                }
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
    /// Bulk update department status (activate or deactivate multiple departments)
    /// </summary>
    /// <param name="dto">Bulk status update request</param>
    /// <returns>Number of departments affected</returns>
    /// <response code="200">Bulk update completed successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("bulk-status")]
    [Authorize(Roles = "Admin,HR")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkDepartmentStatusDto dto)
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

            var result = await _departmentService.BulkUpdateStatusAsync(dto, userName);

            if (!result.Success)
            {
                return BadRequest(new { success = false, error = result.Message });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                affectedCount = result.AffectedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk status update");
            return StatusCode(500, new { success = false, error = "An error occurred during bulk status update" });
        }
    }

    #endregion

    #region Delete Operations

    /// <summary>
    /// Delete a department (soft delete)
    /// Requires department to have no active employees or sub-departments
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <returns>Success message</returns>
    /// <response code="200">Department deleted successfully</response>
    /// <response code="404">Department not found</response>
    /// <response code="422">Cannot delete department (has active employees or sub-departments)</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

            var result = await _departmentService.DeleteAsync(id, userName);

            if (!result.Success)
            {
                if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { success = false, error = result.Message });
                }
                else
                {
                    return UnprocessableEntity(new { success = false, error = result.Message });
                }
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
    /// Bulk delete multiple departments
    /// </summary>
    /// <param name="departmentIds">List of department IDs to delete</param>
    /// <returns>Delete results with count and any errors</returns>
    /// <response code="200">Bulk delete completed (check results for individual failures)</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("bulk-delete")]
    [Authorize(Roles = "Admin")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDelete([FromBody] List<Guid> departmentIds)
    {
        if (departmentIds == null || !departmentIds.Any())
        {
            return BadRequest(new { success = false, error = "At least one department ID is required" });
        }

        try
        {
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

            var result = await _departmentService.BulkDeleteAsync(departmentIds, userName);

            return Ok(new
            {
                success = result.Success,
                message = result.Message,
                deletedCount = result.DeletedCount,
                errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk delete");
            return StatusCode(500, new { success = false, error = "An error occurred during bulk delete operation" });
        }
    }

    #endregion

    #region Advanced Operations

    /// <summary>
    /// Merge two departments - moves all employees and sub-departments from source to target
    /// Enterprise feature for organizational restructuring
    /// </summary>
    /// <param name="dto">Merge configuration</param>
    /// <returns>Merge results including counts of moved employees and sub-departments</returns>
    /// <response code="200">Merge completed successfully</response>
    /// <response code="400">Invalid merge request</response>
    /// <response code="422">Business rule violation</response>
    [HttpPost("merge")]
    [Authorize(Roles = "Admin")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MergeDepartments([FromBody] DepartmentMergeDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                error = "Invalid merge request",
                errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
            });
        }

        try
        {
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

            var result = await _departmentService.MergeDepartmentsAsync(dto, userName);

            if (!result.Success)
            {
                if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return UnprocessableEntity(new { success = false, error = result.Message });
                }
                else
                {
                    return BadRequest(new { success = false, error = result.Message });
                }
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                result = result.Result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging departments {Source} -> {Target}",
                dto.SourceDepartmentId, dto.TargetDepartmentId);
            return StatusCode(500, new { success = false, error = "An error occurred during department merge" });
        }
    }

    /// <summary>
    /// Reassign all active employees from one department to another
    /// Helper for department cleanup before deletion
    /// </summary>
    /// <param name="fromDepartmentId">Source department ID</param>
    /// <param name="toDepartmentId">Target department ID</param>
    /// <returns>Number of employees reassigned</returns>
    /// <response code="200">Employees reassigned successfully</response>
    /// <response code="400">Invalid department IDs</response>
    /// <response code="404">Department not found</response>
    [HttpPost("reassign-employees")]
    [Authorize(Roles = "Admin,HR")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReassignEmployees(
        [FromQuery] Guid fromDepartmentId,
        [FromQuery] Guid toDepartmentId)
    {
        if (fromDepartmentId == Guid.Empty || toDepartmentId == Guid.Empty)
        {
            return BadRequest(new { success = false, error = "Valid department IDs are required" });
        }

        if (fromDepartmentId == toDepartmentId)
        {
            return BadRequest(new { success = false, error = "Source and target departments must be different" });
        }

        try
        {
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "System";

            var result = await _departmentService.ReassignEmployeesAsync(fromDepartmentId, toDepartmentId, userName);

            if (!result.Success)
            {
                if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { success = false, error = result.Message });
                }
                else
                {
                    return BadRequest(new { success = false, error = result.Message });
                }
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                reassignedCount = result.ReassignedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reassigning employees from {From} to {To}",
                fromDepartmentId, toDepartmentId);
            return StatusCode(500, new { success = false, error = "An error occurred during employee reassignment" });
        }
    }

    /// <summary>
    /// Get department activity history for audit trail
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <returns>List of activity log entries</returns>
    /// <response code="200">Activity history retrieved successfully</response>
    /// <response code="404">Department not found</response>
    [HttpGet("{id:guid}/activity")]
    [Authorize(Roles = "Admin,HR")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActivityHistory(Guid id)
    {
        try
        {
            var activities = await _departmentService.GetActivityHistoryAsync(id);
            return Ok(new { success = true, data = activities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activity history for department {Id}", id);
            return StatusCode(500, new { success = false, error = "An error occurred while retrieving activity history" });
        }
    }

    #endregion

    #region Validation Helpers

    /// <summary>
    /// Check if a department code is available
    /// </summary>
    /// <param name="code">Department code to check</param>
    /// <param name="excludeDepartmentId">Optional department ID to exclude (for updates)</param>
    /// <returns>Boolean indicating availability</returns>
    /// <response code="200">Check completed successfully</response>
    [HttpGet("check-code")]
    [Authorize(Roles = "Admin,HR")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckCodeAvailable([FromQuery] string code, [FromQuery] Guid? excludeDepartmentId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new { success = false, error = "Code is required" });
        }

        try
        {
            var isAvailable = await _departmentService.IsCodeAvailableAsync(code, excludeDepartmentId);
            return Ok(new { success = true, available = isAvailable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking code availability for {Code}", code);
            return StatusCode(500, new { success = false, error = "An error occurred while checking code availability" });
        }
    }

    /// <summary>
    /// Check if an employee is already a department head
    /// </summary>
    /// <param name="employeeId">Employee ID to check</param>
    /// <returns>Department head status and department name if applicable</returns>
    /// <response code="200">Check completed successfully</response>
    [HttpGet("check-head/{employeeId:guid}")]
    [Authorize(Roles = "Admin,HR")]
    
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckEmployeeDepartmentHead(Guid employeeId)
    {
        try
        {
            var (isHead, departmentName) = await _departmentService.IsEmployeeDepartmentHeadAsync(employeeId);
            return Ok(new
            {
                success = true,
                isHead,
                departmentName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking department head status for employee {Id}", employeeId);
            return StatusCode(500, new { success = false, error = "An error occurred while checking department head status" });
        }
    }

    #endregion
}
