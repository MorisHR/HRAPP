using HRMS.Application.DTOs.TimesheetDtos;
using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRMS.API.Controllers;

/// <summary>
/// Timesheet management API
/// Handles timesheet generation, approval workflow, and adjustments
/// Bridges attendance tracking and payroll processing
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimesheetController : ControllerBase
{
    private readonly ITimesheetGenerationService _generationService;
    private readonly ITimesheetApprovalService _approvalService;
    private readonly ITimesheetAdjustmentService _adjustmentService;
    private readonly TenantDbContext _context;
    private readonly ILogger<TimesheetController> _logger;

    public TimesheetController(
        ITimesheetGenerationService generationService,
        ITimesheetApprovalService approvalService,
        ITimesheetAdjustmentService adjustmentService,
        TenantDbContext context,
        ILogger<TimesheetController> logger)
    {
        _generationService = generationService;
        _approvalService = approvalService;
        _adjustmentService = adjustmentService;
        _context = context;
        _logger = logger;
    }

    // ==================== EMPLOYEE ENDPOINTS ====================

    /// <summary>
    /// Get all timesheets for the logged-in employee
    /// </summary>
    [HttpGet("my-timesheets")]
    public async Task<IActionResult> GetMyTimesheets(
        [FromQuery] TimesheetStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var employeeId = GetEmployeeId();

            var query = _context.Timesheets
                .Include(t => t.Employee)
                .Where(t => t.EmployeeId == employeeId && !t.IsDeleted);

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.PeriodStart >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(t => t.PeriodEnd <= toDate.Value);
            }

            var timesheets = await query
                .OrderByDescending(t => t.PeriodStart)
                .Select(t => new TimesheetListDto
                {
                    Id = t.Id,
                    EmployeeId = t.EmployeeId,
                    EmployeeName = t.Employee!.FullName,
                    EmployeeCode = t.Employee.EmployeeCode,
                    PeriodType = t.PeriodType,
                    PeriodStart = t.PeriodStart,
                    PeriodEnd = t.PeriodEnd,
                    TotalPayableHours = t.TotalPayableHours,
                    TotalOvertimeHours = t.TotalOvertimeHours,
                    Status = t.Status,
                    IsLocked = t.IsLocked,
                    SubmittedAt = t.SubmittedAt,
                    ApprovedAt = t.ApprovedAt,
                    ApprovedByName = t.ApprovedByName,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(timesheets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee timesheets");
            return StatusCode(500, new { error = "An error occurred while retrieving timesheets" });
        }
    }

    /// <summary>
    /// Get timesheet details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTimesheetById(Guid id)
    {
        try
        {
            var employeeId = GetEmployeeId();

            var timesheet = await _context.Timesheets
                .Include(t => t.Employee)
                .Include(t => t.Entries)
                .ThenInclude(e => e.Adjustments)
                .Include(t => t.Comments)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

            if (timesheet == null)
            {
                return NotFound(new { error = "Timesheet not found" });
            }

            // Check authorization - employee can only view their own, managers can view their team's
            if (timesheet.EmployeeId != employeeId)
            {
                var canApprove = await _approvalService.CanApproveTimesheetAsync(id, employeeId);
                if (!canApprove)
                {
                    return Forbid();
                }
            }

            var dto = MapToTimesheetDto(timesheet);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving timesheet {TimesheetId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving timesheet" });
        }
    }

    /// <summary>
    /// Submit timesheet for approval
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> SubmitTimesheet(Guid id)
    {
        try
        {
            var employeeId = GetEmployeeId();

            var timesheet = await _approvalService.SubmitTimesheetAsync(id, employeeId);

            return Ok(new
            {
                message = "Timesheet submitted successfully",
                timesheetId = timesheet.Id,
                status = timesheet.Status.ToString()
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized timesheet submission attempt");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid timesheet submission");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting timesheet {TimesheetId}", id);
            return StatusCode(500, new { error = "An error occurred while submitting timesheet" });
        }
    }

    /// <summary>
    /// Reopen rejected timesheet for editing
    /// </summary>
    [HttpPost("{id:guid}/reopen")]
    public async Task<IActionResult> ReopenTimesheet(Guid id)
    {
        try
        {
            var employeeId = GetEmployeeId();

            var timesheet = await _approvalService.ReopenTimesheetAsync(id, employeeId);

            return Ok(new
            {
                message = "Timesheet reopened successfully",
                timesheetId = timesheet.Id,
                status = timesheet.Status.ToString()
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized timesheet reopen attempt");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid timesheet reopen");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening timesheet {TimesheetId}", id);
            return StatusCode(500, new { error = "An error occurred while reopening timesheet" });
        }
    }

    /// <summary>
    /// Update timesheet entry (only for Draft status)
    /// </summary>
    [HttpPut("entries/{entryId:guid}")]
    public async Task<IActionResult> UpdateTimesheetEntry(
        Guid entryId,
        [FromBody] UpdateTimesheetEntryRequest request)
    {
        try
        {
            var employeeId = GetEmployeeId();

            var entry = await _context.TimesheetEntries
                .Include(e => e.Timesheet)
                .FirstOrDefaultAsync(e => e.Id == entryId && !e.IsDeleted);

            if (entry == null)
            {
                return NotFound(new { error = "Timesheet entry not found" });
            }

            // Check ownership
            if (entry.Timesheet?.EmployeeId != employeeId)
            {
                return Forbid();
            }

            // Can only edit Draft timesheets
            if (entry.Timesheet?.Status != TimesheetStatus.Draft)
            {
                return BadRequest(new { error = "Can only edit Draft timesheets" });
            }

            // Update fields
            if (request.ClockInTime.HasValue)
            {
                entry.ClockInTime = request.ClockInTime.Value;
            }

            if (request.ClockOutTime.HasValue)
            {
                entry.ClockOutTime = request.ClockOutTime.Value;
            }

            if (request.BreakDuration.HasValue)
            {
                entry.BreakDuration = request.BreakDuration.Value;
            }

            if (request.Notes != null)
            {
                entry.Notes = request.Notes;
            }

            // Recalculate hours
            entry.CalculateHours();
            entry.UpdatedAt = DateTime.UtcNow;

            // Recalculate timesheet totals
            entry.Timesheet.CalculateTotals();
            entry.Timesheet.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Timesheet entry updated successfully",
                entryId = entry.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating timesheet entry {EntryId}", entryId);
            return StatusCode(500, new { error = "An error occurred while updating timesheet entry" });
        }
    }

    /// <summary>
    /// Add comment to timesheet
    /// </summary>
    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest request)
    {
        try
        {
            var employeeId = GetEmployeeId();

            var timesheet = await _context.Timesheets
                .Include(t => t.Employee)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

            if (timesheet == null)
            {
                return NotFound(new { error = "Timesheet not found" });
            }

            // Check authorization
            if (timesheet.EmployeeId != employeeId)
            {
                var canApprove = await _approvalService.CanApproveTimesheetAsync(id, employeeId);
                if (!canApprove)
                {
                    return Forbid();
                }
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            var comment = new TimesheetComment
            {
                Id = Guid.NewGuid(),
                TimesheetId = id,
                UserId = employeeId,
                UserName = employee?.FullName ?? "Unknown",
                Comment = request.Comment,
                CommentedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.TimesheetComments.Add(comment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Comment added successfully",
                commentId = comment.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to timesheet {TimesheetId}", id);
            return StatusCode(500, new { error = "An error occurred while adding comment" });
        }
    }

    // ==================== MANAGER ENDPOINTS ====================

    /// <summary>
    /// Get timesheets pending approval for the manager
    /// </summary>
    [HttpGet("pending-approvals")]
    [Authorize(Roles = "Manager,HR,Admin")]
    public async Task<IActionResult> GetPendingApprovals()
    {
        try
        {
            var managerId = GetEmployeeId();

            var timesheets = await _approvalService.GetPendingApprovalsForManagerAsync(managerId);

            var dtos = timesheets.Select(t => new TimesheetListDto
            {
                Id = t.Id,
                EmployeeId = t.EmployeeId,
                EmployeeName = t.Employee?.FullName,
                EmployeeCode = t.Employee?.EmployeeCode,
                PeriodType = t.PeriodType,
                PeriodStart = t.PeriodStart,
                PeriodEnd = t.PeriodEnd,
                TotalPayableHours = t.TotalPayableHours,
                TotalOvertimeHours = t.TotalOvertimeHours,
                Status = t.Status,
                IsLocked = t.IsLocked,
                SubmittedAt = t.SubmittedAt,
                CreatedAt = t.CreatedAt
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending approvals");
            return StatusCode(500, new { error = "An error occurred while retrieving pending approvals" });
        }
    }

    /// <summary>
    /// Approve timesheet
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Manager,HR,Admin")]
    public async Task<IActionResult> ApproveTimesheet(Guid id, [FromBody] string? notes = null)
    {
        try
        {
            var managerId = GetEmployeeId();

            var timesheet = await _approvalService.ApproveTimesheetAsync(id, managerId, notes);

            return Ok(new
            {
                message = "Timesheet approved successfully",
                timesheetId = timesheet.Id,
                status = timesheet.Status.ToString()
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized timesheet approval attempt");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid timesheet approval");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving timesheet {TimesheetId}", id);
            return StatusCode(500, new { error = "An error occurred while approving timesheet" });
        }
    }

    /// <summary>
    /// Reject timesheet with reason
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Manager,HR,Admin")]
    public async Task<IActionResult> RejectTimesheet(
        Guid id,
        [FromBody] RejectTimesheetRequest request)
    {
        try
        {
            var managerId = GetEmployeeId();

            var timesheet = await _approvalService.RejectTimesheetAsync(
                id, managerId, request.RejectionReason);

            return Ok(new
            {
                message = "Timesheet rejected successfully",
                timesheetId = timesheet.Id,
                status = timesheet.Status.ToString()
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized timesheet rejection attempt");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid timesheet rejection");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting timesheet {TimesheetId}", id);
            return StatusCode(500, new { error = "An error occurred while rejecting timesheet" });
        }
    }

    /// <summary>
    /// Bulk approve timesheets
    /// </summary>
    [HttpPost("bulk-approve")]
    [Authorize(Roles = "Manager,HR,Admin")]
    public async Task<IActionResult> BulkApproveTimesheets([FromBody] BulkApproveRequest request)
    {
        try
        {
            var managerId = GetEmployeeId();

            var approvedCount = await _approvalService.BulkApproveTimesheetsAsync(
                request.TimesheetIds, managerId);

            return Ok(new
            {
                message = $"Approved {approvedCount} out of {request.TimesheetIds.Count} timesheets",
                approvedCount,
                totalCount = request.TimesheetIds.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk timesheet approval");
            return StatusCode(500, new { error = "An error occurred during bulk approval" });
        }
    }

    // ==================== ADMIN ENDPOINTS ====================

    /// <summary>
    /// Generate timesheets for period (manual trigger)
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> GenerateTimesheets([FromBody] GenerateTimesheetRequest request)
    {
        try
        {
            var tenantId = GetTenantId();

            if (request.EmployeeIds?.Any() == true)
            {
                // Generate for specific employees
                int generated = 0;
                foreach (var employeeId in request.EmployeeIds)
                {
                    await _generationService.GenerateTimesheetForEmployeeAsync(
                        employeeId,
                        request.PeriodStart,
                        request.PeriodEnd,
                        request.PeriodType);
                    generated++;
                }

                return Ok(new
                {
                    message = $"Generated {generated} timesheets",
                    count = generated
                });
            }
            else
            {
                // Generate for all employees
                var count = await _generationService.GenerateTimesheetsForPeriodAsync(
                    request.PeriodStart,
                    request.PeriodEnd,
                    request.PeriodType,
                    tenantId);

                return Ok(new
                {
                    message = $"Generated {count} timesheets for all employees",
                    count
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating timesheets");
            return StatusCode(500, new { error = "An error occurred while generating timesheets" });
        }
    }

    /// <summary>
    /// Regenerate timesheet from attendance (only for Draft)
    /// </summary>
    [HttpPost("{id:guid}/regenerate")]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> RegenerateTimesheet(Guid id)
    {
        try
        {
            var timesheet = await _generationService.RegenerateTimesheetAsync(id);

            return Ok(new
            {
                message = "Timesheet regenerated successfully",
                timesheetId = timesheet.Id,
                entriesCount = timesheet.Entries.Count
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid timesheet regeneration");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating timesheet {TimesheetId}", id);
            return StatusCode(500, new { error = "An error occurred while regenerating timesheet" });
        }
    }

    /// <summary>
    /// Get all timesheets (Admin view)
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> GetAllTimesheets(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] TimesheetStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var query = _context.Timesheets
                .Include(t => t.Employee)
                .Where(t => !t.IsDeleted);

            if (employeeId.HasValue)
            {
                query = query.Where(t => t.EmployeeId == employeeId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.PeriodStart >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(t => t.PeriodEnd <= toDate.Value);
            }

            var totalCount = await query.CountAsync();

            var timesheets = await query
                .OrderByDescending(t => t.PeriodStart)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TimesheetListDto
                {
                    Id = t.Id,
                    EmployeeId = t.EmployeeId,
                    EmployeeName = t.Employee!.FullName,
                    EmployeeCode = t.Employee.EmployeeCode,
                    PeriodType = t.PeriodType,
                    PeriodStart = t.PeriodStart,
                    PeriodEnd = t.PeriodEnd,
                    TotalPayableHours = t.TotalPayableHours,
                    TotalOvertimeHours = t.TotalOvertimeHours,
                    Status = t.Status,
                    IsLocked = t.IsLocked,
                    SubmittedAt = t.SubmittedAt,
                    ApprovedAt = t.ApprovedAt,
                    ApprovedByName = t.ApprovedByName,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                data = timesheets,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all timesheets");
            return StatusCode(500, new { error = "An error occurred while retrieving timesheets" });
        }
    }

    /// <summary>
    /// Lock timesheet for payroll processing
    /// </summary>
    [HttpPost("{id:guid}/lock")]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> LockTimesheet(Guid id)
    {
        try
        {
            var userId = GetEmployeeId();

            var timesheet = await _approvalService.LockTimesheetAsync(id, userId);

            return Ok(new
            {
                message = "Timesheet locked for payroll processing",
                timesheetId = timesheet.Id,
                status = timesheet.Status.ToString()
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid timesheet lock");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking timesheet {TimesheetId}", id);
            return StatusCode(500, new { error = "An error occurred while locking timesheet" });
        }
    }

    // ==================== ADJUSTMENT ENDPOINTS ====================

    /// <summary>
    /// Create adjustment request
    /// </summary>
    [HttpPost("entries/{entryId:guid}/adjustments")]
    public async Task<IActionResult> CreateAdjustment(
        Guid entryId,
        [FromBody] CreateAdjustmentRequest request)
    {
        try
        {
            var employeeId = GetEmployeeId();
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            var adjustment = await _adjustmentService.CreateAdjustmentAsync(
                entryId,
                request.AdjustmentType,
                request.FieldName,
                request.OldValue,
                request.NewValue,
                request.Reason,
                employeeId,
                employee?.FullName ?? "Unknown");

            return Ok(new
            {
                message = "Adjustment created successfully",
                adjustmentId = adjustment.Id,
                status = adjustment.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating adjustment for entry {EntryId}", entryId);
            return StatusCode(500, new { error = "An error occurred while creating adjustment" });
        }
    }

    /// <summary>
    /// Approve adjustment
    /// </summary>
    [HttpPost("adjustments/{adjustmentId:guid}/approve")]
    [Authorize(Roles = "Manager,HR,Admin")]
    public async Task<IActionResult> ApproveAdjustment(Guid adjustmentId)
    {
        try
        {
            var managerId = GetEmployeeId();
            var manager = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == managerId);

            var adjustment = await _adjustmentService.ApproveAdjustmentAsync(
                adjustmentId,
                managerId,
                manager?.FullName ?? "Unknown");

            return Ok(new
            {
                message = "Adjustment approved and applied",
                adjustmentId = adjustment.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving adjustment {AdjustmentId}", adjustmentId);
            return StatusCode(500, new { error = "An error occurred while approving adjustment" });
        }
    }

    /// <summary>
    /// Reject adjustment
    /// </summary>
    [HttpPost("adjustments/{adjustmentId:guid}/reject")]
    [Authorize(Roles = "Manager,HR,Admin")]
    public async Task<IActionResult> RejectAdjustment(
        Guid adjustmentId,
        [FromBody] string rejectionReason)
    {
        try
        {
            var managerId = GetEmployeeId();

            var adjustment = await _adjustmentService.RejectAdjustmentAsync(
                adjustmentId,
                managerId,
                rejectionReason);

            return Ok(new
            {
                message = "Adjustment rejected",
                adjustmentId = adjustment.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting adjustment {AdjustmentId}", adjustmentId);
            return StatusCode(500, new { error = "An error occurred while rejecting adjustment" });
        }
    }

    // ==================== HELPER METHODS ====================

    private Guid GetEmployeeId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        return Guid.Parse(userIdClaim);
    }

    private Guid GetTenantId()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            return Guid.Empty;
        }

        return Guid.Parse(tenantIdClaim);
    }

    private TimesheetDto MapToTimesheetDto(Timesheet timesheet)
    {
        return new TimesheetDto
        {
            Id = timesheet.Id,
            EmployeeId = timesheet.EmployeeId,
            EmployeeName = timesheet.Employee?.FullName,
            EmployeeCode = timesheet.Employee?.EmployeeCode,
            PeriodType = timesheet.PeriodType,
            PeriodStart = timesheet.PeriodStart,
            PeriodEnd = timesheet.PeriodEnd,
            TotalRegularHours = timesheet.TotalRegularHours,
            TotalOvertimeHours = timesheet.TotalOvertimeHours,
            TotalHolidayHours = timesheet.TotalHolidayHours,
            TotalSickLeaveHours = timesheet.TotalSickLeaveHours,
            TotalAnnualLeaveHours = timesheet.TotalAnnualLeaveHours,
            TotalAbsentHours = timesheet.TotalAbsentHours,
            TotalPayableHours = timesheet.TotalPayableHours,
            Status = timesheet.Status,
            IsLocked = timesheet.IsLocked,
            SubmittedAt = timesheet.SubmittedAt,
            ApprovedAt = timesheet.ApprovedAt,
            ApprovedBy = timesheet.ApprovedBy,
            ApprovedByName = timesheet.ApprovedByName,
            RejectedAt = timesheet.RejectedAt,
            RejectionReason = timesheet.RejectionReason,
            Notes = timesheet.Notes,
            CreatedAt = timesheet.CreatedAt,
            UpdatedAt = timesheet.UpdatedAt,
            Entries = timesheet.Entries.OrderBy(e => e.Date).Select(e => new TimesheetEntryDto
            {
                Id = e.Id,
                TimesheetId = e.TimesheetId,
                Date = e.Date,
                DayOfWeek = e.Date.ToString("dddd"),
                AttendanceId = e.AttendanceId,
                ClockInTime = e.ClockInTime,
                ClockOutTime = e.ClockOutTime,
                BreakDuration = e.BreakDuration,
                ActualHours = e.ActualHours,
                RegularHours = e.RegularHours,
                OvertimeHours = e.OvertimeHours,
                HolidayHours = e.HolidayHours,
                SickLeaveHours = e.SickLeaveHours,
                AnnualLeaveHours = e.AnnualLeaveHours,
                IsAbsent = e.IsAbsent,
                IsHoliday = e.IsHoliday,
                IsWeekend = e.IsWeekend,
                IsOnLeave = e.IsOnLeave,
                DayType = e.DayType,
                Notes = e.Notes,
                Adjustments = e.Adjustments.Select(a => new TimesheetAdjustmentDto
                {
                    Id = a.Id,
                    TimesheetEntryId = a.TimesheetEntryId,
                    AdjustmentType = a.AdjustmentType,
                    FieldName = a.FieldName,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    Reason = a.Reason,
                    AdjustedBy = a.AdjustedBy,
                    AdjustedByName = a.AdjustedByName,
                    AdjustedAt = a.AdjustedAt,
                    Status = a.Status,
                    ApprovedBy = a.ApprovedBy,
                    ApprovedByName = a.ApprovedByName,
                    ApprovedAt = a.ApprovedAt,
                    RejectionReason = a.RejectionReason
                }).ToList()
            }).ToList(),
            Comments = timesheet.Comments.OrderBy(c => c.CommentedAt).Select(c => new TimesheetCommentDto
            {
                Id = c.Id,
                TimesheetId = c.TimesheetId,
                UserId = c.UserId,
                UserName = c.UserName,
                Comment = c.Comment,
                CommentedAt = c.CommentedAt
            }).ToList()
        };
    }
}
