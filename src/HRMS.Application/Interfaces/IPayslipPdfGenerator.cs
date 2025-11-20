using HRMS.Core.Entities.Tenant;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Interface for generating PDF payslips
/// FIXED: Created interface for dependency injection (CRITICAL-2)
/// </summary>
public interface IPayslipPdfGenerator
{
    /// <summary>
    /// Generates a PDF payslip document
    /// </summary>
    /// <param name="payslip">Payslip entity with all earnings and deductions</param>
    /// <param name="employee">Employee entity with personal details</param>
    /// <param name="companyName">Name of the company/organization</param>
    /// <returns>PDF document as byte array</returns>
    byte[] GeneratePayslipPdf(Payslip payslip, Employee employee, string companyName);
}
