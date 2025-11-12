using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security;

namespace HRMS.Infrastructure.Services;

public class LeaveService : ILeaveService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<LeaveService> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;

    // Role constants for authorization
    private const string RoleAdmin = "Admin";
    private const string RoleHR = "HR";
    private const string RoleManager = "Manager";
    private const string RoleEmployee = "Employee";

    public LeaveService(
        TenantDbContext context,
        ILogger<LeaveService> logger,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _logger = logger;
        _fileStorageService = fileStorageService;
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
    /// Checks if the current user can approve a leave request
    /// SECURITY:
    /// - Admin/HR can approve all leave requests
    /// - Manager can approve leave requests for their direct reports only
    /// </summary>
    private async Task<bool> CanApproveLeaveAsync(Guid leaveRequestId)
    {
        // Admin and HR can approve all leave requests
        if (IsHROrAdmin())
            return true;

        // Check if user is a manager
        if (!_currentUserService.HasRole(RoleManager))
            return false;

        // Get the leave request
        var leaveRequest = await _context.LeaveApplications
            .AsNoTracking()
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == leaveRequestId);

        if (leaveRequest == null)
            return false;

        // Get current user's employee record
        var currentEmployee = await GetEmployeeByUserIdAsync(_currentUserService.UserId ?? "");
        if (currentEmployee == null)
            return false;

        // Manager can only approve for direct reports
        return leaveRequest.Employee?.ManagerId == currentEmployee.Id;
    }

    /// <summary>
    /// Checks if the current user can view a leave request for a specific employee
    /// SECURITY:
    /// - Admin/HR can view all leave requests
    /// - Manager can view leave requests for their direct reports
    /// - Employee can view their own leave requests
    /// </summary>
    private async Task<bool> CanViewLeaveRequestAsync(Guid employeeId)
    {
        // Admin and HR can view all leave requests
        if (IsHROrAdmin())
            return true;

        // Check if viewing self
        var currentEmployee = await GetEmployeeByUserIdAsync(_currentUserService.UserId ?? "");
        if (currentEmployee?.Id == employeeId)
            return true;

        // Manager can view their direct reports
        if (_currentUserService.HasRole(RoleManager))
        {
            var targetEmployee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (targetEmployee?.ManagerId == currentEmployee?.Id)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets employee record by user ID (from authentication)
    /// </summary>
    private async Task<Employee?> GetEmployeeByUserIdAsync(string userId)
    {
        // Map by email since that's the common link
        return await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Email == _currentUserService.Email && e.IsActive);
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
    /// Logs leave approval/rejection actions for audit trail
    /// </summary>
    private void LogLeaveAction(string action, Guid leaveRequestId, Guid employeeId, string? comments = null)
    {
        _logger.LogInformation(
            "LEAVE ACTION: User {UserId} performed {Action} on leave request {LeaveRequestId} for employee {EmployeeId}. Comments: {Comments}",
            _currentUserService.UserId,
            action,
            leaveRequestId,
            employeeId,
            comments);
    }

    /// <summary>
    /// Logs leave balance modifications for audit trail
    /// </summary>
    private void LogLeaveBalanceModification(string operation, Guid employeeId, Guid leaveTypeId, decimal days, string reason)
    {
        _logger.LogInformation(
            "LEAVE BALANCE MODIFICATION: User {UserId} performed {Operation} on employee {EmployeeId} leave type {LeaveTypeId}. Days: {Days}. Reason: {Reason}",
            _currentUserService.UserId,
            operation,
            employeeId,
            leaveTypeId,
            days,
            reason);
    }

    #endregion

    #region Leave Application

    public async Task<LeaveApplicationDto> ApplyForLeaveAsync(Guid employeeId, CreateLeaveApplicationRequest request)
    {
        // Validate the application
        var validationError = await ValidateLeaveApplicationAsync(employeeId, request);
        if (validationError != null)
            throw new InvalidOperationException(validationError);

        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        var leaveType = await _context.LeaveTypes.FindAsync(request.LeaveTypeId);
        if (leaveType == null)
            throw new InvalidOperationException("Leave type not found");

        // Calculate working days
        var workingDays = await CalculateWorkingDaysAsync(request.StartDate, request.EndDate);

        // Generate application number
        var applicationNumber = await GenerateApplicationNumberAsync(request.StartDate.Year);

        // Create leave application
        var leaveApplication = new LeaveApplication
        {
            Id = Guid.NewGuid(),
            ApplicationNumber = applicationNumber,
            EmployeeId = employeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = workingDays,
            CalculationType = request.CalculationType,
            Reason = request.Reason,
            ContactNumber = request.ContactNumber,
            ContactAddress = request.ContactAddress,
            Status = LeaveStatus.PendingApproval,
            AppliedDate = DateTime.UtcNow,
            RequiresHRApproval = workingDays > 5, // HR approval for leaves > 5 days
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Handle file attachment
        if (!string.IsNullOrEmpty(request.AttachmentBase64))
        {
            leaveApplication.AttachmentPath = await SaveAttachmentAsync(
                request.AttachmentBase64,
                request.AttachmentFileName ?? "attachment",
                employeeId);
        }

        _context.LeaveApplications.Add(leaveApplication);

        // Update leave balance - add to pending
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId &&
                                      b.LeaveTypeId == request.LeaveTypeId &&
                                      b.Year == request.StartDate.Year);

        if (balance != null)
        {
            balance.PendingDays += workingDays;
            balance.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Leave application {ApplicationNumber} created for employee {EmployeeId}",
            applicationNumber, employeeId);

        return await GetLeaveApplicationByIdAsync(leaveApplication.Id);
    }

    /// <summary>
    /// Gets leave application by ID with role-based authorization
    /// SECURITY:
    /// - Admin/HR can view all leave applications
    /// - Manager can view leave applications for their direct reports
    /// - Employee can view their own leave applications
    /// </summary>
    public async Task<LeaveApplicationDto> GetLeaveApplicationByIdAsync(Guid id)
    {
        var application = await _context.LeaveApplications
            .AsNoTracking()
            .Include(l => l.Employee)
                .ThenInclude(e => e!.Department)
            .Include(l => l.LeaveType)
            .Include(l => l.Approver)
            .Include(l => l.Rejector)
            .Include(l => l.Approvals)
                .ThenInclude(a => a.Approver)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (application == null)
        {
            LogAuthorizationFailure("GetLeaveApplicationById", id);
            throw new InvalidOperationException("Leave application not found");
        }

        // AUTHORIZATION CHECK: Verify user can view this leave application
        var canView = await CanViewLeaveRequestAsync(application.EmployeeId);
        if (!canView)
        {
            LogAuthorizationFailure("GetLeaveApplicationById", id);
            throw new UnauthorizedAccessException("You do not have permission to view this leave application");
        }

        return MapToLeaveApplicationDto(application);
    }

    /// <summary>
    /// Gets leave applications for an employee with role-based authorization
    /// SECURITY:
    /// - Admin/HR can view all employee leave applications
    /// - Manager can view leave applications for their direct reports
    /// - Employee can view their own leave applications
    /// </summary>
    public async Task<List<LeaveApplicationListDto>> GetMyLeavesAsync(Guid employeeId, int? year = null)
    {
        // AUTHORIZATION CHECK: Verify user can view this employee's leave applications
        var canView = await CanViewLeaveRequestAsync(employeeId);
        if (!canView)
        {
            LogAuthorizationFailure("GetMyLeaves", employeeId);
            throw new UnauthorizedAccessException("You do not have permission to view this employee's leave applications");
        }

        var query = _context.LeaveApplications
            .AsNoTracking()
            .Include(l => l.LeaveType)
            .Include(l => l.Employee)
            .Where(l => l.EmployeeId == employeeId && !l.IsDeleted);

        if (year.HasValue)
        {
            query = query.Where(l => l.StartDate.Year == year.Value);
        }

        var applications = await query
            .OrderByDescending(l => l.AppliedDate)
            .ToListAsync();

        return applications.Select(MapToLeaveApplicationListDto).ToList();
    }

    /// <summary>
    /// Cancels a leave application with role-based authorization
    /// SECURITY:
    /// - Admin/HR can cancel any leave application
    /// - Employee can only cancel their own leave applications
    /// - Logs all cancellation actions for audit trail
    /// </summary>
    public async Task<LeaveApplicationDto> CancelLeaveAsync(Guid leaveId, Guid employeeId, CancelLeaveRequest request)
    {
        var application = await _context.LeaveApplications
            .Include(l => l.LeaveType)
            .FirstOrDefaultAsync(l => l.Id == leaveId && l.EmployeeId == employeeId && !l.IsDeleted);

        if (application == null)
        {
            LogAuthorizationFailure("CancelLeave", leaveId);
            throw new InvalidOperationException("Leave application not found");
        }

        // AUTHORIZATION CHECK: Verify user can cancel this leave application
        var canView = await CanViewLeaveRequestAsync(employeeId);
        if (!canView)
        {
            LogAuthorizationFailure("CancelLeave", leaveId);
            throw new UnauthorizedAccessException("You do not have permission to cancel this leave application");
        }

        if (application.Status != LeaveStatus.PendingApproval && application.Status != LeaveStatus.Approved)
            throw new InvalidOperationException("Only pending or approved leaves can be cancelled");

        // If approved, update balance
        if (application.Status == LeaveStatus.Approved)
        {
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == employeeId &&
                                          b.LeaveTypeId == application.LeaveTypeId &&
                                          b.Year == application.StartDate.Year);

            if (balance != null)
            {
                balance.UsedDays -= application.TotalDays;
                balance.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (application.Status == LeaveStatus.PendingApproval)
        {
            // Remove from pending
            var balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == employeeId &&
                                          b.LeaveTypeId == application.LeaveTypeId &&
                                          b.Year == application.StartDate.Year);

            if (balance != null)
            {
                balance.PendingDays -= application.TotalDays;
                balance.UpdatedAt = DateTime.UtcNow;
            }
        }

        application.Status = LeaveStatus.Cancelled;
        application.CancelledDate = DateTime.UtcNow;
        application.CancelledBy = employeeId;
        application.CancellationReason = request.CancellationReason;
        application.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // AUDIT LOG: Log leave cancellation action
        LogLeaveAction("CancelLeave", leaveId, employeeId, request.CancellationReason);

        return await GetLeaveApplicationByIdAsync(leaveId);
    }

    #endregion

    #region Approval Workflow

    /// <summary>
    /// Approves a leave application with role-based authorization
    /// SECURITY:
    /// - Admin/HR can approve all leave requests
    /// - Manager can approve leave requests for their direct reports only
    /// - Logs all approval actions for audit trail
    /// </summary>
    public async Task<LeaveApplicationDto> ApproveLeaveAsync(Guid leaveId, Guid approverId, ApproveLeaveRequest request)
    {
        var application = await _context.LeaveApplications
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .FirstOrDefaultAsync(l => l.Id == leaveId && !l.IsDeleted);

        if (application == null)
        {
            LogAuthorizationFailure("ApproveLeave", leaveId);
            throw new InvalidOperationException("Leave application not found");
        }

        if (application.Status != LeaveStatus.PendingApproval)
            throw new InvalidOperationException("Leave is not in pending status");

        // AUTHORIZATION CHECK: Verify user can approve this leave request
        var canApprove = await CanApproveLeaveAsync(leaveId);
        if (!canApprove)
        {
            LogAuthorizationFailure("ApproveLeave", leaveId);
            throw new UnauthorizedAccessException("You do not have permission to approve this leave request. Only HR, Admin, or the employee's direct manager can approve leave requests.");
        }

        // Update application status
        application.Status = LeaveStatus.Approved;
        application.ApprovedDate = DateTime.UtcNow;
        application.ApprovedBy = approverId;
        application.ApproverComments = request.Comments;
        application.UpdatedAt = DateTime.UtcNow;

        // Update leave balance
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == application.EmployeeId &&
                                      b.LeaveTypeId == application.LeaveTypeId &&
                                      b.Year == application.StartDate.Year);

        if (balance != null)
        {
            balance.PendingDays -= application.TotalDays;
            balance.UsedDays += application.TotalDays;
            balance.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // AUDIT LOG: Log leave approval action
        LogLeaveAction("ApproveLeave", leaveId, application.EmployeeId, request.Comments);

        return await GetLeaveApplicationByIdAsync(leaveId);
    }

    /// <summary>
    /// Rejects a leave application with role-based authorization
    /// SECURITY:
    /// - Admin/HR can reject all leave requests
    /// - Manager can reject leave requests for their direct reports only
    /// - Logs all rejection actions for audit trail
    /// </summary>
    public async Task<LeaveApplicationDto> RejectLeaveAsync(Guid leaveId, Guid approverId, RejectLeaveRequest request)
    {
        var application = await _context.LeaveApplications
            .FirstOrDefaultAsync(l => l.Id == leaveId && !l.IsDeleted);

        if (application == null)
        {
            LogAuthorizationFailure("RejectLeave", leaveId);
            throw new InvalidOperationException("Leave application not found");
        }

        if (application.Status != LeaveStatus.PendingApproval)
            throw new InvalidOperationException("Leave is not in pending status");

        // AUTHORIZATION CHECK: Verify user can reject this leave request
        var canApprove = await CanApproveLeaveAsync(leaveId);
        if (!canApprove)
        {
            LogAuthorizationFailure("RejectLeave", leaveId);
            throw new UnauthorizedAccessException("You do not have permission to reject this leave request. Only HR, Admin, or the employee's direct manager can reject leave requests.");
        }

        // Update application status
        application.Status = LeaveStatus.Rejected;
        application.RejectedDate = DateTime.UtcNow;
        application.RejectedBy = approverId;
        application.RejectionReason = request.RejectionReason;
        application.UpdatedAt = DateTime.UtcNow;

        // Remove from pending balance
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == application.EmployeeId &&
                                      b.LeaveTypeId == application.LeaveTypeId &&
                                      b.Year == application.StartDate.Year);

        if (balance != null)
        {
            balance.PendingDays -= application.TotalDays;
            balance.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // AUDIT LOG: Log leave rejection action
        LogLeaveAction("RejectLeave", leaveId, application.EmployeeId, request.RejectionReason);

        return await GetLeaveApplicationByIdAsync(leaveId);
    }

    /// <summary>
    /// Gets pending leave approvals for a manager with role-based authorization
    /// SECURITY:
    /// - Admin/HR can view all pending approvals
    /// - Manager can only view pending approvals for their direct reports
    /// </summary>
    public async Task<List<LeaveApplicationListDto>> GetPendingApprovalsAsync(Guid managerId)
    {
        // Get current user's employee record
        var currentEmployee = await GetEmployeeByUserIdAsync(_currentUserService.UserId ?? "");

        // AUTHORIZATION CHECK: Verify the managerId matches current user or user is HR/Admin
        if (!IsHROrAdmin() && currentEmployee?.Id != managerId)
        {
            LogAuthorizationFailure("GetPendingApprovals", managerId);
            throw new UnauthorizedAccessException("You can only view pending approvals for your own team");
        }

        // Get all employees reporting to this manager
        var teamMemberIds = await _context.Employees
            .AsNoTracking()
            .Where(e => e.ManagerId == managerId && !e.IsDeleted)
            .Select(e => e.Id)
            .ToListAsync();

        var applications = await _context.LeaveApplications
            .AsNoTracking()
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .Where(l => teamMemberIds.Contains(l.EmployeeId) &&
                        l.Status == LeaveStatus.PendingApproval &&
                        !l.IsDeleted)
            .OrderBy(l => l.AppliedDate)
            .ToListAsync();

        return applications.Select(MapToLeaveApplicationListDto).ToList();
    }

    /// <summary>
    /// Gets team leave applications for a manager with role-based authorization
    /// SECURITY:
    /// - Admin/HR can view all team leaves
    /// - Manager can only view leaves for their direct reports
    /// </summary>
    public async Task<List<LeaveApplicationListDto>> GetTeamLeavesAsync(Guid managerId, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Get current user's employee record
        var currentEmployee = await GetEmployeeByUserIdAsync(_currentUserService.UserId ?? "");

        // AUTHORIZATION CHECK: Verify the managerId matches current user or user is HR/Admin
        if (!IsHROrAdmin() && currentEmployee?.Id != managerId)
        {
            LogAuthorizationFailure("GetTeamLeaves", managerId);
            throw new UnauthorizedAccessException("You can only view leave applications for your own team");
        }

        // Get all employees reporting to this manager
        var teamMemberIds = await _context.Employees
            .AsNoTracking()
            .Where(e => e.ManagerId == managerId && !e.IsDeleted)
            .Select(e => e.Id)
            .ToListAsync();

        var query = _context.LeaveApplications
            .AsNoTracking()
            .Include(l => l.Employee)
            .Include(l => l.LeaveType)
            .Where(l => teamMemberIds.Contains(l.EmployeeId) && !l.IsDeleted);

        if (startDate.HasValue)
            query = query.Where(l => l.EndDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => l.StartDate <= endDate.Value);

        var applications = await query
            .OrderBy(l => l.StartDate)
            .ToListAsync();

        return applications.Select(MapToLeaveApplicationListDto).ToList();
    }

    #endregion

    #region Leave Balance

    /// <summary>
    /// Gets leave balance for an employee with role-based authorization
    /// SECURITY:
    /// - Admin/HR can view all leave balances
    /// - Manager can view leave balances for their direct reports
    /// - Employee can view their own leave balance
    /// </summary>
    public async Task<List<LeaveBalanceDto>> GetLeaveBalanceAsync(Guid employeeId, int? year = null)
    {
        // AUTHORIZATION CHECK: Verify user can view this employee's leave balance
        var canView = await CanViewLeaveRequestAsync(employeeId);
        if (!canView)
        {
            LogAuthorizationFailure("GetLeaveBalance", employeeId);
            throw new UnauthorizedAccessException("You do not have permission to view this employee's leave balance");
        }

        var currentYear = year ?? DateTime.UtcNow.Year;

        var balances = await _context.LeaveBalances
            .AsNoTracking()
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId && b.Year == currentYear && !b.IsDeleted)
            .OrderBy(b => b.LeaveType!.TypeCode)
            .ToListAsync();

        // If no balances exist, initialize them
        if (!balances.Any())
        {
            await InitializeLeaveBalanceAsync(employeeId, currentYear);
            balances = await _context.LeaveBalances
                .AsNoTracking()
                .Include(b => b.LeaveType)
                .Where(b => b.EmployeeId == employeeId && b.Year == currentYear && !b.IsDeleted)
                .OrderBy(b => b.LeaveType!.TypeCode)
                .ToListAsync();
        }

        return balances.Select(b => new LeaveBalanceDto
        {
            Id = b.Id,
            LeaveTypeId = b.LeaveTypeId,
            LeaveTypeName = b.LeaveType?.Name ?? "",
            Year = b.Year,
            TotalEntitlement = b.TotalEntitlement,
            UsedDays = b.UsedDays,
            PendingDays = b.PendingDays,
            AvailableDays = b.AvailableDays,
            CarriedForward = b.CarriedForward,
            ExpiryDate = b.ExpiryDate
        }).ToList();
    }

    public async Task InitializeLeaveBalanceAsync(Guid employeeId, int year)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        var leaveTypes = await _context.LeaveTypes
            .Where(lt => lt.IsActive && !lt.IsDeleted)
            .ToListAsync();

        foreach (var leaveType in leaveTypes)
        {
            var existingBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == employeeId &&
                                          b.LeaveTypeId == leaveType.Id &&
                                          b.Year == year);

            if (existingBalance == null)
            {
                var entitlement = await CalculateProRatedLeaveEntitlementAsync(
                    employeeId, leaveType.Id, employee.JoiningDate, year);

                var balance = new LeaveBalance
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employeeId,
                    LeaveTypeId = leaveType.Id,
                    Year = year,
                    TotalEntitlement = entitlement,
                    UsedDays = 0,
                    PendingDays = 0,
                    CarriedForward = 0,
                    Accrued = entitlement,
                    LastAccrualDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.LeaveBalances.Add(balance);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Leave balance initialized for employee {EmployeeId} for year {Year}",
            employeeId, year);
    }

    public async Task AccrueMonthlyLeaveAsync(Guid employeeId, int year, int month)
    {
        var balances = await _context.LeaveBalances
            .Where(b => b.EmployeeId == employeeId && b.Year == year && !b.IsDeleted)
            .ToListAsync();

        foreach (var balance in balances)
        {
            // Accrue monthly (simplified - 22 days / 12 months for annual leave)
            var monthlyAccrual = balance.TotalEntitlement / 12;
            balance.Accrued += monthlyAccrual;
            balance.LastAccrualDate = new DateTime(year, month, 1);
            balance.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Monthly leave accrued for employee {EmployeeId} for {Month}/{Year}",
            employeeId, month, year);
    }

    /// <summary>
    /// Updates leave balance for an employee with authorization and audit logging
    /// SECURITY:
    /// - Only Admin/HR can update leave balances
    /// - All modifications are logged for audit trail
    /// </summary>
    public async Task UpdateLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year, decimal days, string reason)
    {
        // AUTHORIZATION CHECK: Only HR/Admin can update leave balances
        if (!IsHROrAdmin())
        {
            LogAuthorizationFailure("UpdateLeaveBalance", employeeId);
            throw new UnauthorizedAccessException("Only HR and Admin roles can update leave balances");
        }

        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId &&
                                      b.LeaveTypeId == leaveTypeId &&
                                      b.Year == year);

        if (balance == null)
        {
            await InitializeLeaveBalanceAsync(employeeId, year);
            balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == employeeId &&
                                          b.LeaveTypeId == leaveTypeId &&
                                          b.Year == year);
        }

        if (balance != null)
        {
            balance.TotalEntitlement += days;
            balance.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // AUDIT LOG: Log leave balance modification
            LogLeaveBalanceModification("UpdateLeaveBalance", employeeId, leaveTypeId, days, reason);
        }
    }

    #endregion

    #region Leave Calendar

    public async Task<List<LeaveCalendarDto>> GetLeaveCalendarAsync(DateTime startDate, DateTime endDate, Guid? departmentId = null)
    {
        var query = _context.LeaveApplications
            .AsNoTracking()
            .Include(l => l.Employee)
                .ThenInclude(e => e!.Department)
            .Include(l => l.LeaveType)
            .Where(l => l.Status == LeaveStatus.Approved &&
                        l.StartDate <= endDate &&
                        l.EndDate >= startDate &&
                        !l.IsDeleted);

        if (departmentId.HasValue)
        {
            query = query.Where(l => l.Employee!.DepartmentId == departmentId.Value);
        }

        var applications = await query.ToListAsync();

        return applications.Select(l => new LeaveCalendarDto
        {
            LeaveApplicationId = l.Id,
            EmployeeId = l.EmployeeId,
            EmployeeCode = l.Employee?.EmployeeCode ?? "",
            EmployeeName = l.Employee != null ? $"{l.Employee.FirstName} {l.Employee.LastName}" : "",
            LeaveTypeName = l.LeaveType?.Name ?? "",
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            TotalDays = l.TotalDays,
            Status = l.Status
        }).ToList();
    }

    public async Task<List<LeaveCalendarDto>> GetDepartmentLeaveCalendarAsync(Guid departmentId, int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await GetLeaveCalendarAsync(startDate, endDate, departmentId);
    }

    #endregion

    #region Leave Types

    public async Task<List<LeaveTypeDto>> GetLeaveTypesAsync()
    {
        var leaveTypes = await _context.LeaveTypes
            .AsNoTracking()
            .Where(lt => lt.IsActive && !lt.IsDeleted)
            .OrderBy(lt => lt.TypeCode)
            .ToListAsync();

        return leaveTypes.Select(lt => new LeaveTypeDto
        {
            Id = lt.Id,
            TypeCode = lt.TypeCode,
            Name = lt.Name,
            Description = lt.Description,
            DefaultEntitlement = lt.DefaultEntitlement,
            IsPaid = lt.IsPaid,
            CanCarryForward = lt.CanCarryForward,
            MaxCarryForwardDays = lt.MaxCarryForwardDays,
            RequiresDocumentation = lt.RequiresDocumentation,
            IsActive = lt.IsActive
        }).ToList();
    }

    public async Task<LeaveTypeDto> GetLeaveTypeByIdAsync(Guid id)
    {
        var leaveType = await _context.LeaveTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(lt => lt.Id == id && !lt.IsDeleted);

        if (leaveType == null)
            throw new InvalidOperationException("Leave type not found");

        return new LeaveTypeDto
        {
            Id = leaveType.Id,
            TypeCode = leaveType.TypeCode,
            Name = leaveType.Name,
            Description = leaveType.Description,
            DefaultEntitlement = leaveType.DefaultEntitlement,
            IsPaid = leaveType.IsPaid,
            CanCarryForward = leaveType.CanCarryForward,
            MaxCarryForwardDays = leaveType.MaxCarryForwardDays,
            RequiresDocumentation = leaveType.RequiresDocumentation,
            IsActive = leaveType.IsActive
        };
    }

    #endregion

    #region Public Holidays

    public async Task<List<PublicHolidayDto>> GetPublicHolidaysAsync(int year)
    {
        var holidays = await _context.PublicHolidays
            .AsNoTracking()
            .Where(h => h.Year == year && h.IsActive && !h.IsDeleted)
            .OrderBy(h => h.Date)
            .ToListAsync();

        return holidays.Select(h => new PublicHolidayDto
        {
            Id = h.Id,
            Name = h.Name,
            Date = h.Date,
            Year = h.Year,
            Type = h.Type,
            Description = h.Description,
            IsActive = h.IsActive
        }).ToList();
    }

    public async Task<bool> IsPublicHolidayAsync(DateTime date)
    {
        return await _context.PublicHolidays
            .AnyAsync(h => h.Date.Date == date.Date && h.IsActive && !h.IsDeleted);
    }

    #endregion

    #region Calculations

    public async Task<decimal> CalculateWorkingDaysAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new InvalidOperationException("Start date cannot be after end date");

        var holidays = await _context.PublicHolidays
            .Where(h => h.Date >= startDate && h.Date <= endDate && h.IsActive && !h.IsDeleted)
            .Select(h => h.Date.Date)
            .ToListAsync();

        decimal workingDays = 0;
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            // Skip weekends (Saturday = 6, Sunday = 0)
            if (currentDate.DayOfWeek != DayOfWeek.Saturday &&
                currentDate.DayOfWeek != DayOfWeek.Sunday &&
                !holidays.Contains(currentDate))
            {
                workingDays++;
            }
            currentDate = currentDate.AddDays(1);
        }

        return workingDays;
    }

    public async Task<decimal> CalculateProRatedLeaveEntitlementAsync(Guid employeeId, Guid leaveTypeId, DateTime joiningDate, int year)
    {
        var leaveType = await _context.LeaveTypes.FindAsync(leaveTypeId);
        if (leaveType == null)
            return 0;

        var startOfYear = new DateTime(year, 1, 1);
        var endOfYear = new DateTime(year, 12, 31);

        // If employee joined before this year, give full entitlement
        if (joiningDate.Year < year)
            return leaveType.DefaultEntitlement;

        // If employee joins in this year, pro-rate based on months remaining
        if (joiningDate.Year == year)
        {
            var monthsRemaining = 12 - joiningDate.Month + 1;
            var proRatedEntitlement = (leaveType.DefaultEntitlement / 12) * monthsRemaining;
            return Math.Round(proRatedEntitlement, 2);
        }

        // If employee joins after this year, no entitlement
        return 0;
    }

    #endregion

    #region Leave Encashment

    public async Task<LeaveEncashmentDto> CalculateLeaveEncashmentAsync(Guid employeeId, DateTime lastWorkingDay)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        var year = lastWorkingDay.Year;
        var balances = await _context.LeaveBalances
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId && b.Year == year && !b.IsDeleted)
            .ToListAsync();

        var annualLeaveBalance = balances.FirstOrDefault(b => b.LeaveType!.TypeCode == LeaveTypeEnum.AnnualLeave);
        var sickLeaveBalance = balances.FirstOrDefault(b => b.LeaveType!.TypeCode == LeaveTypeEnum.SickLeave);

        decimal unusedAnnualDays = annualLeaveBalance?.AvailableDays ?? 0;
        decimal unusedSickDays = sickLeaveBalance?.AvailableDays ?? 0;

        // Only annual leave is typically encashable in Mauritius
        decimal totalEncashableDays = unusedAnnualDays;

        // Calculate daily salary (assuming monthly salary)
        decimal dailySalary = employee.BasicSalary / 22; // 22 working days per month
        decimal totalAmount = totalEncashableDays * dailySalary;

        return new LeaveEncashmentDto
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            CalculationDate = DateTime.UtcNow,
            LastWorkingDay = lastWorkingDay,
            UnusedAnnualLeaveDays = unusedAnnualDays,
            UnusedSickLeaveDays = unusedSickDays,
            TotalEncashableDays = totalEncashableDays,
            DailySalary = dailySalary,
            TotalEncashmentAmount = totalAmount,
            IsPaid = false,
            PaidDate = null
        };
    }

    public async Task<LeaveEncashmentDto> ProcessLeaveEncashmentAsync(Guid employeeId, DateTime lastWorkingDay)
    {
        var calculation = await CalculateLeaveEncashmentAsync(employeeId, lastWorkingDay);

        var encashment = new LeaveEncashment
        {
            Id = calculation.Id,
            EmployeeId = employeeId,
            CalculationDate = calculation.CalculationDate,
            LastWorkingDay = lastWorkingDay,
            UnusedAnnualLeaveDays = calculation.UnusedAnnualLeaveDays,
            UnusedSickLeaveDays = calculation.UnusedSickLeaveDays,
            TotalEncashableDays = calculation.TotalEncashableDays,
            DailySalary = calculation.DailySalary,
            TotalEncashmentAmount = calculation.TotalEncashmentAmount,
            IsPaid = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.LeaveEncashments.Add(encashment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Leave encashment processed for employee {EmployeeId}. Amount: {Amount}",
            employeeId, encashment.TotalEncashmentAmount);

        return calculation;
    }

    #endregion

    #region Validation

    public async Task<string?> ValidateLeaveApplicationAsync(Guid employeeId, CreateLeaveApplicationRequest request)
    {
        var errors = new List<string>();

        // Cannot apply for past dates
        if (request.StartDate.Date < DateTime.UtcNow.Date)
            errors.Add("Cannot apply for leave in the past");

        // End date must be >= start date
        if (request.EndDate < request.StartDate)
            errors.Add("End date cannot be before start date");

        // Check leave type
        var leaveType = await _context.LeaveTypes.FindAsync(request.LeaveTypeId);
        if (leaveType == null)
        {
            errors.Add("Invalid leave type");
            return string.Join("; ", errors);
        }

        // Check leave balance
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId &&
                                      b.LeaveTypeId == request.LeaveTypeId &&
                                      b.Year == request.StartDate.Year);

        if (balance == null)
        {
            // Initialize balance if not exists
            await InitializeLeaveBalanceAsync(employeeId, request.StartDate.Year);
            balance = await _context.LeaveBalances
                .FirstOrDefaultAsync(b => b.EmployeeId == employeeId &&
                                          b.LeaveTypeId == request.LeaveTypeId &&
                                          b.Year == request.StartDate.Year);
        }

        var workingDays = await CalculateWorkingDaysAsync(request.StartDate, request.EndDate);

        if (balance!.AvailableDays < workingDays)
            errors.Add($"Insufficient leave balance. Available: {balance.AvailableDays} days, Requested: {workingDays} days");

        // Check overlapping leaves
        var hasOverlap = await HasOverlappingLeaveAsync(employeeId, request.StartDate, request.EndDate);
        if (hasOverlap)
            errors.Add("You have an overlapping leave application");

        // Sick leave >3 days requires medical certificate
        if (leaveType.TypeCode == LeaveTypeEnum.SickLeave &&
            workingDays > 3 &&
            string.IsNullOrEmpty(request.AttachmentBase64))
            errors.Add("Medical certificate required for sick leave exceeding 3 days");

        return errors.Any() ? string.Join("; ", errors) : null;
    }

    public async Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeLeaveId = null)
    {
        var query = _context.LeaveApplications
            .Where(l => l.EmployeeId == employeeId &&
                        !l.IsDeleted &&
                        (l.Status == LeaveStatus.PendingApproval || l.Status == LeaveStatus.Approved) &&
                        ((l.StartDate <= endDate && l.EndDate >= startDate)));

        if (excludeLeaveId.HasValue)
        {
            query = query.Where(l => l.Id != excludeLeaveId.Value);
        }

        return await query.AnyAsync();
    }

    #endregion

    #region Private Helper Methods

    private async Task<string> GenerateApplicationNumberAsync(int year)
    {
        var count = await _context.LeaveApplications
            .Where(l => l.ApplicationNumber.StartsWith($"LEV-{year}-"))
            .CountAsync();

        return $"LEV-{year}-{(count + 1):D4}";
    }

    private async Task<string> SaveAttachmentAsync(string base64Content, string fileName, Guid employeeId)
    {
        // PRODUCTION-READY: Use Google Cloud Storage for file persistence
        // Files are stored in cloud bucket and survive container restarts/scaling

        try
        {
            // Convert base64 to stream
            var bytes = Convert.FromBase64String(base64Content);
            using var memoryStream = new MemoryStream(bytes);

            // Upload to cloud storage
            // Folder structure: leave-attachments/{employeeId}/
            var folder = $"leave-attachments/{employeeId}";

            var cloudStoragePath = await _fileStorageService.UploadFileAsync(
                fileStream: memoryStream,
                fileName: fileName,
                folder: folder);

            _logger.LogInformation("Leave attachment uploaded to cloud storage: {Path}", cloudStoragePath);

            return cloudStoragePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload leave attachment for employee {EmployeeId}", employeeId);
            throw new InvalidOperationException("Failed to upload leave attachment. Please try again.", ex);
        }
    }

    private LeaveApplicationDto MapToLeaveApplicationDto(LeaveApplication application)
    {
        return new LeaveApplicationDto
        {
            Id = application.Id,
            ApplicationNumber = application.ApplicationNumber,
            EmployeeId = application.EmployeeId,
            EmployeeCode = application.Employee?.EmployeeCode ?? "",
            EmployeeName = application.Employee != null
                ? $"{application.Employee.FirstName} {application.Employee.LastName}"
                : "",
            Department = application.Employee?.Department?.Name ?? "",
            LeaveTypeId = application.LeaveTypeId,
            LeaveTypeName = application.LeaveType?.Name ?? "",
            StartDate = application.StartDate,
            EndDate = application.EndDate,
            TotalDays = application.TotalDays,
            Reason = application.Reason,
            Status = application.Status,
            StatusText = application.Status.ToString(),
            AppliedDate = application.AppliedDate ?? DateTime.UtcNow,
            ApprovedDate = application.ApprovedDate,
            ApprovedByName = application.Approver != null
                ? $"{application.Approver.FirstName} {application.Approver.LastName}"
                : null,
            ApproverComments = application.ApproverComments,
            RejectedDate = application.RejectedDate,
            RejectedByName = application.Rejector != null
                ? $"{application.Rejector.FirstName} {application.Rejector.LastName}"
                : null,
            RejectionReason = application.RejectionReason,
            ContactNumber = application.ContactNumber,
            AttachmentPath = application.AttachmentPath,
            Approvals = application.Approvals.Select(a => new LeaveApprovalDto
            {
                Id = a.Id,
                ApprovalLevel = a.ApprovalLevel,
                ApproverRole = a.ApproverRole,
                ApproverName = a.Approver != null
                    ? $"{a.Approver.FirstName} {a.Approver.LastName}"
                    : null,
                Status = a.Status,
                ActionDate = a.ActionDate,
                Comments = a.Comments
            }).ToList()
        };
    }

    private LeaveApplicationListDto MapToLeaveApplicationListDto(LeaveApplication application)
    {
        return new LeaveApplicationListDto
        {
            Id = application.Id,
            ApplicationNumber = application.ApplicationNumber,
            EmployeeCode = application.Employee?.EmployeeCode ?? "",
            EmployeeName = application.Employee != null
                ? $"{application.Employee.FirstName} {application.Employee.LastName}"
                : "",
            LeaveTypeName = application.LeaveType?.Name ?? "",
            StartDate = application.StartDate,
            EndDate = application.EndDate,
            TotalDays = application.TotalDays,
            Status = application.Status,
            StatusText = application.Status.ToString(),
            AppliedDate = application.AppliedDate ?? DateTime.UtcNow
        };
    }

    #endregion
}
