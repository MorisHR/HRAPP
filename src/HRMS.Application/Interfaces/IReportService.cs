using HRMS.Application.DTOs.Reports;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service for generating reports
/// </summary>
public interface IReportService
{
    // Dashboard
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();

    // Payroll Reports
    Task<MonthlyPayrollSummaryDto> GetMonthlyPayrollSummaryAsync(int month, int year);
    Task<byte[]> ExportMonthlyPayrollToExcelAsync(int month, int year);
    Task<byte[]> ExportStatutoryDeductionsToExcelAsync(int month, int year);
    Task<byte[]> ExportBankTransferListToExcelAsync(int month, int year);

    // Attendance Reports
    Task<MonthlyAttendanceReportDto> GetMonthlyAttendanceReportAsync(int month, int year);
    Task<OvertimeReportDto> GetOvertimeReportAsync(int month, int year);
    Task<byte[]> ExportAttendanceRegisterToExcelAsync(int month, int year);
    Task<byte[]> ExportOvertimeReportToExcelAsync(int month, int year);

    // Leave Reports
    Task<LeaveBalanceReportDto> GetLeaveBalanceReportAsync(int year);
    Task<LeaveUtilizationReportDto> GetLeaveUtilizationReportAsync(int year);
    Task<byte[]> ExportLeaveBalanceToExcelAsync(int year);

    // Employee Reports
    Task<HeadcountReportDto> GetHeadcountReportAsync();
    Task<ExpatriateReportDto> GetExpatriateReportAsync();
    Task<TurnoverReportDto> GetTurnoverReportAsync(int month, int year);
    Task<byte[]> ExportHeadcountToExcelAsync();
    Task<byte[]> ExportExpatriatesToExcelAsync();
}
