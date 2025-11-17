using HRMS.Application.DTOs.AttendanceDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;
using HRMS.Core.Exceptions;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security;
using System.Text.Json;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Attendance management service with SECTOR-AWARE overtime calculation
/// Integrates with Industry Sector System for compliance rules
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly TenantDbContext _tenantContext;
    private readonly MasterDbContext _masterContext;
    private readonly ISectorComplianceService _sectorComplianceService;
    private readonly ITenantService _tenantService;
    private readonly ILeaveService _leaveService;
    private readonly ILogger<AttendanceService> _logger;
    private readonly ICurrentUserService _currentUserService;

    // Role constants for authorization
    private const string RoleAdmin = "Admin";
    private const string RoleHR = "HR";
    private const string RoleManager = "Manager";
    private const string RoleEmployee = "Employee";

    public AttendanceService(
        TenantDbContext tenantContext,
        MasterDbContext masterContext,
        ISectorComplianceService sectorComplianceService,
        ITenantService tenantService,
        ILeaveService leaveService,
        ILogger<AttendanceService> logger,
        ICurrentUserService currentUserService)
    {
        _tenantContext = tenantContext;
        _masterContext = masterContext;
        _sectorComplianceService = sectorComplianceService;
        _tenantService = tenantService;
        _leaveService = leaveService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    #region Authorization Helper Methods

    /// <summary>
    /// Checks if the current user is HR or Admin
    /// </summary>
    private bool IsHROrAdmin()
    {
        return _currentUserService.HasAnyRole(RoleAdmin, RoleHR);
    }

    /// <summary>
    /// Checks if the current user can view an employee's attendance
    /// SECURITY:
    /// - Admin/HR can view all
    /// - Manager can view direct reports
    /// - Employee can view their own
    /// </summary>
    private async Task<bool> CanViewAttendanceAsync(Guid employeeId)
    {
        // Admin and HR can view all attendance
        if (IsHROrAdmin())
            return true;

        // Get current employee record
        var currentEmployee = await GetCurrentEmployeeAsync();
        if (currentEmployee == null)
            return false;

        // Employee can view their own attendance
        if (currentEmployee.Id == employeeId)
            return true;

        // Manager can view direct reports' attendance
        if (_currentUserService.HasRole(RoleManager))
        {
            var targetEmployee = await _tenantContext.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (targetEmployee?.ManagerId == currentEmployee.Id)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the current user can modify an employee's attendance record
    /// SECURITY: Only Admin/HR can modify attendance records
    /// Note: Employees can record punches but not modify existing records
    /// </summary>
    private bool CanModifyAttendance()
    {
        // Only Admin/HR can modify attendance records
        return IsHROrAdmin();
    }

    /// <summary>
    /// Logs authorization failure for security audit
    /// </summary>
    private void LogAuthorizationFailure(string operation, Guid? resourceId = null)
    {
        _logger.LogWarning(
            "AUTHORIZATION FAILURE: User {UserId} ({Username}) attempted {Operation} on resource {ResourceId}. Roles: {Roles}",
            _currentUserService.UserId,
            _currentUserService.Username,
            operation,
            resourceId,
            string.Join(", ", _currentUserService.Roles));
    }

    /// <summary>
    /// Logs attendance modification for audit trail
    /// </summary>
    private void LogAttendanceModification(string action, Guid attendanceId, Guid employeeId)
    {
        _logger.LogInformation(
            "ATTENDANCE MODIFICATION: User {UserId} performed {Action} on Attendance {AttendanceId} for Employee {EmployeeId}",
            _currentUserService.UserId,
            action,
            attendanceId,
            employeeId);
    }

    /// <summary>
    /// Gets the current employee record by user ID
    /// </summary>
    private async Task<Employee?> GetCurrentEmployeeAsync()
    {
        if (string.IsNullOrEmpty(_currentUserService.Email))
            return null;

        return await _tenantContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Email == _currentUserService.Email && e.IsActive);
    }

    #endregion

    /// <summary>
    /// ✅ FORTUNE 500: Employee self-service check-in
    /// Creates attendance record for today with check-in time
    /// SECURITY: Employees can check in for themselves
    /// </summary>
    public async Task<AttendanceDetailsDto> CheckInAsync(Guid employeeId)
    {
        _logger.LogInformation("Employee {EmployeeId} checking in", employeeId);

        // AUTHORIZATION CHECK: Verify user can check in for this employee
        var currentEmployee = await GetCurrentEmployeeAsync();
        if (currentEmployee == null || (currentEmployee.Id != employeeId && !IsHROrAdmin()))
        {
            LogAuthorizationFailure("CheckIn", employeeId);
            throw new ForbiddenException(
                ErrorCodes.ATT_UNAUTHORIZED_ACCESS,
                "You can only check in for yourself.",
                $"User attempted to check in for employee {employeeId} without permission",
                "Use your own employee account to check in.");
        }

        // Validate employee exists and is active
        var employee = await _tenantContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (employee == null)
        {
            _logger.LogWarning("SECURITY: Employee {EmployeeId} not found in current tenant context", employeeId);
            throw new NotFoundException(
                ErrorCodes.EMP_NOT_FOUND,
                "Employee information could not be found.",
                $"Employee ID {employeeId} not found in database",
                "Verify your employee account and try again.");
        }

        if (!employee.IsActive)
        {
            throw new ValidationException(
                ErrorCodes.EMP_INACTIVE,
                "Cannot check in. Employee account is inactive.",
                $"Employee {employeeId} is inactive",
                "Contact HR to activate your account.");
        }

        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        // Check for existing attendance on this date
        var existing = await _tenantContext.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == today);

        if (existing != null)
        {
            // If already checked in today
            if (existing.CheckInTime.HasValue && !existing.CheckOutTime.HasValue)
            {
                throw new ConflictException(
                    ErrorCodes.ATT_ALREADY_CHECKED_IN,
                    "You have already checked in today.",
                    $"Employee {employeeId} already checked in at {existing.CheckInTime}",
                    "Use check-out to complete your attendance for today.");
            }

            // If already checked out today
            if (existing.CheckInTime.HasValue && existing.CheckOutTime.HasValue)
            {
                throw new ConflictException(
                    ErrorCodes.ATT_ALREADY_COMPLETED,
                    "You have already completed attendance for today.",
                    $"Employee {employeeId} already checked in and out for {today:yyyy-MM-dd}",
                    "Attendance for today is complete. Contact HR for corrections.");
            }
        }

        // Determine day type
        var isSunday = today.DayOfWeek == DayOfWeek.Sunday;
        var isPublicHoliday = await IsPublicHolidayAsync(today);

        // Determine status based on check-in time
        var status = DetermineCheckInStatus(now, isSunday, isPublicHoliday);

        // Create attendance record
        var attendance = new Attendance
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Date = today,
            CheckInTime = now,
            CheckOutTime = null,
            WorkingHours = 0,
            OvertimeHours = 0,
            Status = status,
            IsSunday = isSunday,
            IsPublicHoliday = isPublicHoliday,
            IsRegularized = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.GetAuditUsername(),
            IsDeleted = false
        };

        _tenantContext.Attendances.Add(attendance);
        await _tenantContext.SaveChangesAsync();

        // Audit log
        LogAttendanceModification("CheckIn", attendance.Id, employeeId);
        _logger.LogInformation("Employee {EmployeeId} checked in successfully at {CheckInTime}",
            employeeId, now);

        // Load full details with includes
        var result = await _tenantContext.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(a => a.Id == attendance.Id);

        return MapToDetailsDto(result!);
    }

    /// <summary>
    /// ✅ FORTUNE 500: Employee self-service check-out
    /// Updates today's attendance with check-out time and calculates working hours
    /// SECURITY: Employees can check out for themselves
    /// </summary>
    public async Task<AttendanceDetailsDto> CheckOutAsync(Guid employeeId)
    {
        _logger.LogInformation("Employee {EmployeeId} checking out", employeeId);

        // AUTHORIZATION CHECK: Verify user can check out for this employee
        var currentEmployee = await GetCurrentEmployeeAsync();
        if (currentEmployee == null || (currentEmployee.Id != employeeId && !IsHROrAdmin()))
        {
            LogAuthorizationFailure("CheckOut", employeeId);
            throw new ForbiddenException(
                ErrorCodes.ATT_UNAUTHORIZED_ACCESS,
                "You can only check out for yourself.",
                $"User attempted to check out for employee {employeeId} without permission",
                "Use your own employee account to check out.");
        }

        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        // Find today's attendance record
        var attendance = await _tenantContext.Attendances
            .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == today);

        if (attendance == null)
        {
            throw new NotFoundException(
                ErrorCodes.ATT_NO_CHECKIN,
                "Cannot check out. You have not checked in today.",
                $"No attendance record found for employee {employeeId} on {today:yyyy-MM-dd}",
                "Please check in first before checking out.");
        }

        // Verify check-in exists
        if (!attendance.CheckInTime.HasValue)
        {
            throw new ValidationException(
                ErrorCodes.ATT_NO_CHECKIN,
                "Cannot check out. No check-in time found.",
                $"Attendance {attendance.Id} has no check-in time",
                "Please check in first before checking out.");
        }

        // Verify not already checked out
        if (attendance.CheckOutTime.HasValue)
        {
            throw new ConflictException(
                ErrorCodes.ATT_ALREADY_CHECKED_OUT,
                "You have already checked out today.",
                $"Employee {employeeId} already checked out at {attendance.CheckOutTime}",
                "Attendance for today is already complete. Contact HR for corrections.");
        }

        // Update check-out time
        attendance.CheckOutTime = now;
        attendance.UpdatedAt = DateTime.UtcNow;
        attendance.UpdatedBy = _currentUserService.GetAuditUsername();

        // Calculate working hours
        var totalHours = (decimal)(now - attendance.CheckInTime.Value).TotalHours;

        // Subtract break time (1 hour if working > 5 hours)
        if (totalHours > 5)
        {
            totalHours -= 1; // 1 hour lunch break
        }

        attendance.WorkingHours = Math.Round(totalHours, 2);

        // Update status to Present if it was just a check-in
        if (attendance.Status != AttendanceStatus.Late)
        {
            attendance.Status = AttendanceStatus.Present;
        }

        await _tenantContext.SaveChangesAsync();

        // Audit log
        LogAttendanceModification("CheckOut", attendance.Id, employeeId);
        _logger.LogInformation("Employee {EmployeeId} checked out successfully at {CheckOutTime}. Working hours: {WorkingHours}",
            employeeId, now, attendance.WorkingHours);

        return MapToDetailsDto(attendance);
    }

    /// <summary>
    /// Helper method to determine status during check-in
    /// </summary>
    private AttendanceStatus DetermineCheckInStatus(DateTime checkInTime, bool isSunday, bool isPublicHoliday)
    {
        if (isPublicHoliday)
        {
            return AttendanceStatus.PublicHoliday;
        }

        if (isSunday || checkInTime.DayOfWeek == DayOfWeek.Saturday)
        {
            return AttendanceStatus.Weekend;
        }

        // Check if late (assuming 9:00 AM start time - should be from shift)
        var startTime = new TimeSpan(9, 0, 0);
        if (checkInTime.TimeOfDay > startTime.Add(TimeSpan.FromMinutes(15)))
        {
            return AttendanceStatus.Late;
        }

        return AttendanceStatus.Present;
    }

    /// <summary>
    /// Records attendance/punch with role-based authorization
    /// SECURITY: Users can only record their own punches unless Admin/HR
    /// </summary>
    public async Task<Guid> RecordAttendanceAsync(CreateAttendanceDto dto, string createdBy)
    {
        _logger.LogInformation("Recording attendance for employee {EmployeeId} on {Date}", dto.EmployeeId, dto.Date);

        // AUTHORIZATION CHECK: Verify user can record punch for this employee
        if (!IsHROrAdmin())
        {
            var currentEmployee = await GetCurrentEmployeeAsync();
            if (currentEmployee == null || currentEmployee.Id != dto.EmployeeId)
            {
                LogAuthorizationFailure("RecordAttendance", dto.EmployeeId);
                throw new ForbiddenException(
                    ErrorCodes.ATT_UNAUTHORIZED_ACCESS,
                    "You can only record attendance for yourself.",
                    $"User attempted to record attendance for employee {dto.EmployeeId} without permission",
                    "Contact your HR department to record attendance for others.");
            }
        }

        // Validate employee exists
        var employee = await _tenantContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && !e.IsDeleted);

        if (employee == null)
        {
            _logger.LogWarning("SECURITY: Employee {EmployeeId} not found in current tenant context", dto.EmployeeId);
            throw new NotFoundException(
                ErrorCodes.EMP_NOT_FOUND,
                "Employee information could not be found.",
                $"Employee ID {dto.EmployeeId} not found in database",
                "Verify the employee selection and try again.");
        }

        // Check for existing attendance on this date
        var existing = await _tenantContext.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == dto.EmployeeId && a.Date.Date == dto.Date.Date);

        if (existing != null)
        {
            throw new ConflictException(
                ErrorCodes.ATT_DUPLICATE_RECORD,
                "Attendance has already been recorded for this date.",
                $"Duplicate attendance record for employee {dto.EmployeeId} on {dto.Date:yyyy-MM-dd}",
                "To make changes, please edit the existing attendance record or contact HR.");
        }

        // Validate check-in/out times
        if (dto.CheckInTime.HasValue && dto.CheckOutTime.HasValue)
        {
            if (dto.CheckOutTime <= dto.CheckInTime)
            {
                throw new ValidationException(
                    ErrorCodes.ATT_INVALID_TIME,
                    "Check-out time must be later than check-in time.",
                    $"Invalid times: Check-in={dto.CheckInTime}, Check-out={dto.CheckOutTime}",
                    "Please adjust the times and try again.");
            }
        }

        // Determine day type
        var isSunday = dto.Date.DayOfWeek == DayOfWeek.Sunday;
        var isPublicHoliday = await IsPublicHolidayAsync(dto.Date);

        // Calculate working hours
        decimal workingHours = 0;
        if (dto.CheckInTime.HasValue && dto.CheckOutTime.HasValue)
        {
            var totalHours = (decimal)(dto.CheckOutTime.Value - dto.CheckInTime.Value).TotalHours;

            // Subtract break time (default: 1 hour if working > 5 hours)
            if (totalHours > 5)
            {
                totalHours -= 1; // 1 hour lunch break
            }

            workingHours = Math.Round(totalHours, 2);
        }

        // Determine status
        var status = DetermineStatus(dto.CheckInTime, dto.CheckOutTime, isSunday, isPublicHoliday);

        // Create attendance record
        var attendance = new Attendance
        {
            Id = Guid.NewGuid(),
            EmployeeId = dto.EmployeeId,
            Date = dto.Date.Date,
            CheckInTime = dto.CheckInTime,
            CheckOutTime = dto.CheckOutTime,
            WorkingHours = workingHours,
            OvertimeHours = 0, // Will be calculated weekly
            Status = status,
            ShiftId = dto.ShiftId,
            AttendanceMachineId = dto.AttendanceMachineId,
            Remarks = dto.Remarks,
            IsSunday = isSunday,
            IsPublicHoliday = isPublicHoliday,
            IsRegularized = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            IsDeleted = false
        };

        _tenantContext.Attendances.Add(attendance);
        await _tenantContext.SaveChangesAsync();

        // Audit log for attendance recording
        LogAttendanceModification("RecordAttendance", attendance.Id, dto.EmployeeId);
        _logger.LogInformation("Attendance recorded: {AttendanceId} for employee {EmployeeId}", attendance.Id, dto.EmployeeId);

        return attendance.Id;
    }

    /// <summary>
    /// Gets attendance by ID with role-based authorization
    /// SECURITY: Verifies user can access this specific attendance record
    /// </summary>
    public async Task<AttendanceDetailsDto?> GetAttendanceByIdAsync(Guid id)
    {
        var attendance = await _tenantContext.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attendance == null)
        {
            return null;
        }

        // AUTHORIZATION CHECK: Verify user can view this attendance record
        if (!await CanViewAttendanceAsync(attendance.EmployeeId))
        {
            LogAuthorizationFailure("GetAttendanceById", id);
            throw new UnauthorizedAccessException($"You do not have permission to view attendance record {id}");
        }

        _logger.LogInformation("User {UserId} retrieved attendance {AttendanceId} for employee {EmployeeId}",
            _currentUserService.UserId, id, attendance.EmployeeId);

        return MapToDetailsDto(attendance);
    }

    /// <summary>
    /// Gets attendances with role-based filtering
    /// SECURITY:
    /// - Admin/HR: Can view all attendances
    /// - Manager: Can only view their direct reports
    /// - Employee: Can only view their own attendance
    /// </summary>
    public async Task<List<AttendanceListDto>> GetAttendancesAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? employeeId = null,
        Guid? departmentId = null,
        AttendanceStatus? status = null)
    {
        var query = _tenantContext.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
            .AsQueryable();

        // AUTHORIZATION FILTER: Apply role-based filtering
        if (!IsHROrAdmin())
        {
            var currentEmployee = await GetCurrentEmployeeAsync();
            if (currentEmployee == null)
            {
                LogAuthorizationFailure("GetAttendances");
                throw new UnauthorizedAccessException("Unable to identify current employee");
            }

            if (_currentUserService.HasRole(RoleManager))
            {
                // Manager can view their direct reports
                var directReportIds = await _tenantContext.Employees
                    .AsNoTracking()
                    .Where(e => e.ManagerId == currentEmployee.Id)
                    .Select(e => e.Id)
                    .ToListAsync();

                // Include self
                directReportIds.Add(currentEmployee.Id);
                query = query.Where(a => directReportIds.Contains(a.EmployeeId));

                _logger.LogInformation("Manager {UserId} viewing attendance for {Count} direct reports",
                    _currentUserService.UserId, directReportIds.Count);
            }
            else
            {
                // Employee can only view their own attendance
                query = query.Where(a => a.EmployeeId == currentEmployee.Id);

                _logger.LogInformation("Employee {UserId} viewing own attendance",
                    _currentUserService.UserId);
            }
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Date <= toDate.Value.Date);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(a => a.Employee!.DepartmentId == departmentId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var attendances = await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Employee!.FirstName)
            .ToListAsync();

        _logger.LogInformation("User {UserId} retrieved {Count} attendance records",
            _currentUserService.UserId, attendances.Count);

        return attendances.Select(MapToListDto).ToList();
    }

    public async Task<decimal> CalculateWorkingHoursAsync(Guid attendanceId)
    {
        var attendance = await _tenantContext.Attendances
            .FirstOrDefaultAsync(a => a.Id == attendanceId && !a.IsDeleted);

        if (attendance == null)
        {
            _logger.LogWarning("SECURITY: Attendance {AttendanceId} not found in current tenant context", attendanceId);
            throw new KeyNotFoundException($"Attendance with ID {attendanceId} not found or access denied");
        }

        if (!attendance.CheckInTime.HasValue || !attendance.CheckOutTime.HasValue)
        {
            return 0;
        }

        var totalHours = (decimal)(attendance.CheckOutTime.Value - attendance.CheckInTime.Value).TotalHours;

        // Subtract break time (1 hour if working > 5 hours)
        if (totalHours > 5)
        {
            totalHours -= 1;
        }

        var workingHours = Math.Round(totalHours, 2);

        // Update attendance record
        attendance.WorkingHours = workingHours;
        attendance.UpdatedAt = DateTime.UtcNow;
        await _tenantContext.SaveChangesAsync();

        return workingHours;
    }

    /// <summary>
    /// CRITICAL: Calculate overtime hours using SECTOR COMPLIANCE RULES
    /// This is where the Industry Sector System integration happens!
    /// </summary>
    public async Task<decimal> CalculateOvertimeHoursAsync(Guid employeeId, DateTime weekStartDate)
    {
        _logger.LogInformation("Calculating overtime for employee {EmployeeId}, week starting {WeekStart}",
            employeeId, weekStartDate);

        // Get employee
        var employee = await _tenantContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (employee == null)
        {
            _logger.LogWarning("SECURITY: Employee {EmployeeId} not found in current tenant context", employeeId);
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found or access denied");
        }

        // Get week end date
        var weekEndDate = weekStartDate.AddDays(6);

        // Get all attendance records for the week
        var attendances = await _tenantContext.Attendances
            .Where(a => a.EmployeeId == employeeId)
            .Where(a => a.Date >= weekStartDate.Date && a.Date <= weekEndDate.Date)
            .Where(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late)
            .ToListAsync();

        if (!attendances.Any())
        {
            return 0;
        }

        // Calculate total weekly hours
        var totalWeeklyHours = attendances.Sum(a => a.WorkingHours);

        // ===== SECTOR INTEGRATION: Get sector compliance rules =====

        // Get current tenant's sector
        var currentSchema = _tenantService.GetCurrentTenantSchema();
        var tenant = await _masterContext.Tenants
            .FirstOrDefaultAsync(t => t.SchemaName == currentSchema);

        if (tenant == null || !tenant.SectorId.HasValue)
        {
            _logger.LogWarning("Tenant or sector not configured. Using default overtime threshold of 45 hours.");

            // Default: 45 hours/week, 1.5x overtime
            if (totalWeeklyHours > 45)
            {
                return Math.Round(totalWeeklyHours - 45, 2);
            }
            return 0;
        }

        // Get sector's OVERTIME compliance rules
        var sectorId = tenant.SectorId.Value;
        var overtimeRule = await _masterContext.SectorComplianceRules
            .Where(r => r.SectorId == sectorId)
            .Where(r => r.RuleCategory == "OVERTIME")
            .Where(r => r.EffectiveFrom <= DateTime.UtcNow)
            .Where(r => r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();

        decimal weeklyThreshold = 45; // Default
        decimal weekdayRate = 1.5m;
        decimal sundayRate = 2.0m;
        decimal publicHolidayRate = 2.0m;

        if (overtimeRule != null)
        {
            try
            {
                var ruleConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(overtimeRule.RuleConfig);

                if (ruleConfig != null)
                {
                    // Get sector-specific thresholds and rates
                    if (ruleConfig.TryGetValue("standard_weekly_hours", out var hours))
                    {
                        weeklyThreshold = Convert.ToDecimal(hours);
                    }

                    if (ruleConfig.TryGetValue("weekday_overtime_rate", out var wRate))
                    {
                        weekdayRate = Convert.ToDecimal(wRate);
                    }

                    if (ruleConfig.TryGetValue("sunday_rate", out var sRate))
                    {
                        sundayRate = Convert.ToDecimal(sRate);
                    }

                    if (ruleConfig.TryGetValue("public_holiday_normal_hours_rate", out var phRate))
                    {
                        publicHolidayRate = Convert.ToDecimal(phRate);
                    }
                }

                _logger.LogInformation("Using sector overtime rules - Threshold: {Threshold}h, Weekday: {Weekday}x, Sunday: {Sunday}x",
                    weeklyThreshold, weekdayRate, sundayRate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing overtime rule config. Using defaults.");
            }
        }

        // Get working hours rule for threshold
        var workingHoursRule = await _masterContext.SectorComplianceRules
            .Where(r => r.SectorId == sectorId)
            .Where(r => r.RuleCategory == "WORKING_HOURS")
            .Where(r => r.EffectiveFrom <= DateTime.UtcNow)
            .Where(r => r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (workingHoursRule != null)
        {
            try
            {
                var ruleConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(workingHoursRule.RuleConfig);
                if (ruleConfig != null && ruleConfig.TryGetValue("standard_weekly_hours", out var hours))
                {
                    weeklyThreshold = Convert.ToDecimal(hours);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing working hours rule config.");
            }
        }

        // Calculate overtime
        if (totalWeeklyHours <= weeklyThreshold)
        {
            return 0;
        }

        var overtimeHours = totalWeeklyHours - weeklyThreshold;

        // Update each attendance record with overtime details
        foreach (var attendance in attendances)
        {
            // Determine the rate for this day
            decimal rate = weekdayRate;
            if (attendance.IsPublicHoliday)
            {
                rate = publicHolidayRate;
            }
            else if (attendance.IsSunday)
            {
                rate = sundayRate;
            }

            attendance.OvertimeRate = rate;
            attendance.UpdatedAt = DateTime.UtcNow;
        }

        await _tenantContext.SaveChangesAsync();

        _logger.LogInformation("Overtime calculated: {Overtime} hours (Total: {Total}h, Threshold: {Threshold}h)",
            overtimeHours, totalWeeklyHours, weeklyThreshold);

        return Math.Round(overtimeHours, 2);
    }

    /// <summary>
    /// Gets monthly attendance summary with role-based authorization
    /// SECURITY: Filters based on user's access level
    /// </summary>
    public async Task<MonthlyAttendanceSummaryDto> GetMonthlyAttendanceAsync(Guid employeeId, int year, int month)
    {
        // AUTHORIZATION CHECK: Verify user can view this employee's attendance
        if (!await CanViewAttendanceAsync(employeeId))
        {
            LogAuthorizationFailure("GetMonthlyAttendance", employeeId);
            throw new UnauthorizedAccessException($"You do not have permission to view attendance for employee {employeeId}");
        }

        var employee = await _tenantContext.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new Exception($"Employee not found: {employeeId}");
        }

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var attendances = await _tenantContext.Attendances
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .OrderBy(a => a.Date)
            .ToListAsync();

        var summary = new MonthlyAttendanceSummaryDto
        {
            EmployeeId = employeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            Year = year,
            Month = month,
            TotalWorkingDays = GetWorkingDaysInMonth(year, month),
            PresentDays = attendances.Count(a => a.Status == AttendanceStatus.Present),
            AbsentDays = attendances.Count(a => a.Status == AttendanceStatus.Absent),
            LateDays = attendances.Count(a => a.Status == AttendanceStatus.Late),
            HalfDays = attendances.Count(a => a.Status == AttendanceStatus.HalfDay),
            LeaveDays = attendances.Count(a => a.Status == AttendanceStatus.OnLeave),
            WeekendDays = attendances.Count(a => a.Status == AttendanceStatus.Weekend),
            PublicHolidayDays = attendances.Count(a => a.Status == AttendanceStatus.PublicHoliday),
            TotalWorkingHours = attendances.Sum(a => a.WorkingHours),
            TotalOvertimeHours = attendances.Sum(a => a.OvertimeHours),
            TotalLateMinutes = attendances.Sum(a => a.LateArrivalMinutes ?? 0),
            TotalEarlyDepartureMinutes = attendances.Sum(a => a.EarlyDepartureMinutes ?? 0),
            DailyRecords = attendances.Select(MapToListDto).ToList()
        };

        // Calculate attendance percentage
        var workingDays = summary.TotalWorkingDays;
        var presentDays = summary.PresentDays + summary.LateDays;
        summary.AttendancePercentage = workingDays > 0 ? Math.Round((decimal)presentDays / workingDays * 100, 2) : 0;

        return summary;
    }

    public async Task<List<AttendanceListDto>> GetTeamAttendanceAsync(Guid managerId, DateTime date)
    {
        // Get manager's department
        var manager = await _tenantContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == managerId && !e.IsDeleted);

        if (manager == null)
        {
            _logger.LogWarning("SECURITY: Manager {ManagerId} not found in current tenant context", managerId);
            throw new KeyNotFoundException($"Manager with ID {managerId} not found or access denied");
        }

        // Get all employees in manager's department
        var teamMembers = await _tenantContext.Employees
            .Where(e => e.DepartmentId == manager.DepartmentId)
            .Where(e => e.IsActive)
            .Select(e => e.Id)
            .ToListAsync();

        return await GetAttendancesAsync(
            fromDate: date.Date,
            toDate: date.Date,
            employeeId: null,
            departmentId: manager.DepartmentId,
            status: null);
    }

    public async Task MarkAbsentForDateAsync(DateTime date)
    {
        _logger.LogInformation("Marking absences for date: {Date}", date);

        // Get all active employees
        var employees = await _tenantContext.Employees
            .Where(e => e.IsActive)
            .ToListAsync();

        foreach (var employee in employees)
        {
            // Check if attendance already recorded
            var existing = await _tenantContext.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date.Date == date.Date);

            if (existing != null)
            {
                continue; // Already recorded
            }

            // Check if employee is on approved leave
            var isOnLeave = await IsOnApprovedLeaveAsync(employee.Id, date);
            if (isOnLeave)
            {
                // Mark as OnLeave
                var leaveAttendance = new Attendance
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee.Id,
                    Date = date.Date,
                    Status = AttendanceStatus.OnLeave,
                    WorkingHours = 0,
                    OvertimeHours = 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.GetAuditUsername(),
                    IsDeleted = false
                };
                _tenantContext.Attendances.Add(leaveAttendance);
                continue;
            }

            // Check if weekend or public holiday
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                continue; // Don't mark weekend as absent
            }

            if (await IsPublicHolidayAsync(date))
            {
                continue; // Don't mark public holiday as absent
            }

            // Mark as absent
            var absence = new Attendance
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                Date = date.Date,
                Status = AttendanceStatus.Absent,
                WorkingHours = 0,
                OvertimeHours = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.GetAuditUsername(),
                IsDeleted = false
            };
            _tenantContext.Attendances.Add(absence);
        }

        await _tenantContext.SaveChangesAsync();
        _logger.LogInformation("Absences marked for {Date}", date);
    }

    /// <summary>
    /// Requests attendance correction with authorization
    /// SECURITY: Users can request corrections for their own attendance or Admin/HR can request for anyone
    /// </summary>
    public async Task<Guid> RequestAttendanceCorrectionAsync(AttendanceCorrectionRequestDto dto, Guid requestedBy)
    {
        var attendance = await _tenantContext.Attendances
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == dto.AttendanceId);

        if (attendance == null)
        {
            throw new Exception($"Attendance not found: {dto.AttendanceId}");
        }

        // AUTHORIZATION CHECK: Verify user can request correction for this attendance
        if (!await CanViewAttendanceAsync(attendance.EmployeeId))
        {
            LogAuthorizationFailure("RequestAttendanceCorrection", dto.AttendanceId);
            throw new UnauthorizedAccessException("You can only request corrections for your own attendance");
        }

        // Check if correction already exists
        var existingCorrection = await _tenantContext.AttendanceCorrections
            .FirstOrDefaultAsync(c => c.AttendanceId == dto.AttendanceId && c.Status == AttendanceCorrectionStatus.Pending);

        if (existingCorrection != null)
        {
            throw new Exception("A pending correction request already exists for this attendance record");
        }

        var correction = new AttendanceCorrection
        {
            Id = Guid.NewGuid(),
            AttendanceId = dto.AttendanceId,
            EmployeeId = attendance.EmployeeId,
            RequestedBy = requestedBy,
            OriginalCheckIn = attendance.CheckInTime,
            OriginalCheckOut = attendance.CheckOutTime,
            CorrectedCheckIn = dto.CorrectedCheckIn,
            CorrectedCheckOut = dto.CorrectedCheckOut,
            Reason = dto.Reason,
            Status = AttendanceCorrectionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _tenantContext.AttendanceCorrections.Add(correction);
        await _tenantContext.SaveChangesAsync();

        // Audit log for correction request
        LogAttendanceModification("RequestCorrection", dto.AttendanceId, attendance.EmployeeId);
        _logger.LogInformation("Attendance correction requested: {CorrectionId} for attendance {AttendanceId}",
            correction.Id, dto.AttendanceId);

        return correction.Id;
    }

    /// <summary>
    /// Approves/rejects attendance correction with admin-only authorization
    /// SECURITY: Only Admin/HR can approve or reject corrections
    /// </summary>
    public async Task<bool> ApproveAttendanceCorrectionAsync(Guid correctionId, ApproveAttendanceCorrectionDto dto, Guid approvedBy)
    {
        // AUTHORIZATION CHECK: Only Admin/HR can approve/reject corrections
        if (!IsHROrAdmin())
        {
            LogAuthorizationFailure("ApproveAttendanceCorrection", correctionId);
            throw new UnauthorizedAccessException("Only Admin and HR roles can approve or reject attendance corrections");
        }

        var correction = await _tenantContext.AttendanceCorrections
            .Include(c => c.Attendance)
            .FirstOrDefaultAsync(c => c.Id == correctionId);

        if (correction == null)
        {
            throw new Exception($"Correction request not found: {correctionId}");
        }

        if (correction.Status != AttendanceCorrectionStatus.Pending)
        {
            throw new Exception("Correction request is not pending");
        }

        if (dto.IsApproved)
        {
            // Apply correction
            correction.Status = AttendanceCorrectionStatus.Approved;
            correction.ApprovedBy = approvedBy;
            correction.ApprovedAt = DateTime.UtcNow;

            // Update attendance
            if (correction.Attendance != null)
            {
                correction.Attendance.CheckInTime = correction.CorrectedCheckIn;
                correction.Attendance.CheckOutTime = correction.CorrectedCheckOut;
                correction.Attendance.IsRegularized = true;
                correction.Attendance.RegularizedBy = approvedBy;
                correction.Attendance.RegularizedAt = DateTime.UtcNow;
                correction.Attendance.UpdatedAt = DateTime.UtcNow;

                // Recalculate working hours
                await CalculateWorkingHoursAsync(correction.AttendanceId);
            }
        }
        else
        {
            correction.Status = AttendanceCorrectionStatus.Rejected;
            correction.ApprovedBy = approvedBy;
            correction.ApprovedAt = DateTime.UtcNow;
            correction.RejectionReason = dto.RejectionReason;
        }

        await _tenantContext.SaveChangesAsync();

        // Audit log for status change
        LogAttendanceModification(
            dto.IsApproved ? "ApproveCorrection" : "RejectCorrection",
            correction.AttendanceId,
            correction.EmployeeId);

        _logger.LogWarning("ATTENDANCE STATUS CHANGE: User {UserId} {Action} correction {CorrectionId} for attendance {AttendanceId}",
            _currentUserService.UserId,
            dto.IsApproved ? "approved" : "rejected",
            correctionId,
            correction.AttendanceId);

        return dto.IsApproved;
    }

    public async Task<AttendanceReportDto> GenerateAttendanceReportAsync(DateTime fromDate, DateTime toDate, Guid? departmentId = null)
    {
        var query = _tenantContext.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
                .ThenInclude(e => e!.Department)
            .Where(a => a.Date >= fromDate.Date && a.Date <= toDate.Date)
            .AsQueryable();

        if (departmentId.HasValue)
        {
            query = query.Where(a => a.Employee!.DepartmentId == departmentId.Value);
        }

        var attendances = await query.ToListAsync();

        var employeeGroups = attendances.GroupBy(a => a.EmployeeId);

        var employeeSummaries = employeeGroups.Select(group =>
        {
            var employee = group.First().Employee!;
            var workingDays = GetWorkingDaysBetween(fromDate, toDate);
            var presentDays = group.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);

            return new EmployeeAttendanceSummary
            {
                EmployeeId = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                PresentDays = group.Count(a => a.Status == AttendanceStatus.Present),
                AbsentDays = group.Count(a => a.Status == AttendanceStatus.Absent),
                LateDays = group.Count(a => a.Status == AttendanceStatus.Late),
                TotalWorkingHours = group.Sum(a => a.WorkingHours),
                TotalOvertimeHours = group.Sum(a => a.OvertimeHours),
                AttendancePercentage = workingDays > 0 ? Math.Round((decimal)presentDays / workingDays * 100, 2) : 0
            };
        }).ToList();

        var report = new AttendanceReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            DepartmentId = departmentId,
            TotalEmployees = employeeSummaries.Count,
            AverageAttendancePercentage = employeeSummaries.Any() ? Math.Round(employeeSummaries.Average(e => e.AttendancePercentage), 2) : 0,
            TotalPresentDays = attendances.Count(a => a.Status == AttendanceStatus.Present),
            TotalAbsentDays = attendances.Count(a => a.Status == AttendanceStatus.Absent),
            TotalLateDays = attendances.Count(a => a.Status == AttendanceStatus.Late),
            TotalWorkingHours = attendances.Sum(a => a.WorkingHours),
            TotalOvertimeHours = attendances.Sum(a => a.OvertimeHours),
            EmployeeSummaries = employeeSummaries
        };

        return report;
    }

    // Helper methods

    private AttendanceStatus DetermineStatus(DateTime? checkIn, DateTime? checkOut, bool isSunday, bool isPublicHoliday)
    {
        if (isPublicHoliday)
        {
            return AttendanceStatus.PublicHoliday;
        }

        if (isSunday || (checkIn.HasValue && checkIn.Value.DayOfWeek == DayOfWeek.Saturday))
        {
            return AttendanceStatus.Weekend;
        }

        if (!checkIn.HasValue)
        {
            return AttendanceStatus.Absent;
        }

        // Check if late (assuming 9:00 AM start time - should be from shift)
        var startTime = new TimeSpan(9, 0, 0);
        if (checkIn.Value.TimeOfDay > startTime.Add(TimeSpan.FromMinutes(15)))
        {
            return AttendanceStatus.Late;
        }

        return AttendanceStatus.Present;
    }

    private async Task<bool> IsPublicHolidayAsync(DateTime date)
    {
        return await _tenantContext.PublicHolidays
            .AnyAsync(h => h.Date.Date == date.Date);
    }

    private async Task<bool> IsOnApprovedLeaveAsync(Guid employeeId, DateTime date)
    {
        var leaves = await _leaveService.GetMyLeavesAsync(employeeId);
        return leaves.Any(l => l.Status == LeaveStatus.Approved &&
                               date.Date >= l.StartDate.Date &&
                               date.Date <= l.EndDate.Date);
    }

    private int GetWorkingDaysInMonth(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        return GetWorkingDaysBetween(startDate, endDate);
    }

    private int GetWorkingDaysBetween(DateTime startDate, DateTime endDate)
    {
        int workingDays = 0;
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
        }
        return workingDays;
    }

    private AttendanceListDto MapToListDto(Attendance attendance)
    {
        return new AttendanceListDto
        {
            Id = attendance.Id,
            EmployeeId = attendance.EmployeeId,
            EmployeeCode = attendance.Employee?.EmployeeCode ?? "",
            EmployeeName = attendance.Employee != null ? $"{attendance.Employee.FirstName} {attendance.Employee.LastName}" : "",
            DepartmentName = attendance.Employee?.Department?.Name,
            Date = attendance.Date,
            CheckInTime = attendance.CheckInTime,
            CheckOutTime = attendance.CheckOutTime,
            WorkingHours = attendance.WorkingHours,
            OvertimeHours = attendance.OvertimeHours,
            OvertimeRate = attendance.OvertimeRate,
            Status = attendance.Status,
            StatusDisplay = attendance.Status.ToString(),
            LateArrivalMinutes = attendance.LateArrivalMinutes,
            EarlyDepartureMinutes = attendance.EarlyDepartureMinutes,
            IsRegularized = attendance.IsRegularized,
            IsSunday = attendance.IsSunday,
            IsPublicHoliday = attendance.IsPublicHoliday
        };
    }

    private AttendanceDetailsDto MapToDetailsDto(Attendance attendance)
    {
        return new AttendanceDetailsDto
        {
            Id = attendance.Id,
            EmployeeId = attendance.EmployeeId,
            EmployeeCode = attendance.Employee?.EmployeeCode ?? "",
            EmployeeName = attendance.Employee != null ? $"{attendance.Employee.FirstName} {attendance.Employee.LastName}" : "",
            DepartmentName = attendance.Employee?.Department?.Name,
            Date = attendance.Date,
            CheckInTime = attendance.CheckInTime,
            CheckOutTime = attendance.CheckOutTime,
            WorkingHours = attendance.WorkingHours,
            OvertimeHours = attendance.OvertimeHours,
            OvertimeRate = attendance.OvertimeRate,
            Status = attendance.Status,
            StatusDisplay = attendance.Status.ToString(),
            LateArrivalMinutes = attendance.LateArrivalMinutes,
            EarlyDepartureMinutes = attendance.EarlyDepartureMinutes,
            Remarks = attendance.Remarks,
            IsRegularized = attendance.IsRegularized,
            RegularizedBy = attendance.RegularizedBy,
            RegularizedAt = attendance.RegularizedAt,
            IsSunday = attendance.IsSunday,
            IsPublicHoliday = attendance.IsPublicHoliday,
            CreatedAt = attendance.CreatedAt,
            UpdatedAt = attendance.UpdatedAt,
            CreatedBy = attendance.CreatedBy,
            UpdatedBy = attendance.UpdatedBy
        };
    }
}
