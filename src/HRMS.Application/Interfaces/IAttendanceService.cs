using HRMS.Application.DTOs.AttendanceDtos;
using HRMS.Core.Enums;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Attendance management service with sector-aware calculations
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Record attendance (manual check-in/out)
    /// </summary>
    Task<Guid> RecordAttendanceAsync(CreateAttendanceDto dto, string createdBy);

    /// <summary>
    /// Get attendance by ID
    /// </summary>
    Task<AttendanceDetailsDto?> GetAttendanceByIdAsync(Guid id);

    /// <summary>
    /// Get attendances with filters
    /// </summary>
    Task<List<AttendanceListDto>> GetAttendancesAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? employeeId = null,
        Guid? departmentId = null,
        AttendanceStatus? status = null);

    /// <summary>
    /// Calculate working hours for an attendance record
    /// </summary>
    Task<decimal> CalculateWorkingHoursAsync(Guid attendanceId);

    /// <summary>
    /// Calculate overtime hours for employee in a week
    /// SECTOR-AWARE: Uses sector compliance rules for threshold and rates
    /// </summary>
    Task<decimal> CalculateOvertimeHoursAsync(Guid employeeId, DateTime weekStartDate);

    /// <summary>
    /// Get monthly attendance summary for employee
    /// </summary>
    Task<MonthlyAttendanceSummaryDto> GetMonthlyAttendanceAsync(Guid employeeId, int year, int month);

    /// <summary>
    /// Get team attendance for a manager
    /// </summary>
    Task<List<AttendanceListDto>> GetTeamAttendanceAsync(Guid managerId, DateTime date);

    /// <summary>
    /// Mark absent for date (automated job)
    /// </summary>
    Task MarkAbsentForDateAsync(DateTime date);

    /// <summary>
    /// Request attendance correction
    /// </summary>
    Task<Guid> RequestAttendanceCorrectionAsync(AttendanceCorrectionRequestDto dto, Guid requestedBy);

    /// <summary>
    /// Approve attendance correction
    /// </summary>
    Task<bool> ApproveAttendanceCorrectionAsync(Guid correctionId, ApproveAttendanceCorrectionDto dto, Guid approvedBy);

    /// <summary>
    /// Generate attendance report
    /// </summary>
    Task<AttendanceReportDto> GenerateAttendanceReportAsync(DateTime fromDate, DateTime toDate, Guid? departmentId = null);
}
