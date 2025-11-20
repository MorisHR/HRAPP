using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using HRMS.Core.Entities.Tenant;
using HRMS.Application.Interfaces;
using System.Globalization;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for generating professional PDF payslips compliant with Mauritius labor laws
/// Uses QuestPDF library for document generation
/// FIXED: Implements IPayslipPdfGenerator for dependency injection (CRITICAL-2)
/// </summary>
public class PayslipPdfGeneratorService : IPayslipPdfGenerator
{
    private const string CURRENCY = "MUR";
    private static readonly CultureInfo MauritiusCulture = new CultureInfo("en-MU");

    static PayslipPdfGeneratorService()
    {
        // Configure QuestPDF license (Community license is free for non-commercial or small commercial use)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Generates a PDF payslip document
    /// </summary>
    /// <param name="payslip">Payslip entity with all earnings and deductions</param>
    /// <param name="employee">Employee entity with personal details</param>
    /// <param name="companyName">Name of the company/organization</param>
    /// <returns>PDF document as byte array</returns>
    public byte[] GeneratePayslipPdf(Payslip payslip, Employee employee, string companyName)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });

            // Local method for header
            void ComposeHeader(IContainer container)
            {
                container.Column(column =>
                {
                    // Company header
                    column.Item().BorderBottom(2).BorderColor(Colors.Blue.Darken2).PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(companyName)
                                .FontSize(18)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            col.Item().PaddingTop(5).Text("PAYSLIP")
                                .FontSize(14)
                                .SemiBold();
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"Payslip Number: {payslip.PayslipNumber}")
                                .FontSize(9)
                                .SemiBold();

                            col.Item().Text($"Period: {GetMonthName(payslip.Month)} {payslip.Year}")
                                .FontSize(11)
                                .SemiBold();

                            if (payslip.PayrollCycle?.PaymentDate != null)
                            {
                                col.Item().Text($"Payment Date: {payslip.PayrollCycle.PaymentDate:dd MMM yyyy}")
                                    .FontSize(9);
                            }
                        });
                    });
                });
            }

            // Local method for content
            void ComposeContent(IContainer container)
            {
                container.PaddingVertical(15).Column(column =>
                {
                    column.Spacing(15);

                    // Employee Information Section
                    column.Item().Element(ComposeEmployeeInfo);

                    // Attendance Summary
                    column.Item().Element(ComposeAttendanceSummary);

                    // Earnings Section
                    column.Item().Element(ComposeEarnings);

                    // Deductions Section
                    column.Item().Element(ComposeDeductions);

                    // Net Pay Section
                    column.Item().Element(ComposeNetPay);
                });
            }

            // Employee Information
            void ComposeEmployeeInfo(IContainer container)
            {
                container.Background(Colors.Grey.Lighten3).Padding(10).Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Employee: ").SemiBold();
                            text.Span($"{employee.FirstName} {employee.LastName}");
                        });

                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Employee Code: ").SemiBold();
                            text.Span(employee.EmployeeCode);
                        });
                    });

                    column.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Department: ").SemiBold();
                            text.Span(employee.Department?.Name ?? "N/A");
                        });

                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Designation: ").SemiBold();
                            text.Span(employee.Designation ?? "N/A");
                        });
                    });
                });
            }

            // Attendance Summary
            void ComposeAttendanceSummary(IContainer container)
            {
                if (payslip.WorkingDays > 0)
                {
                    container.Padding(5).Row(row =>
                    {
                        row.RelativeItem().Text($"Working Days: {payslip.WorkingDays}").FontSize(9);
                        row.RelativeItem().Text($"Days Worked: {payslip.ActualDaysWorked}").FontSize(9);
                        row.RelativeItem().Text($"Paid Leave: {payslip.PaidLeaveDays:F1}").FontSize(9);

                        if (payslip.UnpaidLeaveDays > 0)
                        {
                            row.RelativeItem().Text($"Unpaid Leave: {payslip.UnpaidLeaveDays:F1}").FontSize(9).FontColor(Colors.Red.Darken1);
                        }

                        if (payslip.OvertimeHours > 0)
                        {
                            row.RelativeItem().Text($"Overtime Hours: {payslip.OvertimeHours:F2}").FontSize(9).FontColor(Colors.Green.Darken1);
                        }
                    });
                }
            }

            // Earnings Section
            void ComposeEarnings(IContainer container)
            {
                container.Column(column =>
                {
                    // Section header
                    column.Item().Background(Colors.Blue.Lighten3).Padding(5).Text("EARNINGS")
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken3);

                    // Earnings table
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Description
                            columns.RelativeColumn(1); // Amount
                        });

                        // Table styling
                        table.Cell().Element(CellStyle).Text("Description").SemiBold();
                        table.Cell().Element(CellStyle).AlignRight().Text($"Amount ({CURRENCY})").SemiBold();

                        // Basic Salary
                        if (payslip.BasicSalary > 0)
                            AddEarningRow(table, "Basic Salary", payslip.BasicSalary);

                        // Allowances
                        if (payslip.HousingAllowance > 0)
                            AddEarningRow(table, "Housing Allowance", payslip.HousingAllowance);
                        if (payslip.TransportAllowance > 0)
                            AddEarningRow(table, "Transport Allowance", payslip.TransportAllowance);
                        if (payslip.MealAllowance > 0)
                            AddEarningRow(table, "Meal Allowance", payslip.MealAllowance);
                        if (payslip.MobileAllowance > 0)
                            AddEarningRow(table, "Mobile Allowance", payslip.MobileAllowance);
                        if (payslip.OtherAllowances > 0)
                            AddEarningRow(table, "Other Allowances", payslip.OtherAllowances);

                        // Overtime
                        if (payslip.OvertimePay > 0)
                            AddEarningRow(table, $"Overtime Pay ({payslip.OvertimeHours:F2} hrs)", payslip.OvertimePay);

                        // Special Payments
                        if (payslip.ThirteenthMonthBonus > 0)
                            AddEarningRow(table, "13th Month Bonus", payslip.ThirteenthMonthBonus);
                        if (payslip.LeaveEncashment > 0)
                            AddEarningRow(table, "Leave Encashment", payslip.LeaveEncashment);
                        if (payslip.GratuityPayment > 0)
                            AddEarningRow(table, "Gratuity Payment", payslip.GratuityPayment);
                        if (payslip.Commission > 0)
                            AddEarningRow(table, "Commission/Bonus", payslip.Commission);

                        // Gross Total
                        table.Cell().Element(CellStyle).BorderTop(1).PaddingTop(5).Text("GROSS PAY").Bold();
                        table.Cell().Element(CellStyle).BorderTop(1).PaddingTop(5).AlignRight()
                            .Text(FormatCurrency(payslip.TotalGrossSalary)).Bold().FontColor(Colors.Green.Darken2);
                    });
                });
            }

            // Deductions Section
            void ComposeDeductions(IContainer container)
            {
                container.Column(column =>
                {
                    // Section header
                    column.Item().Background(Colors.Red.Lighten4).Padding(5).Text("DEDUCTIONS")
                        .SemiBold()
                        .FontColor(Colors.Red.Darken3);

                    // Deductions table
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Description
                            columns.RelativeColumn(1); // Amount
                        });

                        // Table styling
                        table.Cell().Element(CellStyle).Text("Description").SemiBold();
                        table.Cell().Element(CellStyle).AlignRight().Text($"Amount ({CURRENCY})").SemiBold();

                        // Statutory Deductions
                        if (payslip.CSG_Employee > 0)
                            AddDeductionRow(table, "CSG - Contribution Sociale Généralisée", payslip.CSG_Employee);
                        if (payslip.NPF_Employee > 0)
                            AddDeductionRow(table, "NPF - National Pension Fund (Legacy)", payslip.NPF_Employee);
                        if (payslip.NSF_Employee > 0)
                            AddDeductionRow(table, "NSF - National Savings Fund", payslip.NSF_Employee);
                        if (payslip.PAYE_Tax > 0)
                            AddDeductionRow(table, "PAYE - Income Tax", payslip.PAYE_Tax);

                        // Other Deductions
                        if (payslip.LeaveDeductions > 0)
                            AddDeductionRow(table, "Leave Without Pay", payslip.LeaveDeductions);
                        if (payslip.LoanDeduction > 0)
                            AddDeductionRow(table, "Loan Repayment", payslip.LoanDeduction);
                        if (payslip.AdvanceDeduction > 0)
                            AddDeductionRow(table, "Advance Salary Deduction", payslip.AdvanceDeduction);
                        if (payslip.MedicalInsurance > 0)
                            AddDeductionRow(table, "Medical Insurance", payslip.MedicalInsurance);
                        if (payslip.OtherDeductions > 0)
                            AddDeductionRow(table, "Other Deductions", payslip.OtherDeductions);

                        // Total Deductions
                        table.Cell().Element(CellStyle).BorderTop(1).PaddingTop(5).Text("TOTAL DEDUCTIONS").Bold();
                        table.Cell().Element(CellStyle).BorderTop(1).PaddingTop(5).AlignRight()
                            .Text(FormatCurrency(payslip.TotalDeductions)).Bold().FontColor(Colors.Red.Darken2);
                    });

                    // Employer Contributions (informational only)
                    if (payslip.CSG_Employer > 0 || payslip.NSF_Employer > 0 || payslip.PRGF_Contribution > 0 || payslip.TrainingLevy > 0)
                    {
                        column.Item().PaddingTop(10).Column(col =>
                        {
                            col.Item().Text("Employer Contributions (for information only)").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);

                            col.Item().PaddingTop(3).Row(row =>
                            {
                                if (payslip.CSG_Employer > 0)
                                    row.RelativeItem().Text($"CSG: {FormatCurrency(payslip.CSG_Employer)}").FontSize(8);
                                if (payslip.NSF_Employer > 0)
                                    row.RelativeItem().Text($"NSF: {FormatCurrency(payslip.NSF_Employer)}").FontSize(8);
                                if (payslip.PRGF_Contribution > 0)
                                    row.RelativeItem().Text($"PRGF: {FormatCurrency(payslip.PRGF_Contribution)}").FontSize(8);
                                if (payslip.TrainingLevy > 0)
                                    row.RelativeItem().Text($"Training Levy: {FormatCurrency(payslip.TrainingLevy)}").FontSize(8);
                            });
                        });
                    }
                });
            }

            // Net Pay Section
            void ComposeNetPay(IContainer container)
            {
                container.Background(Colors.Green.Lighten4).BorderColor(Colors.Green.Darken2).Border(2).Padding(15).Row(row =>
                {
                    row.RelativeItem().Text("NET PAY").FontSize(14).Bold().FontColor(Colors.Green.Darken3);
                    row.RelativeItem().AlignRight().Text(FormatCurrency(payslip.NetSalary))
                        .FontSize(18)
                        .Bold()
                        .FontColor(Colors.Green.Darken3);
                });
            }

            // Footer
            void ComposeFooter(IContainer container)
            {
                container.AlignCenter().Column(column =>
                {
                    column.Item().PaddingTop(10).BorderTop(1).BorderColor(Colors.Grey.Lighten1);

                    column.Item().PaddingTop(5).Text(text =>
                    {
                        text.Span("This is a computer-generated payslip and does not require a signature. ").FontSize(8).Italic();
                        text.Span($"Generated on {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                    });

                    column.Item().PaddingTop(3).Text("All statutory deductions are in compliance with Mauritius Labor Laws and Regulations.")
                        .FontSize(8)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);

                    if (!string.IsNullOrWhiteSpace(payslip.Remarks))
                    {
                        column.Item().PaddingTop(5).Text($"Remarks: {payslip.Remarks}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken2);
                    }
                });
            }

            // Helper methods for table cells
            IContainer CellStyle(IContainer container)
            {
                return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
            }

            void AddEarningRow(TableDescriptor table, string description, decimal amount)
            {
                table.Cell().Element(CellStyle).Text(description);
                table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(amount));
            }

            void AddDeductionRow(TableDescriptor table, string description, decimal amount)
            {
                table.Cell().Element(CellStyle).Text(description);
                table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(amount));
            }
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Formats currency value in Mauritius format
    /// </summary>
    private static string FormatCurrency(decimal amount)
    {
        return amount.ToString("N2", MauritiusCulture);
    }

    /// <summary>
    /// Gets the month name from month number
    /// </summary>
    private static string GetMonthName(int month)
    {
        return new DateTime(2000, month, 1).ToString("MMMM", MauritiusCulture);
    }
}
