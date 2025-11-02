using HRMS.Application.DTOs.PayrollDtos;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// Controller for payroll management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(
        IPayrollService payrollService,
        ILogger<PayrollController> logger)
    {
        _payrollService = payrollService;
        _logger = logger;
    }

    // ==================== PAYROLL CYCLE MANAGEMENT ====================

    /// <summary>
    /// Creates a new payroll cycle
    /// </summary>
    [HttpPost("cycles")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<Guid>> CreatePayrollCycle([FromBody] CreatePayrollCycleDto dto)
    {
        try
        {
            var username = User.Identity?.Name ?? "system";
            var cycleId = await _payrollService.CreatePayrollCycleAsync(dto, username);

            return CreatedAtAction(nameof(GetPayrollCycle), new { id = cycleId }, cycleId);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll cycle");
            return StatusCode(500, new { error = "An error occurred while creating the payroll cycle" });
        }
    }

    /// <summary>
    /// Retrieves a specific payroll cycle by ID
    /// </summary>
    [HttpGet("cycles/{id}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<PayrollCycleDto>> GetPayrollCycle(Guid id)
    {
        try
        {
            var cycle = await _payrollService.GetPayrollCycleAsync(id);

            if (cycle == null)
                return NotFound(new { error = "Payroll cycle not found" });

            return Ok(cycle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll cycle {CycleId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the payroll cycle" });
        }
    }

    /// <summary>
    /// Retrieves all payroll cycles, optionally filtered by year
    /// </summary>
    [HttpGet("cycles")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<List<PayrollCycleSummaryDto>>> GetPayrollCycles([FromQuery] int? year = null)
    {
        try
        {
            var cycles = await _payrollService.GetPayrollCyclesAsync(year);
            return Ok(cycles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll cycles");
            return StatusCode(500, new { error = "An error occurred while retrieving payroll cycles" });
        }
    }

    /// <summary>
    /// Processes a payroll cycle (calculates salaries and generates payslips)
    /// </summary>
    [HttpPost("cycles/{id}/process")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> ProcessPayroll(Guid id, [FromBody] ProcessPayrollDto dto)
    {
        try
        {
            var username = User.Identity?.Name ?? "system";
            await _payrollService.ProcessPayrollAsync(id, dto, username);

            return Ok(new { message = "Payroll processed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payroll cycle {CycleId}", id);
            return StatusCode(500, new { error = "An error occurred while processing payroll" });
        }
    }

    /// <summary>
    /// Approves or rejects a payroll cycle
    /// </summary>
    [HttpPost("cycles/{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApprovePayroll(Guid id, [FromBody] ApprovePayrollDto dto)
    {
        try
        {
            var username = User.Identity?.Name ?? "system";
            await _payrollService.ApprovePayrollAsync(id, dto, username);

            var message = dto.IsApproved ? "Payroll approved successfully" : "Payroll rejected";
            return Ok(new { message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving payroll cycle {CycleId}", id);
            return StatusCode(500, new { error = "An error occurred while approving payroll" });
        }
    }

    /// <summary>
    /// Cancels a payroll cycle
    /// </summary>
    [HttpPost("cycles/{id}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CancelPayroll(Guid id)
    {
        try
        {
            await _payrollService.CancelPayrollAsync(id);
            return Ok(new { message = "Payroll cancelled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payroll cycle {CycleId}", id);
            return StatusCode(500, new { error = "An error occurred while cancelling payroll" });
        }
    }

    /// <summary>
    /// Marks a payroll cycle as paid
    /// </summary>
    [HttpPost("cycles/{id}/mark-paid")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> MarkPayrollAsPaid(Guid id)
    {
        try
        {
            await _payrollService.MarkPayrollAsPaidAsync(id);
            return Ok(new { message = "Payroll marked as paid successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking payroll as paid {CycleId}", id);
            return StatusCode(500, new { error = "An error occurred while marking payroll as paid" });
        }
    }

    // ==================== PAYSLIP OPERATIONS ====================

    /// <summary>
    /// Retrieves detailed payslip information
    /// SECURITY: Employees can only view their own payslips
    /// </summary>
    [HttpGet("payslips/{id}")]
    public async Task<ActionResult<PayslipDetailsDto>> GetPayslip(Guid id)
    {
        try
        {
            var payslip = await _payrollService.GetPayslipAsync(id);

            if (payslip == null)
                return NotFound(new { error = "Payslip not found" });

            // SECURITY FIX: Check authorization - employees can only see their own payslips
            var isAdminOrHR = User.IsInRole("Admin") || User.IsInRole("HR") || User.IsInRole("Manager");
            if (!isAdminOrHR)
            {
                var currentEmployeeId = GetEmployeeIdFromToken();
                if (payslip.EmployeeId != currentEmployeeId)
                {
                    _logger.LogWarning(
                        "SECURITY: Employee {EmployeeId} attempted to access payslip {PayslipId} belonging to {OwnerId}",
                        currentEmployeeId, id, payslip.EmployeeId);

                    return Forbid(); // 403 Forbidden
                }
            }

            return Ok(payslip);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payslip {PayslipId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the payslip" });
        }
    }

    /// <summary>
    /// Retrieves all payslips for a specific payroll cycle
    /// </summary>
    [HttpGet("cycles/{cycleId}/payslips")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<List<PayslipDto>>> GetPayslipsForCycle(Guid cycleId)
    {
        try
        {
            var payslips = await _payrollService.GetPayslipsForCycleAsync(cycleId);
            return Ok(payslips);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payslips for cycle {CycleId}", cycleId);
            return StatusCode(500, new { error = "An error occurred while retrieving payslips" });
        }
    }

    /// <summary>
    /// Retrieves all payslips for a specific employee
    /// SECURITY: Employees can only view their own payslips
    /// </summary>
    [HttpGet("employees/{employeeId}/payslips")]
    public async Task<ActionResult<List<EmployeePayslipDto>>> GetEmployeePayslips(Guid employeeId, [FromQuery] int? year = null)
    {
        try
        {
            // SECURITY FIX: Verify authorization - employees can only see their own payslips
            var isAdminOrHR = User.IsInRole("Admin") || User.IsInRole("HR") || User.IsInRole("Manager");
            if (!isAdminOrHR)
            {
                var currentEmployeeId = GetEmployeeIdFromToken();
                if (employeeId != currentEmployeeId)
                {
                    _logger.LogWarning(
                        "SECURITY: Employee {EmployeeId} attempted to access payslips for employee {TargetEmployeeId}",
                        currentEmployeeId, employeeId);

                    return Forbid(); // 403 Forbidden
                }
            }

            var payslips = await _payrollService.GetEmployeePayslipsAsync(employeeId, year);
            return Ok(payslips);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payslips for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while retrieving employee payslips" });
        }
    }

    /// <summary>
    /// Regenerates a specific payslip (for corrections)
    /// </summary>
    [HttpPost("payslips/{id}/regenerate")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> RegeneratePayslip(Guid id)
    {
        try
        {
            var username = User.Identity?.Name ?? "system";
            await _payrollService.RegeneratePayslipAsync(id, username);

            return Ok(new { message = "Payslip regenerated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating payslip {PayslipId}", id);
            return StatusCode(500, new { error = "An error occurred while regenerating the payslip" });
        }
    }

    // ==================== REPORTS & EXPORTS ====================

    /// <summary>
    /// Generates comprehensive payroll summary report
    /// </summary>
    [HttpGet("cycles/{cycleId}/summary")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<PayrollSummaryDto>> GetPayrollSummary(Guid cycleId)
    {
        try
        {
            var summary = await _payrollService.GetPayrollSummaryAsync(cycleId);
            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payroll summary for cycle {CycleId}", cycleId);
            return StatusCode(500, new { error = "An error occurred while generating payroll summary" });
        }
    }

    /// <summary>
    /// Generates bank transfer file for bulk payment processing
    /// </summary>
    [HttpGet("cycles/{cycleId}/bank-transfer-file")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> GenerateBankTransferFile(Guid cycleId)
    {
        try
        {
            var fileContent = await _payrollService.GenerateBankTransferFileAsync(cycleId);
            var fileName = $"PayrollBankTransfer_{cycleId}_{DateTime.UtcNow:yyyyMMdd}.csv";

            return File(fileContent, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating bank transfer file for cycle {CycleId}", cycleId);
            return StatusCode(500, new { error = "An error occurred while generating the bank transfer file" });
        }
    }

    /// <summary>
    /// Generates PDF payslip for download
    /// SECURITY: Employees can only download their own payslips
    /// </summary>
    [HttpGet("payslips/{id}/pdf")]
    public async Task<IActionResult> DownloadPayslipPdf(Guid id)
    {
        try
        {
            // SECURITY FIX: First retrieve payslip to verify ownership
            var payslip = await _payrollService.GetPayslipAsync(id);

            if (payslip == null)
                return NotFound(new { error = "Payslip not found" });

            // Verify authorization - employees can only download their own payslips
            var isAdminOrHR = User.IsInRole("Admin") || User.IsInRole("HR") || User.IsInRole("Manager");
            if (!isAdminOrHR)
            {
                var currentEmployeeId = GetEmployeeIdFromToken();
                if (payslip.EmployeeId != currentEmployeeId)
                {
                    _logger.LogWarning(
                        "SECURITY: Employee {EmployeeId} attempted to download payslip {PayslipId} belonging to {OwnerId}",
                        currentEmployeeId, id, payslip.EmployeeId);

                    return Forbid(); // 403 Forbidden
                }
            }

            var pdfContent = await _payrollService.GeneratePayslipPdfAsync(id);
            var fileName = $"Payslip_{id}_{DateTime.UtcNow:yyyyMMdd}.pdf";

            return File(pdfContent, "application/pdf", fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, new { error = "PDF generation not yet implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for payslip {PayslipId}", id);
            return StatusCode(500, new { error = "An error occurred while generating the payslip PDF" });
        }
    }

    // ==================== CALCULATIONS (FOR TESTING/PREVIEW) ====================

    /// <summary>
    /// Calculates CSG employee contribution (for preview/testing)
    /// </summary>
    [HttpGet("calculations/csg-employee")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<decimal>> CalculateCSGEmployee([FromQuery] decimal monthlySalary)
    {
        try
        {
            var csg = await _payrollService.CalculateCSGEmployeeAsync(monthlySalary);
            return Ok(new { monthlySalary, csgEmployee = csg });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating CSG employee");
            return StatusCode(500, new { error = "An error occurred while calculating CSG" });
        }
    }

    /// <summary>
    /// Calculates PAYE tax (for preview/testing)
    /// </summary>
    [HttpGet("calculations/paye")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<decimal>> CalculatePAYE([FromQuery] decimal annualGross, [FromQuery] decimal annualDeductions)
    {
        try
        {
            var paye = await _payrollService.CalculatePAYEAsync(annualGross, annualDeductions);
            return Ok(new { annualGross, annualDeductions, monthlyPaye = paye });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating PAYE");
            return StatusCode(500, new { error = "An error occurred while calculating PAYE" });
        }
    }

    /// <summary>
    /// Calculates overtime pay for an employee (for preview/testing)
    /// </summary>
    [HttpGet("calculations/overtime/{employeeId}")]
    [Authorize(Roles = "Admin,HR,Manager")]
    public async Task<ActionResult<decimal>> CalculateOvertimePay(Guid employeeId, [FromQuery] int month, [FromQuery] int year)
    {
        try
        {
            var overtimePay = await _payrollService.CalculateOvertimePayAsync(employeeId, month, year);
            return Ok(new { employeeId, month, year, overtimePay });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating overtime pay for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while calculating overtime pay" });
        }
    }

    #region Private Helper Methods

    private Guid GetEmployeeIdFromToken()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            throw new UnauthorizedAccessException("Employee ID not found in token");
        }

        return employeeId;
    }

    #endregion
}
