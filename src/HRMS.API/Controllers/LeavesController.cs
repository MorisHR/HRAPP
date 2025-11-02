using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Leave Management API
/// Handles leave applications, approvals, balances, and calendar
/// Mauritius Labour Law compliant
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly ILogger<LeavesController> _logger;

    public LeavesController(
        ILeaveService leaveService,
        ILogger<LeavesController> logger)
    {
        _leaveService = leaveService;
        _logger = logger;
    }

    /// <summary>
    /// Apply for leave
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LeaveApplicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyForLeave([FromBody] CreateLeaveApplicationRequest request)
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

            var employeeId = GetEmployeeIdFromToken();
            var leaveApplication = await _leaveService.ApplyForLeaveAsync(employeeId, request);

            _logger.LogInformation("Leave application {ApplicationNumber} created by employee {EmployeeId}",
                leaveApplication.ApplicationNumber, employeeId);

            return CreatedAtAction(
                nameof(GetLeaveById),
                new { id = leaveApplication.Id },
                new
                {
                    success = true,
                    message = "Leave application submitted successfully",
                    data = leaveApplication
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error applying for leave");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying for leave");
            return StatusCode(500, new { success = false, message = "Error submitting leave application" });
        }
    }

    /// <summary>
    /// Get my leave applications
    /// </summary>
    [HttpGet("my-leaves")]
    [ProducesResponseType(typeof(List<LeaveApplicationListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyLeaves([FromQuery] int? year = null)
    {
        try
        {
            var employeeId = GetEmployeeIdFromToken();
            var leaves = await _leaveService.GetMyLeavesAsync(employeeId, year);

            return Ok(new
            {
                success = true,
                data = leaves,
                count = leaves.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving my leaves");
            return StatusCode(500, new { success = false, message = "Error retrieving leave applications" });
        }
    }

    /// <summary>
    /// Get leave application by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LeaveApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaveById(Guid id)
    {
        try
        {
            var leave = await _leaveService.GetLeaveApplicationByIdAsync(id);
            return Ok(new { success = true, data = leave });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave {LeaveId}", id);
            return StatusCode(500, new { success = false, message = "Error retrieving leave application" });
        }
    }

    /// <summary>
    /// Cancel a leave application
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(LeaveApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelLeave(Guid id, [FromBody] CancelLeaveRequest request)
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

            var employeeId = GetEmployeeIdFromToken();
            var leave = await _leaveService.CancelLeaveAsync(id, employeeId, request);

            _logger.LogInformation("Leave {LeaveId} cancelled by employee {EmployeeId}", id, employeeId);

            return Ok(new
            {
                success = true,
                message = "Leave cancelled successfully",
                data = leave
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling leave {LeaveId}", id);
            return StatusCode(500, new { success = false, message = "Error cancelling leave" });
        }
    }

    /// <summary>
    /// Approve a leave application (Manager/HR/Admin only)
    /// SECURITY: Requires Manager, HR or Admin role
    /// </summary>
    [HttpPost("{id}/approve")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,HR,Manager")]
    [ProducesResponseType(typeof(LeaveApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveLeave(Guid id, [FromBody] ApproveLeaveRequest request)
    {
        try
        {
            var approverId = GetEmployeeIdFromToken();
            var leave = await _leaveService.ApproveLeaveAsync(id, approverId, request);

            _logger.LogInformation("Leave {LeaveId} approved by {ApproverId}", id, approverId);

            return Ok(new
            {
                success = true,
                message = "Leave approved successfully",
                data = leave
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving leave {LeaveId}", id);
            return StatusCode(500, new { success = false, message = "Error approving leave" });
        }
    }

    /// <summary>
    /// Reject a leave application (Manager/HR/Admin only)
    /// SECURITY: Requires Manager, HR or Admin role
    /// </summary>
    [HttpPost("{id}/reject")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,HR,Manager")]
    [ProducesResponseType(typeof(LeaveApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectLeave(Guid id, [FromBody] RejectLeaveRequest request)
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

            var approverId = GetEmployeeIdFromToken();
            var leave = await _leaveService.RejectLeaveAsync(id, approverId, request);

            _logger.LogInformation("Leave {LeaveId} rejected by {ApproverId}", id, approverId);

            return Ok(new
            {
                success = true,
                message = "Leave rejected successfully",
                data = leave
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting leave {LeaveId}", id);
            return StatusCode(500, new { success = false, message = "Error rejecting leave" });
        }
    }

    /// <summary>
    /// Get pending approvals (for managers)
    /// </summary>
    [HttpGet("pending-approvals")]
    [ProducesResponseType(typeof(List<LeaveApplicationListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingApprovals()
    {
        try
        {
            var managerId = GetEmployeeIdFromToken();
            var pendingLeaves = await _leaveService.GetPendingApprovalsAsync(managerId);

            return Ok(new
            {
                success = true,
                data = pendingLeaves,
                count = pendingLeaves.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending approvals");
            return StatusCode(500, new { success = false, message = "Error retrieving pending approvals" });
        }
    }

    /// <summary>
    /// Get team leaves (for managers)
    /// </summary>
    [HttpGet("team")]
    [ProducesResponseType(typeof(List<LeaveApplicationListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamLeaves(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var managerId = GetEmployeeIdFromToken();
            var teamLeaves = await _leaveService.GetTeamLeavesAsync(managerId, startDate, endDate);

            return Ok(new
            {
                success = true,
                data = teamLeaves,
                count = teamLeaves.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving team leaves");
            return StatusCode(500, new { success = false, message = "Error retrieving team leaves" });
        }
    }

    /// <summary>
    /// Get my leave balance
    /// </summary>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(List<LeaveBalanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaveBalance([FromQuery] int? year = null)
    {
        try
        {
            var employeeId = GetEmployeeIdFromToken();
            var balances = await _leaveService.GetLeaveBalanceAsync(employeeId, year);

            return Ok(new
            {
                success = true,
                data = balances,
                year = year ?? DateTime.UtcNow.Year
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave balance");
            return StatusCode(500, new { success = false, message = "Error retrieving leave balance" });
        }
    }

    /// <summary>
    /// Get leave calendar
    /// </summary>
    [HttpGet("calendar")]
    [ProducesResponseType(typeof(List<LeaveCalendarDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaveCalendar(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] Guid? departmentId = null)
    {
        try
        {
            var calendar = await _leaveService.GetLeaveCalendarAsync(startDate, endDate, departmentId);

            return Ok(new
            {
                success = true,
                data = calendar,
                count = calendar.Count,
                startDate,
                endDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave calendar");
            return StatusCode(500, new { success = false, message = "Error retrieving leave calendar" });
        }
    }

    /// <summary>
    /// Get department leave calendar
    /// </summary>
    [HttpGet("department/{departmentId}/calendar")]
    [ProducesResponseType(typeof(List<LeaveCalendarDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartmentCalendar(
        Guid departmentId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        try
        {
            var calendar = await _leaveService.GetDepartmentLeaveCalendarAsync(departmentId, year, month);

            return Ok(new
            {
                success = true,
                data = calendar,
                count = calendar.Count,
                year,
                month
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department calendar");
            return StatusCode(500, new { success = false, message = "Error retrieving department calendar" });
        }
    }

    /// <summary>
    /// Get all leave types
    /// </summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(List<LeaveTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaveTypes()
    {
        try
        {
            var leaveTypes = await _leaveService.GetLeaveTypesAsync();

            return Ok(new
            {
                success = true,
                data = leaveTypes,
                count = leaveTypes.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave types");
            return StatusCode(500, new { success = false, message = "Error retrieving leave types" });
        }
    }

    /// <summary>
    /// Calculate leave encashment
    /// </summary>
    [HttpGet("encashment/calculate")]
    [ProducesResponseType(typeof(LeaveEncashmentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateEncashment(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateTime? lastWorkingDay = null)
    {
        try
        {
            var empId = employeeId ?? GetEmployeeIdFromToken();
            var lwd = lastWorkingDay ?? DateTime.UtcNow;

            var encashment = await _leaveService.CalculateLeaveEncashmentAsync(empId, lwd);

            return Ok(new
            {
                success = true,
                data = encashment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating leave encashment");
            return StatusCode(500, new { success = false, message = "Error calculating encashment" });
        }
    }

    /// <summary>
    /// Get public holidays
    /// </summary>
    [HttpGet("public-holidays")]
    [ProducesResponseType(typeof(List<PublicHolidayDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicHolidays([FromQuery] int? year = null)
    {
        try
        {
            var currentYear = year ?? DateTime.UtcNow.Year;
            var holidays = await _leaveService.GetPublicHolidaysAsync(currentYear);

            return Ok(new
            {
                success = true,
                data = holidays,
                count = holidays.Count,
                year = currentYear
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public holidays");
            return StatusCode(500, new { success = false, message = "Error retrieving public holidays" });
        }
    }

    /// <summary>
    /// Check if a date is a public holiday
    /// </summary>
    [HttpGet("is-holiday")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> IsPublicHoliday([FromQuery] DateTime date)
    {
        try
        {
            var isHoliday = await _leaveService.IsPublicHolidayAsync(date);

            return Ok(new
            {
                success = true,
                data = new
                {
                    date = date.Date,
                    isHoliday
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if date is holiday");
            return StatusCode(500, new { success = false, message = "Error checking holiday status" });
        }
    }

    #region Private Helper Methods

    private Guid GetEmployeeIdFromToken()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            throw new UnauthorizedAccessException("Employee ID not found in token");
        }

        return employeeId;
    }

    #endregion
}
