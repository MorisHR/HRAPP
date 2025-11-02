using HRMS.Application.DTOs.AttendanceDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Attendance management with sector-aware calculations
/// Supports manual check-in/out, biometric integration, and attendance corrections
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(
        IAttendanceService attendanceService,
        ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    /// <summary>
    /// Record attendance (manual check-in/out)
    /// </summary>
    /// <remarks>
    /// POST /api/attendance
    /// {
    ///   "employeeId": "guid",
    ///   "date": "2025-11-01",
    ///   "checkIn": "2025-11-01T08:00:00Z",
    ///   "checkOut": "2025-11-01T17:00:00Z",
    ///   "status": 1
    /// }
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "HR,Manager,Admin")]
    public async Task<IActionResult> RecordAttendance([FromBody] CreateAttendanceDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token");

            var attendanceId = await _attendanceService.RecordAttendanceAsync(dto, userId);

            return CreatedAtAction(
                nameof(GetAttendanceById),
                new { id = attendanceId },
                new { id = attendanceId, message = "Attendance recorded successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while recording attendance");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording attendance");
            return StatusCode(500, new { error = "An error occurred while recording attendance" });
        }
    }

    /// <summary>
    /// Get attendance by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAttendanceById(Guid id)
    {
        try
        {
            var attendance = await _attendanceService.GetAttendanceByIdAsync(id);

            if (attendance == null)
            {
                return NotFound(new { error = "Attendance record not found" });
            }

            return Ok(attendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance {AttendanceId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving attendance" });
        }
    }

    /// <summary>
    /// Get attendances with filters
    /// </summary>
    /// <remarks>
    /// GET /api/attendance?fromDate=2025-11-01&amp;toDate=2025-11-30&amp;employeeId=guid&amp;status=1
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetAttendances(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] AttendanceStatus? status = null)
    {
        try
        {
            var attendances = await _attendanceService.GetAttendancesAsync(
                fromDate, toDate, employeeId, departmentId, status);

            return Ok(new
            {
                total = attendances.Count,
                data = attendances
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendances");
            return StatusCode(500, new { error = "An error occurred while retrieving attendances" });
        }
    }

    /// <summary>
    /// Calculate working hours for an attendance record
    /// </summary>
    [HttpGet("{id:guid}/working-hours")]
    public async Task<IActionResult> CalculateWorkingHours(Guid id)
    {
        try
        {
            var hours = await _attendanceService.CalculateWorkingHoursAsync(id);

            return Ok(new
            {
                attendanceId = id,
                workingHours = hours,
                formattedHours = $"{Math.Floor(hours)}h {(hours % 1) * 60:00}m"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating working hours for {AttendanceId}", id);
            return StatusCode(500, new { error = "An error occurred while calculating working hours" });
        }
    }

    /// <summary>
    /// Calculate overtime hours for employee in a week (SECTOR-AWARE)
    /// </summary>
    /// <remarks>
    /// GET /api/attendance/overtime/employee/{employeeId}?weekStartDate=2025-11-01
    ///
    /// This endpoint demonstrates Industry Sector System integration:
    /// - Gets employee's tenant sector
    /// - Fetches sector OVERTIME compliance rules
    /// - Applies sector-specific rates (weekday 1.5x, Sunday 2x, holiday 2-3x)
    /// </remarks>
    [HttpGet("overtime/employee/{employeeId:guid}")]
    public async Task<IActionResult> CalculateOvertimeHours(Guid employeeId, [FromQuery] DateTime weekStartDate)
    {
        try
        {
            var overtimeHours = await _attendanceService.CalculateOvertimeHoursAsync(employeeId, weekStartDate);

            return Ok(new
            {
                employeeId,
                weekStartDate = weekStartDate.Date,
                weekEndDate = weekStartDate.AddDays(6).Date,
                overtimeHours,
                formattedHours = $"{Math.Floor(overtimeHours)}h {(overtimeHours % 1) * 60:00}m",
                message = "Calculated using sector-specific overtime rules"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating overtime for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while calculating overtime" });
        }
    }

    /// <summary>
    /// Get monthly attendance summary for employee
    /// </summary>
    /// <remarks>
    /// GET /api/attendance/monthly/employee/{employeeId}?year=2025&amp;month=11
    /// </remarks>
    [HttpGet("monthly/employee/{employeeId:guid}")]
    public async Task<IActionResult> GetMonthlyAttendance(Guid employeeId, [FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            if (year < 2000 || year > 2100 || month < 1 || month > 12)
            {
                return BadRequest(new { error = "Invalid year or month" });
            }

            var summary = await _attendanceService.GetMonthlyAttendanceAsync(employeeId, year, month);

            return Ok(summary);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving monthly attendance for employee {EmployeeId}", employeeId);
            return StatusCode(500, new { error = "An error occurred while retrieving monthly attendance" });
        }
    }

    /// <summary>
    /// Get team attendance for a manager (today's date)
    /// </summary>
    [HttpGet("team/manager/{managerId:guid}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> GetTeamAttendanceToday(Guid managerId)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var teamAttendance = await _attendanceService.GetTeamAttendanceAsync(managerId, today);

            return Ok(new
            {
                managerId,
                date = today,
                total = teamAttendance.Count,
                data = teamAttendance
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving team attendance for manager {ManagerId}", managerId);
            return StatusCode(500, new { error = "An error occurred while retrieving team attendance" });
        }
    }

    /// <summary>
    /// Get team attendance for a manager (specific date)
    /// </summary>
    [HttpGet("team/manager/{managerId:guid}/date")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> GetTeamAttendanceByDate(Guid managerId, [FromQuery] DateTime date)
    {
        try
        {
            var teamAttendance = await _attendanceService.GetTeamAttendanceAsync(managerId, date.Date);

            return Ok(new
            {
                managerId,
                date = date.Date,
                total = teamAttendance.Count,
                data = teamAttendance
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving team attendance for manager {ManagerId}", managerId);
            return StatusCode(500, new { error = "An error occurred while retrieving team attendance" });
        }
    }

    /// <summary>
    /// Mark employees as absent for a specific date (admin/automated job)
    /// </summary>
    [HttpPost("mark-absent")]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> MarkAbsentForDate([FromQuery] DateTime date)
    {
        try
        {
            await _attendanceService.MarkAbsentForDateAsync(date.Date);

            return Ok(new
            {
                message = $"Marked absent for {date:yyyy-MM-dd}",
                date = date.Date
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking absent for date {Date}", date);
            return StatusCode(500, new { error = "An error occurred while marking absent" });
        }
    }

    /// <summary>
    /// Request attendance correction
    /// </summary>
    /// <remarks>
    /// POST /api/attendance/corrections
    /// {
    ///   "attendanceId": "guid",
    ///   "correctedCheckIn": "2025-11-01T08:00:00Z",
    ///   "correctedCheckOut": "2025-11-01T17:00:00Z",
    ///   "reason": "Forgot to punch in"
    /// }
    /// </remarks>
    [HttpPost("corrections")]
    public async Task<IActionResult> RequestAttendanceCorrection([FromBody] AttendanceCorrectionRequestDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token");

            var userIdGuid = Guid.Parse(userId);
            var correctionId = await _attendanceService.RequestAttendanceCorrectionAsync(dto, userIdGuid);

            return CreatedAtAction(
                nameof(GetAttendanceById),
                new { id = dto.AttendanceId },
                new { correctionId, message = "Attendance correction requested successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while requesting attendance correction");
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting attendance correction");
            return StatusCode(500, new { error = "An error occurred while requesting attendance correction" });
        }
    }

    /// <summary>
    /// Approve or reject attendance correction
    /// </summary>
    /// <remarks>
    /// PUT /api/attendance/corrections/{correctionId}/approve
    /// {
    ///   "isApproved": true,
    ///   "rejectionReason": null
    /// }
    /// </remarks>
    [HttpPut("corrections/{correctionId:guid}/approve")]
    [Authorize(Roles = "HR,Manager,Admin")]
    public async Task<IActionResult> ApproveAttendanceCorrection(
        Guid correctionId,
        [FromBody] ApproveAttendanceCorrectionDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found in token");

            var userIdGuid = Guid.Parse(userId);
            var success = await _attendanceService.ApproveAttendanceCorrectionAsync(correctionId, dto, userIdGuid);

            if (!success)
            {
                return BadRequest(new { error = "Failed to approve attendance correction" });
            }

            var status = dto.IsApproved ? "approved" : "rejected";
            return Ok(new { message = $"Attendance correction {status} successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while approving attendance correction");
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving attendance correction");
            return StatusCode(500, new { error = "An error occurred while approving attendance correction" });
        }
    }

    /// <summary>
    /// Generate attendance report
    /// </summary>
    /// <remarks>
    /// GET /api/attendance/reports?fromDate=2025-11-01&amp;toDate=2025-11-30&amp;departmentId=guid
    /// </remarks>
    [HttpGet("reports")]
    [Authorize(Roles = "HR,Manager,Admin")]
    public async Task<IActionResult> GenerateAttendanceReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid? departmentId = null)
    {
        try
        {
            if (toDate < fromDate)
            {
                return BadRequest(new { error = "toDate must be greater than or equal to fromDate" });
            }

            if ((toDate - fromDate).TotalDays > 366)
            {
                return BadRequest(new { error = "Report period cannot exceed 1 year" });
            }

            var report = await _attendanceService.GenerateAttendanceReportAsync(fromDate, toDate, departmentId);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attendance report");
            return StatusCode(500, new { error = "An error occurred while generating attendance report" });
        }
    }
}
