using HRMS.Application.DTOs.PayrollDtos;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// Controller for managing employee salary components (allowances and deductions)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalaryComponentsController : ControllerBase
{
    private readonly ISalaryComponentService _salaryComponentService;
    private readonly ILogger<SalaryComponentsController> _logger;

    public SalaryComponentsController(
        ISalaryComponentService salaryComponentService,
        ILogger<SalaryComponentsController> logger)
    {
        _salaryComponentService = salaryComponentService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new salary component for an employee
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<Guid>> CreateComponent([FromBody] CreateSalaryComponentDto dto)
    {
        try
        {
            var username = User.Identity?.Name ?? "system";
            var componentId = await _salaryComponentService.CreateComponentAsync(dto, username);

            return CreatedAtAction(nameof(GetComponent), new { id = componentId }, componentId);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating salary component");
            return StatusCode(500, new { error = "An error occurred while creating the salary component" });
        }
    }

    /// <summary>
    /// Retrieves a specific salary component by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<SalaryComponentDto>> GetComponent(Guid id)
    {
        try
        {
            var component = await _salaryComponentService.GetComponentAsync(id);

            if (component == null)
                return NotFound(new { error = "Salary component not found" });

            return Ok(component);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary component {ComponentId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the salary component" });
        }
    }

    /// <summary>
    /// Retrieves all salary components for a specific employee
    /// </summary>
    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<List<SalaryComponentDto>>> GetEmployeeComponents(
        Guid employeeId,
        [FromQuery] bool activeOnly = true)
    {
        try
        {
            // TODO: Verify authorization - employees can only see their own components
            var components = await _salaryComponentService.GetEmployeeComponentsAsync(employeeId, activeOnly);
            return Ok(components);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving components for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while retrieving salary components" });
        }
    }

    /// <summary>
    /// Retrieves all salary components for all employees (Admin/HR only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<List<SalaryComponentDto>>> GetAllComponents([FromQuery] bool activeOnly = true)
    {
        try
        {
            var components = await _salaryComponentService.GetAllComponentsAsync(activeOnly);
            return Ok(components);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all salary components");
            return StatusCode(500, new { error = "An error occurred while retrieving salary components" });
        }
    }

    /// <summary>
    /// Updates an existing salary component
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> UpdateComponent(Guid id, [FromBody] UpdateSalaryComponentDto dto)
    {
        try
        {
            var username = User.Identity?.Name ?? "system";
            await _salaryComponentService.UpdateComponentAsync(id, dto, username);

            return Ok(new { message = "Salary component updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating salary component {ComponentId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the salary component" });
        }
    }

    /// <summary>
    /// Deactivates a salary component (soft delete)
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> DeactivateComponent(Guid id)
    {
        try
        {
            var username = User.Identity?.Name ?? "system";
            await _salaryComponentService.DeactivateComponentAsync(id, username);

            return Ok(new { message = "Salary component deactivated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating salary component {ComponentId}", id);
            return StatusCode(500, new { error = "An error occurred while deactivating the salary component" });
        }
    }

    /// <summary>
    /// Permanently deletes a salary component
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteComponent(Guid id)
    {
        try
        {
            await _salaryComponentService.DeleteComponentAsync(id);

            return Ok(new { message = "Salary component deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting salary component {ComponentId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the salary component" });
        }
    }

    /// <summary>
    /// Approves a salary component that requires approval
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> ApproveComponent(Guid id)
    {
        try
        {
            var username = User.Identity?.Name ?? "system";
            await _salaryComponentService.ApproveComponentAsync(id, username);

            return Ok(new { message = "Salary component approved successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving salary component {ComponentId}", id);
            return StatusCode(500, new { error = "An error occurred while approving the salary component" });
        }
    }

    /// <summary>
    /// Bulk creates salary components for multiple employees
    /// </summary>
    [HttpPost("bulk-create")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<List<Guid>>> BulkCreateComponents([FromBody] BulkCreateComponentsDto dto)
    {
        try
        {
            var username = User.Identity?.Name ?? "system";
            var componentIds = await _salaryComponentService.BulkCreateComponentsAsync(
                dto.EmployeeIds,
                dto.ComponentDetails,
                username);

            return Ok(new
            {
                message = $"{componentIds.Count} salary components created successfully",
                componentIds
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating salary components");
            return StatusCode(500, new { error = "An error occurred while creating salary components" });
        }
    }

    /// <summary>
    /// Calculates total allowances for an employee in a specific month
    /// </summary>
    [HttpGet("employee/{employeeId}/allowances")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<decimal>> GetTotalAllowances(
        Guid employeeId,
        [FromQuery] int month,
        [FromQuery] int year)
    {
        try
        {
            var total = await _salaryComponentService.GetTotalAllowancesAsync(employeeId, month, year);

            return Ok(new
            {
                employeeId,
                month,
                year,
                totalAllowances = total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total allowances for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while calculating total allowances" });
        }
    }

    /// <summary>
    /// Calculates total deductions for an employee in a specific month
    /// </summary>
    [HttpGet("employee/{employeeId}/deductions")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<decimal>> GetTotalDeductions(
        Guid employeeId,
        [FromQuery] int month,
        [FromQuery] int year)
    {
        try
        {
            var total = await _salaryComponentService.GetTotalDeductionsAsync(employeeId, month, year);

            return Ok(new
            {
                employeeId,
                month,
                year,
                totalDeductions = total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total deductions for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while calculating total deductions" });
        }
    }
}

/// <summary>
/// DTO for bulk creating salary components
/// </summary>
public class BulkCreateComponentsDto
{
    public List<Guid> EmployeeIds { get; set; } = new();
    public CreateSalaryComponentDto ComponentDetails { get; set; } = null!;
}
