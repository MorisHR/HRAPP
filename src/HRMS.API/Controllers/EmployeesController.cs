using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;

namespace HRMS.API.Controllers;

/// <summary>
/// Employee Management API
/// Handles both Local and Expatriate employees with full compliance tracking
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(
        IEmployeeService employeeService,
        ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new employee (Local or Expatriate)
    /// SECURITY: Requires HR or Admin role
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var employee = await _employeeService.CreateEmployeeAsync(request);

            _logger.LogInformation("Employee created: {EmployeeCode} ({Type})",
                employee.EmployeeCode, employee.EmployeeType);

            return CreatedAtAction(
                nameof(GetEmployeeById),
                new { id = employee.Id },
                new
                {
                    success = true,
                    message = "Employee created successfully",
                    data = employee
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating employee");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee");
            return StatusCode(500, new { success = false, message = "Error creating employee" });
        }
    }

    /// <summary>
    /// Get all employees (with optional filters)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<EmployeeListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllEmployees([FromQuery] bool includeInactive = false)
    {
        try
        {
            var employees = await _employeeService.GetAllEmployeesAsync(includeInactive);

            return Ok(new
            {
                success = true,
                data = employees,
                count = employees.Count,
                filters = new { includeInactive }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees");
            return StatusCode(500, new { success = false, message = "Error retrieving employees" });
        }
    }

    /// <summary>
    /// Get employee by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeById(Guid id)
    {
        try
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            return Ok(new { success = true, data = employee });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee {Id}", id);
            return StatusCode(500, new { success = false, message = "Error retrieving employee" });
        }
    }

    /// <summary>
    /// Get employee by employee code
    /// </summary>
    [HttpGet("code/{employeeCode}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeByCode(string employeeCode)
    {
        try
        {
            var employee = await _employeeService.GetEmployeeByCodeAsync(employeeCode);
            return Ok(new { success = true, data = employee });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee with code {Code}", employeeCode);
            return StatusCode(500, new { success = false, message = "Error retrieving employee" });
        }
    }

    /// <summary>
    /// Update employee information
    /// SECURITY: Requires HR or Admin role
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,HR")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request data",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var employee = await _employeeService.UpdateEmployeeAsync(id, request);

            _logger.LogInformation("Employee updated: {EmployeeCode}", employee.EmployeeCode);

            return Ok(new
            {
                success = true,
                message = "Employee updated successfully",
                data = employee
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating employee {Id}", id);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee {Id}", id);
            return StatusCode(500, new { success = false, message = "Error updating employee" });
        }
    }

    /// <summary>
    /// Delete employee (soft delete)
    /// SECURITY: Requires Admin role
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmployee(Guid id)
    {
        try
        {
            var result = await _employeeService.DeleteEmployeeAsync(id);

            if (!result)
                return NotFound(new { success = false, message = "Employee not found" });

            _logger.LogInformation("Employee soft deleted: {Id}", id);

            return Ok(new { success = true, message = "Employee deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee {Id}", id);
            return StatusCode(500, new { success = false, message = "Error deleting employee" });
        }
    }

    /// <summary>
    /// Get all expatriate employees
    /// </summary>
    [HttpGet("expatriates")]
    [ProducesResponseType(typeof(List<EmployeeListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpatriates()
    {
        try
        {
            var employees = await _employeeService.GetExpatriateEmployeesAsync();

            return Ok(new
            {
                success = true,
                data = employees,
                count = employees.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expatriate employees");
            return StatusCode(500, new { success = false, message = "Error retrieving expatriates" });
        }
    }

    /// <summary>
    /// Get employees grouped by country of origin
    /// </summary>
    [HttpGet("by-country")]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployeesByCountry()
    {
        try
        {
            var result = await _employeeService.GetEmployeesByCountryAsync();

            return Ok(new
            {
                success = true,
                data = result,
                totalCountries = result.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees by country");
            return StatusCode(500, new { success = false, message = "Error retrieving data" });
        }
    }

    /// <summary>
    /// Get documents expiring within specified days (default: 90 days)
    /// Used for expiry dashboard and alerts
    /// </summary>
    [HttpGet("expiring-documents")]
    [ProducesResponseType(typeof(List<DocumentExpiryInfoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiringDocuments([FromQuery] int daysAhead = 90)
    {
        try
        {
            var documents = await _employeeService.GetExpiringDocumentsAsync(daysAhead);

            var criticalCount = documents.Count(d => d.RequiresUrgentAction);

            return Ok(new
            {
                success = true,
                data = documents,
                totalCount = documents.Count,
                criticalCount,
                daysAhead
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring documents");
            return StatusCode(500, new { success = false, message = "Error retrieving expiring documents" });
        }
    }

    /// <summary>
    /// Get document expiry status for a specific employee
    /// </summary>
    [HttpGet("{id}/document-status")]
    [ProducesResponseType(typeof(DocumentExpiryInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentStatus(Guid id)
    {
        try
        {
            var status = await _employeeService.GetDocumentStatusAsync(id);

            return Ok(new { success = true, data = status });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document status for employee {Id}", id);
            return StatusCode(500, new { success = false, message = "Error retrieving document status" });
        }
    }

    /// <summary>
    /// Renew visa/work permit for an expatriate employee
    /// </summary>
    [HttpPost("{id}/renew-visa")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RenewVisa(
        Guid id,
        [FromBody] RenewVisaRequest request)
    {
        try
        {
            var employee = await _employeeService.RenewVisaAsync(id, request.NewExpiryDate, request.NewVisaNumber);

            _logger.LogInformation("Visa renewed for employee: {EmployeeCode}, new expiry: {ExpiryDate}",
                employee.EmployeeCode, request.NewExpiryDate);

            return Ok(new
            {
                success = true,
                message = "Visa renewed successfully",
                data = employee
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing visa for employee {Id}", id);
            return StatusCode(500, new { success = false, message = "Error renewing visa" });
        }
    }

    /// <summary>
    /// Search employees by name, email, or employee code
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<EmployeeListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchEmployees([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { success = false, message = "Search term is required" });
            }

            var employees = await _employeeService.SearchEmployeesAsync(q);

            return Ok(new
            {
                success = true,
                data = employees,
                count = employees.Count,
                searchTerm = q
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching employees with term: {SearchTerm}", q);
            return StatusCode(500, new { success = false, message = "Error searching employees" });
        }
    }

    /// <summary>
    /// Get employees by department
    /// </summary>
    [HttpGet("department/{departmentId}")]
    [ProducesResponseType(typeof(List<EmployeeListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployeesByDepartment(Guid departmentId)
    {
        try
        {
            var employees = await _employeeService.GetEmployeesByDepartmentAsync(departmentId);

            return Ok(new
            {
                success = true,
                data = employees,
                count = employees.Count,
                departmentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees for department {DepartmentId}", departmentId);
            return StatusCode(500, new { success = false, message = "Error retrieving employees" });
        }
    }
}

/// <summary>
/// Request DTO for renewing visa
/// </summary>
public class RenewVisaRequest
{
    public DateTime NewExpiryDate { get; set; }
    public string? NewVisaNumber { get; set; }
}
