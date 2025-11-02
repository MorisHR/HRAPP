using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces;

public interface ILeaveService
{
    // Leave Application
    Task<LeaveApplicationDto> ApplyForLeaveAsync(Guid employeeId, CreateLeaveApplicationRequest request);
    Task<LeaveApplicationDto> GetLeaveApplicationByIdAsync(Guid id);
    Task<List<LeaveApplicationListDto>> GetMyLeavesAsync(Guid employeeId, int? year = null);
    Task<LeaveApplicationDto> CancelLeaveAsync(Guid leaveId, Guid employeeId, CancelLeaveRequest request);

    // Approval Workflow
    Task<LeaveApplicationDto> ApproveLeaveAsync(Guid leaveId, Guid approverId, ApproveLeaveRequest request);
    Task<LeaveApplicationDto> RejectLeaveAsync(Guid leaveId, Guid approverId, RejectLeaveRequest request);
    Task<List<LeaveApplicationListDto>> GetPendingApprovalsAsync(Guid managerId);
    Task<List<LeaveApplicationListDto>> GetTeamLeavesAsync(Guid managerId, DateTime? startDate = null, DateTime? endDate = null);

    // Leave Balance
    Task<List<LeaveBalanceDto>> GetLeaveBalanceAsync(Guid employeeId, int? year = null);
    Task InitializeLeaveBalanceAsync(Guid employeeId, int year);
    Task AccrueMonthlyLeaveAsync(Guid employeeId, int year, int month);
    Task UpdateLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year, decimal days, string reason);

    // Leave Calendar
    Task<List<LeaveCalendarDto>> GetLeaveCalendarAsync(DateTime startDate, DateTime endDate, Guid? departmentId = null);
    Task<List<LeaveCalendarDto>> GetDepartmentLeaveCalendarAsync(Guid departmentId, int year, int month);

    // Leave Types
    Task<List<LeaveTypeDto>> GetLeaveTypesAsync();
    Task<LeaveTypeDto> GetLeaveTypeByIdAsync(Guid id);

    // Public Holidays
    Task<List<PublicHolidayDto>> GetPublicHolidaysAsync(int year);
    Task<bool> IsPublicHolidayAsync(DateTime date);

    // Calculations
    Task<decimal> CalculateWorkingDaysAsync(DateTime startDate, DateTime endDate);
    Task<decimal> CalculateProRatedLeaveEntitlementAsync(Guid employeeId, Guid leaveTypeId, DateTime joiningDate, int year);

    // Leave Encashment
    Task<LeaveEncashmentDto> CalculateLeaveEncashmentAsync(Guid employeeId, DateTime lastWorkingDay);
    Task<LeaveEncashmentDto> ProcessLeaveEncashmentAsync(Guid employeeId, DateTime lastWorkingDay);

    // Validation
    Task<string?> ValidateLeaveApplicationAsync(Guid employeeId, CreateLeaveApplicationRequest request);
    Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeLeaveId = null);
}
