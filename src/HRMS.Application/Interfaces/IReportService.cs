using HRMS.Application.DTOs.Reports;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service for generating reports
/// FORTUNE 500 GRADE: All methods support cancellation tokens for responsive UX
/// </summary>
public interface IReportService
{
    // Dashboard
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

    // Payroll Reports
    Task<MonthlyPayrollSummaryDto> GetMonthlyPayrollSummaryAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<byte[]> ExportMonthlyPayrollToExcelAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<byte[]> ExportStatutoryDeductionsToExcelAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<byte[]> ExportBankTransferListToExcelAsync(int month, int year, CancellationToken cancellationToken = default);

    // Attendance Reports
    Task<MonthlyAttendanceReportDto> GetMonthlyAttendanceReportAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<OvertimeReportDto> GetOvertimeReportAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<byte[]> ExportAttendanceRegisterToExcelAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<byte[]> ExportOvertimeReportToExcelAsync(int month, int year, CancellationToken cancellationToken = default);

    // Leave Reports
    Task<LeaveBalanceReportDto> GetLeaveBalanceReportAsync(int year, CancellationToken cancellationToken = default);
    Task<LeaveUtilizationReportDto> GetLeaveUtilizationReportAsync(int year, CancellationToken cancellationToken = default);
    Task<byte[]> ExportLeaveBalanceToExcelAsync(int year, CancellationToken cancellationToken = default);

    // Employee Reports
    Task<HeadcountReportDto> GetHeadcountReportAsync(CancellationToken cancellationToken = default);
    Task<ExpatriateReportDto> GetExpatriateReportAsync(CancellationToken cancellationToken = default);
    Task<TurnoverReportDto> GetTurnoverReportAsync(int month, int year, CancellationToken cancellationToken = default);
    Task<byte[]> ExportHeadcountToExcelAsync(CancellationToken cancellationToken = default);
    Task<byte[]> ExportExpatriatesToExcelAsync(CancellationToken cancellationToken = default);
}
