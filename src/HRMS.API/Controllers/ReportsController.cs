using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,HR,Manager")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IPdfService _pdfService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService,
        IPdfService pdfService,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _pdfService = pdfService;
        _logger = logger;
    }

    // ==================== DASHBOARD ====================

    /// <summary>
    /// Get dashboard summary with KPIs
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var dashboard = await _reportService.GetDashboardSummaryAsync();
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard summary");
            return StatusCode(500, new { message = "Error retrieving dashboard data", error = ex.Message });
        }
    }

    // ==================== PAYROLL REPORTS ====================

    /// <summary>
    /// Get monthly payroll summary
    /// </summary>
    [HttpGet("payroll/monthly-summary")]
    public async Task<IActionResult> GetMonthlyPayrollSummary([FromQuery] int month, [FromQuery] int year)
    {
        try
        {
            var report = await _reportService.GetMonthlyPayrollSummaryAsync(month, year);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly payroll summary for {Month}/{Year}", month, year);
            return StatusCode(500, new { message = "Error retrieving payroll summary", error = ex.Message });
        }
    }

    /// <summary>
    /// Export monthly payroll summary to Excel
    /// </summary>
    [HttpGet("payroll/monthly-summary/excel")]
    public async Task<IActionResult> ExportMonthlyPayroll([FromQuery] int month, [FromQuery] int year)
    {
        try
        {
            var excelBytes = await _reportService.ExportMonthlyPayrollToExcelAsync(month, year);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"PayrollSummary_{year}_{month:D2}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting monthly payroll to Excel");
            return StatusCode(500, new { message = "Error exporting payroll to Excel", error = ex.Message });
        }
    }

    /// <summary>
    /// Export statutory deductions report to Excel
    /// </summary>
    [HttpGet("payroll/statutory-deductions/excel")]
    public async Task<IActionResult> ExportStatutoryDeductions([FromQuery] int month, [FromQuery] int year)
    {
        try
        {
            var excelBytes = await _reportService.ExportStatutoryDeductionsToExcelAsync(month, year);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"StatutoryDeductions_{year}_{month:D2}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting statutory deductions to Excel");
            return StatusCode(500, new { message = "Error exporting statutory deductions", error = ex.Message });
        }
    }

    /// <summary>
    /// Export bank transfer list to Excel
    /// </summary>
    [HttpGet("payroll/bank-transfer-list/excel")]
    public async Task<IActionResult> ExportBankTransferList([FromQuery] int month, [FromQuery] int year)
    {
        try
        {
            var excelBytes = await _reportService.ExportBankTransferListToExcelAsync(month, year);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BankTransferList_{year}_{month:D2}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting bank transfer list");
            return StatusCode(500, new { message = "Error exporting bank transfer list", error = ex.Message });
        }
    }

    // ==================== ATTENDANCE REPORTS ====================

    /// <summary>
    /// Get monthly attendance report
    /// </summary>
    [HttpGet("attendance/monthly-register")]
    public async Task<IActionResult> GetMonthlyAttendanceRegister([FromQuery] int month, [FromQuery] int year)
    {
        try
        {
            var report = await _reportService.GetMonthlyAttendanceReportAsync(month, year);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly attendance register");
            return StatusCode(500, new { message = "Error retrieving attendance register", error = ex.Message });
        }
    }

    /// <summary>
    /// Export monthly attendance register to Excel
    /// </summary>
    [HttpGet("attendance/monthly-register/excel")]
    public async Task<IActionResult> ExportAttendanceRegister([FromQuery] int month, [FromQuery] int year)
    {
        try
        {
            var excelBytes = await _reportService.ExportAttendanceRegisterToExcelAsync(month, year);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"AttendanceRegister_{year}_{month:D2}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting attendance register");
            return StatusCode(500, new { message = "Error exporting attendance register", error = ex.Message });
        }
    }

    /// <summary>
    /// Get overtime report
    /// </summary>
    [HttpGet("attendance/overtime")]
    public async Task<IActionResult> GetOvertimeReport([FromQuery] int month, [FromQuery] int year)
    {
        try
        {
            var report = await _reportService.GetOvertimeReportAsync(month, year);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overtime report");
            return StatusCode(500, new { message = "Error retrieving overtime report", error = ex.Message });
        }
    }

    /// <summary>
    /// Export overtime report to Excel
    /// </summary>
    [HttpGet("attendance/overtime/excel")]
    public async Task<IActionResult> ExportOvertimeReport([FromQuery] int month, [FromQuery] int year)
    {
        try
        {
            var excelBytes = await _reportService.ExportOvertimeReportToExcelAsync(month, year);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"OvertimeReport_{year}_{month:D2}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting overtime report");
            return StatusCode(500, new { message = "Error exporting overtime report", error = ex.Message });
        }
    }

    // ==================== LEAVE REPORTS ====================

    /// <summary>
    /// Get leave balance report for all employees
    /// </summary>
    [HttpGet("leave/balance")]
    public async Task<IActionResult> GetLeaveBalanceReport([FromQuery] int year)
    {
        try
        {
            var report = await _reportService.GetLeaveBalanceReportAsync(year);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leave balance report");
            return StatusCode(500, new { message = "Error retrieving leave balance report", error = ex.Message });
        }
    }

    /// <summary>
    /// Export leave balance report to Excel
    /// </summary>
    [HttpGet("leave/balance/excel")]
    public async Task<IActionResult> ExportLeaveBalance([FromQuery] int year)
    {
        try
        {
            var excelBytes = await _reportService.ExportLeaveBalanceToExcelAsync(year);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"LeaveBalance_{year}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting leave balance");
            return StatusCode(500, new { message = "Error exporting leave balance", error = ex.Message });
        }
    }

    /// <summary>
    /// Get leave utilization report
    /// </summary>
    [HttpGet("leave/utilization")]
    public async Task<IActionResult> GetLeaveUtilization([FromQuery] int year)
    {
        try
        {
            var report = await _reportService.GetLeaveUtilizationReportAsync(year);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leave utilization report");
            return StatusCode(500, new { message = "Error retrieving leave utilization", error = ex.Message });
        }
    }

    // ==================== EMPLOYEE REPORTS ====================

    /// <summary>
    /// Get headcount report
    /// </summary>
    [HttpGet("employees/headcount")]
    public async Task<IActionResult> GetHeadcount()
    {
        try
        {
            var report = await _reportService.GetHeadcountReportAsync();
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting headcount report");
            return StatusCode(500, new { message = "Error retrieving headcount", error = ex.Message });
        }
    }

    /// <summary>
    /// Export headcount report to Excel
    /// </summary>
    [HttpGet("employees/headcount/excel")]
    public async Task<IActionResult> ExportHeadcount()
    {
        try
        {
            var excelBytes = await _reportService.ExportHeadcountToExcelAsync();
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Headcount_{DateTime.UtcNow:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting headcount");
            return StatusCode(500, new { message = "Error exporting headcount", error = ex.Message });
        }
    }

    /// <summary>
    /// Get expatriate report with document expiry tracking
    /// </summary>
    [HttpGet("employees/expatriates")]
    public async Task<IActionResult> GetExpatriates()
    {
        try
        {
            var report = await _reportService.GetExpatriateReportAsync();
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expatriate report");
            return StatusCode(500, new { message = "Error retrieving expatriates", error = ex.Message });
        }
    }

    /// <summary>
    /// Export expatriate report to Excel
    /// </summary>
    [HttpGet("employees/expatriates/excel")]
    public async Task<IActionResult> ExportExpatriates()
    {
        try
        {
            var excelBytes = await _reportService.ExportExpatriatesToExcelAsync();
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Expatriates_{DateTime.UtcNow:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting expatriates");
            return StatusCode(500, new { message = "Error exporting expatriates", error = ex.Message });
        }
    }

    /// <summary>
    /// Get turnover report
    /// </summary>
    [HttpGet("employees/turnover")]
    public async Task<IActionResult> GetTurnover([FromQuery] int month, [FromQuery] int year)
    {
        try
        {
            var report = await _reportService.GetTurnoverReportAsync(month, year);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting turnover report");
            return StatusCode(500, new { message = "Error retrieving turnover", error = ex.Message });
        }
    }

    // ==================== PDF GENERATION ====================

    /// <summary>
    /// Generate payslip PDF
    /// </summary>
    [HttpGet("pdf/payslip/{payslipId}")]
    public async Task<IActionResult> GeneratePayslipPdf(Guid payslipId)
    {
        try
        {
            var pdfBytes = await _pdfService.GeneratePayslipPdfAsync(payslipId);
            return File(pdfBytes, "application/pdf", $"Payslip_{payslipId}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payslip PDF");
            return StatusCode(500, new { message = "Error generating payslip PDF", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate employment certificate PDF
    /// </summary>
    [HttpGet("pdf/employment-certificate/{employeeId}")]
    public async Task<IActionResult> GenerateEmploymentCertificate(Guid employeeId)
    {
        try
        {
            var pdfBytes = await _pdfService.GenerateEmploymentCertificatePdfAsync(employeeId);
            return File(pdfBytes, "application/pdf", $"EmploymentCertificate_{employeeId}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating employment certificate");
            return StatusCode(500, new { message = "Error generating employment certificate", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate attendance report PDF
    /// </summary>
    [HttpGet("pdf/attendance/{employeeId}")]
    public async Task<IActionResult> GenerateAttendanceReportPdf(
        Guid employeeId,
        [FromQuery] int month,
        [FromQuery] int year)
    {
        try
        {
            var pdfBytes = await _pdfService.GenerateAttendanceReportPdfAsync(employeeId, month, year);
            return File(pdfBytes, "application/pdf", $"AttendanceReport_{employeeId}_{year}_{month:D2}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attendance report PDF");
            return StatusCode(500, new { message = "Error generating attendance report", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate leave report PDF
    /// </summary>
    [HttpGet("pdf/leave/{employeeId}")]
    public async Task<IActionResult> GenerateLeaveReportPdf(
        Guid employeeId,
        [FromQuery] int year)
    {
        try
        {
            var pdfBytes = await _pdfService.GenerateLeaveReportPdfAsync(employeeId, year);
            return File(pdfBytes, "application/pdf", $"LeaveReport_{employeeId}_{year}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating leave report PDF");
            return StatusCode(500, new { message = "Error generating leave report", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate tax certificate (Form C for MRA) PDF
    /// </summary>
    [HttpGet("pdf/tax-certificate/{employeeId}")]
    public async Task<IActionResult> GenerateTaxCertificate(
        Guid employeeId,
        [FromQuery] int year)
    {
        try
        {
            var pdfBytes = await _pdfService.GenerateTaxCertificatePdfAsync(employeeId, year);
            return File(pdfBytes, "application/pdf", $"TaxCertificate_{employeeId}_{year}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tax certificate");
            return StatusCode(500, new { message = "Error generating tax certificate", error = ex.Message });
        }
    }
}
