using HRMS.Application.Interfaces;
using HRMS.Core.Exceptions;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HRMS.Infrastructure.Services;

public class PdfService : IPdfService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<PdfService> _logger;

    public PdfService(
        TenantDbContext context,
        ILogger<PdfService> logger)
    {
        _context = context;
        _logger = logger;

        // Configure QuestPDF license (Community license for development)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePayslipPdfAsync(Guid payslipId)
    {
        var payslip = await _context.Payslips
            .Include(p => p.Employee)
            .ThenInclude(e => e.Department)
            .Include(p => p.PayrollCycle)
            .FirstOrDefaultAsync(p => p.Id == payslipId);

        if (payslip == null)
        {
            throw new NotFoundException(
                ErrorCodes.PAY_SLIP_NOT_FOUND,
                "The payslip you requested could not be found.",
                $"Payslip ID {payslipId} not found in database",
                "Please verify the payslip selection or contact HR for assistance.");
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text($"Payslip - {new DateTime(payslip.PayrollCycle.Year, payslip.PayrollCycle.Month, 1):MMMM yyyy}")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(5);

                        // Employee Details
                        column.Item().Text("Employee Details").SemiBold().FontSize(14);
                        column.Item().LineHorizontal(1);
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Name: {payslip.Employee.FirstName} {payslip.Employee.LastName}");
                            row.RelativeItem().Text($"Employee Code: {payslip.Employee.EmployeeCode}");
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Department: {payslip.Employee.Department?.Name ?? "N/A"}");
                            row.RelativeItem().Text($"Payslip Number: {payslip.PayslipNumber}");
                        });

                        column.Item().PaddingTop(0.5f, Unit.Centimetre);

                        // Earnings
                        column.Item().Text("Earnings").SemiBold().FontSize(14);
                        column.Item().LineHorizontal(1);
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Basic Salary:");
                            row.RelativeItem().AlignRight().Text($"MUR {payslip.BasicSalary:N2}");
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Housing Allowance:");
                            row.RelativeItem().AlignRight().Text($"MUR {payslip.HousingAllowance:N2}");
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Transport Allowance:");
                            row.RelativeItem().AlignRight().Text($"MUR {payslip.TransportAllowance:N2}");
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Overtime Pay:");
                            row.RelativeItem().AlignRight().Text($"MUR {payslip.OvertimePay:N2}");
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Other Allowances:");
                            row.RelativeItem().AlignRight().Text($"MUR {payslip.OtherAllowances:N2}");
                        });
                        column.Item().LineHorizontal(1);
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Gross Salary:").SemiBold();
                            row.RelativeItem().AlignRight().Text($"MUR {payslip.TotalGrossSalary:N2}").SemiBold();
                        });

                        column.Item().PaddingTop(0.5f, Unit.Centimetre);

                        // Deductions
                        column.Item().Text("Deductions").SemiBold().FontSize(14);
                        column.Item().LineHorizontal(1);
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("CSG (Employee):");
                            row.RelativeItem().AlignRight().Text($"MUR {payslip.CSG_Employee:N2}");
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("NSF (Employee):");
                            row.RelativeItem().AlignRight().Text($"MUR {payslip.NSF_Employee:N2}");
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("PAYE Tax:");
                            row.RelativeItem().AlignRight().Text($"MUR {payslip.PAYE_Tax:N2}");
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Other Deductions:");
                            row.RelativeItem().AlignRight().Text($"MUR {payslip.OtherDeductions:N2}");
                        });
                        column.Item().LineHorizontal(1);
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Total Deductions:").SemiBold();
                            row.RelativeItem().AlignRight().Text($"MUR {payslip.TotalDeductions:N2}").SemiBold();
                        });

                        column.Item().PaddingTop(0.5f, Unit.Centimetre);

                        // Net Salary
                        column.Item().Background(Colors.Blue.Lighten3).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Text("NET SALARY:").SemiBold().FontSize(14);
                            row.RelativeItem().AlignRight().Text($"MUR {payslip.NetSalary:N2}").SemiBold().FontSize(14);
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Generated on ");
                        x.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")).SemiBold();
                    });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateEmploymentCertificatePdfAsync(Guid employeeId)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new NotFoundException(
                ErrorCodes.EMP_NOT_FOUND,
                "Employee information could not be found.",
                $"Employee ID {employeeId} not found in database",
                "Please verify the employee selection or contact HR.");
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(3, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header()
                    .AlignCenter()
                    .Text("CERTIFICATE OF EMPLOYMENT")
                    .SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);

                page.Content()
                    .PaddingVertical(2, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        column.Item().Text($"Date: {DateTime.UtcNow:dd MMMM yyyy}");

                        column.Item().PaddingTop(1, Unit.Centimetre).Text("TO WHOM IT MAY CONCERN").SemiBold().FontSize(14);

                        column.Item().Text(text =>
                        {
                            text.Line("This is to certify that");
                            text.Span($" {employee.FirstName} {employee.LastName} ").SemiBold();
                            text.Line($"(Employee Code: {employee.EmployeeCode}) is currently employed with our organization.");
                        });

                        // Note: Designation removed as Employee entity doesn't have Designation navigation property
                        column.Item().Text($"Department: {employee.Department?.Name ?? "N/A"}");
                        column.Item().Text($"Date of Joining: {employee.JoiningDate:dd MMMM yyyy}");

                        column.Item().PaddingTop(1, Unit.Centimetre).Text("This certificate is issued upon the employee's request for official purposes.");

                        column.Item().PaddingTop(2, Unit.Centimetre).Column(col =>
                        {
                            col.Item().Text("Yours sincerely,");
                            col.Item().PaddingTop(2, Unit.Centimetre).Text("_______________________");
                            col.Item().Text("Human Resources Department");
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text("This is a computer-generated certificate and does not require a signature.")
                    .FontSize(8).Italic();
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateAttendanceReportPdfAsync(Guid employeeId, int month, int year)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new NotFoundException(
                ErrorCodes.EMP_NOT_FOUND,
                "Employee information could not be found.",
                $"Employee ID {employeeId} not found in database",
                "Please verify the employee selection or contact HR.");
        }

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var attendances = await _context.Attendances
            .Where(a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate)
            .OrderBy(a => a.Date)
            .ToListAsync();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text("ATTENDANCE REPORT").SemiBold().FontSize(18);
                    column.Item().Text($"{new DateTime(year, month, 1):MMMM yyyy}").FontSize(14);
                });

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text($"Employee: {employee.FirstName} {employee.LastName}");
                    column.Item().Text($"Employee Code: {employee.EmployeeCode}");
                    column.Item().Text($"Department: {employee.Department?.Name ?? "N/A"}");

                    column.Item().PaddingTop(0.5f, Unit.Centimetre);

                    // Summary
                    var presentDays = attendances.Count(a => a.Status == Core.Enums.AttendanceStatus.Present);
                    var absentDays = attendances.Count(a => a.Status == Core.Enums.AttendanceStatus.Absent);
                    var totalWorkingHours = attendances.Sum(a => a.WorkingHours);
                    var totalOvertimeHours = attendances.Sum(a => a.OvertimeHours);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Present Days: {presentDays}");
                        row.RelativeItem().Text($"Absent Days: {absentDays}");
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Working Hours: {totalWorkingHours:N2}");
                        row.RelativeItem().Text($"Overtime Hours: {totalOvertimeHours:N2}");
                    });

                    column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("Daily Attendance:").SemiBold();

                    // Attendance table
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Date");
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Check In");
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Check Out");
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Hours");
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Status");
                        });

                        foreach (var att in attendances)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(att.Date.ToString("dd MMM"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(att.CheckInTime?.ToString("HH:mm") ?? "-");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(att.CheckOutTime?.ToString("HH:mm") ?? "-");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{att.WorkingHours:N1}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(att.Status.ToString());
                        }
                    });
                });

                page.Footer().AlignCenter().Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateLeaveReportPdfAsync(Guid employeeId, int year)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new NotFoundException(
                ErrorCodes.EMP_NOT_FOUND,
                "Employee information could not be found.",
                $"Employee ID {employeeId} not found in database",
                "Please verify the employee selection or contact HR.");
        }

        var leaveBalances = await _context.LeaveBalances
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.EmployeeId == employeeId && lb.Year == year)
            .ToListAsync();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text($"LEAVE REPORT - {year}").SemiBold().FontSize(18);

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text($"Employee: {employee.FirstName} {employee.LastName}");
                    column.Item().Text($"Employee Code: {employee.EmployeeCode}");
                    column.Item().Text($"Department: {employee.Department?.Name ?? "N/A"}");

                    column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("Leave Balances:").SemiBold();

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Leave Type");
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Entitlement");
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Used");
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Pending");
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Available");
                        });

                        foreach (var balance in leaveBalances)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(balance.LeaveType?.Name ?? "");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{balance.TotalEntitlement:N1}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{balance.UsedDays:N1}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{balance.PendingDays:N1}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{balance.AvailableDays:N1}");
                        }
                    });
                });

                page.Footer().AlignCenter().Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateTaxCertificatePdfAsync(Guid employeeId, int year)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            throw new NotFoundException(
                ErrorCodes.EMP_NOT_FOUND,
                "Employee information could not be found.",
                $"Employee ID {employeeId} not found in database",
                "Please verify the employee selection or contact HR.");
        }

        // Get all payslips for the year
        var payslips = await _context.Payslips
            .Include(p => p.PayrollCycle)
            .Where(p => p.EmployeeId == employeeId && p.PayrollCycle.Year == year)
            .ToListAsync();

        var totalGross = payslips.Sum(p => p.TotalGrossSalary);
        var totalPAYE = payslips.Sum(p => p.PAYE_Tax);
        var totalCSG = payslips.Sum(p => p.CSG_Employee);
        var totalNSF = payslips.Sum(p => p.NSF_Employee);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(3, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().AlignCenter().Text($"TAX CERTIFICATE - YEAR {year}").SemiBold().FontSize(18);

                page.Content().PaddingVertical(2, Unit.Centimetre).Column(column =>
                {
                    column.Spacing(15);

                    column.Item().Text($"Employee Name: {employee.FirstName} {employee.LastName}").SemiBold();
                    column.Item().Text($"Employee Code: {employee.EmployeeCode}");
                    column.Item().Text($"National ID: {employee.NationalIdCard ?? "N/A"}");

                    column.Item().PaddingTop(1, Unit.Centimetre).Text("INCOME AND TAX SUMMARY").SemiBold().FontSize(14);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Total Gross Income:");
                        row.RelativeItem().AlignRight().Text($"MUR {totalGross:N2}");
                    });

                    column.Item().PaddingTop(0.5f, Unit.Centimetre).Text("DEDUCTIONS").SemiBold();
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("PAYE Tax:");
                        row.RelativeItem().AlignRight().Text($"MUR {totalPAYE:N2}");
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("CSG (Employee):");
                        row.RelativeItem().AlignRight().Text($"MUR {totalCSG:N2}");
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("NSF (Employee):");
                        row.RelativeItem().AlignRight().Text($"MUR {totalNSF:N2}");
                    });

                    column.Item().PaddingTop(2, Unit.Centimetre).Text("This certificate is issued for income tax filing purposes with the Mauritius Revenue Authority (MRA).");

                    column.Item().PaddingTop(2, Unit.Centimetre).Column(col =>
                    {
                        col.Item().Text("_______________________");
                        col.Item().Text("Human Resources Department");
                        col.Item().Text($"Date: {DateTime.UtcNow:dd MMMM yyyy}");
                    });
                });

                page.Footer().AlignCenter().Text("This is a computer-generated certificate.").FontSize(8).Italic();
            });
        });

        return document.GeneratePdf();
    }
}
