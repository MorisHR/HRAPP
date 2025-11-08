using HRMS.Application.DTOs.PayrollDtos;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service interface for payroll management operations
/// Handles payroll cycle creation, processing, calculations, and statutory compliance
/// </summary>
public interface IPayrollService
{
    // ==================== PAYROLL CYCLE MANAGEMENT ====================

    /// <summary>
    /// Creates a new payroll cycle for the specified month and year
    /// </summary>
    /// <param name="dto">Payroll cycle creation details</param>
    /// <param name="createdBy">Username of the user creating the cycle</param>
    /// <returns>ID of the created payroll cycle</returns>
    Task<Guid> CreatePayrollCycleAsync(CreatePayrollCycleDto dto, string createdBy);

    /// <summary>
    /// Retrieves a specific payroll cycle by ID
    /// </summary>
    /// <param name="id">Payroll cycle ID</param>
    /// <returns>Payroll cycle details or null if not found</returns>
    Task<PayrollCycleDto?> GetPayrollCycleAsync(Guid id);

    /// <summary>
    /// Retrieves all payroll cycles, optionally filtered by year
    /// </summary>
    /// <param name="year">Optional year filter</param>
    /// <returns>List of payroll cycle summaries</returns>
    Task<List<PayrollCycleSummaryDto>> GetPayrollCyclesAsync(int? year = null);

    /// <summary>
    /// Processes payroll for all or selected employees in a cycle
    /// Calculates salaries, deductions, and generates payslips
    /// </summary>
    /// <param name="payrollCycleId">ID of the payroll cycle to process</param>
    /// <param name="dto">Processing options (employee selection, notes)</param>
    /// <param name="processedBy">Username of the user processing payroll</param>
    Task ProcessPayrollAsync(Guid payrollCycleId, ProcessPayrollDto dto, string processedBy);

    /// <summary>
    /// Approves a processed payroll cycle for payment
    /// </summary>
    /// <param name="payrollCycleId">ID of the payroll cycle to approve</param>
    /// <param name="dto">Approval details</param>
    /// <param name="approvedBy">Username of the user approving payroll</param>
    Task ApprovePayrollAsync(Guid payrollCycleId, ApprovePayrollDto dto, string approvedBy);

    /// <summary>
    /// Cancels a payroll cycle (only if not yet paid)
    /// </summary>
    /// <param name="payrollCycleId">ID of the payroll cycle to cancel</param>
    Task CancelPayrollAsync(Guid payrollCycleId);

    /// <summary>
    /// Marks all payslips in a cycle as paid
    /// </summary>
    /// <param name="payrollCycleId">ID of the payroll cycle</param>
    Task MarkPayrollAsPaidAsync(Guid payrollCycleId);

    // ==================== PAYSLIP OPERATIONS ====================

    /// <summary>
    /// Retrieves detailed payslip information
    /// </summary>
    /// <param name="payslipId">Payslip ID</param>
    /// <returns>Detailed payslip or null if not found</returns>
    Task<PayslipDetailsDto?> GetPayslipAsync(Guid payslipId);

    /// <summary>
    /// Retrieves all payslips for a specific payroll cycle
    /// </summary>
    /// <param name="payrollCycleId">Payroll cycle ID</param>
    /// <returns>List of payslips</returns>
    Task<List<PayslipDto>> GetPayslipsForCycleAsync(Guid payrollCycleId);

    /// <summary>
    /// Retrieves all payslips for a specific employee, optionally filtered by year
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="year">Optional year filter</param>
    /// <returns>List of employee payslips</returns>
    Task<List<EmployeePayslipDto>> GetEmployeePayslipsAsync(Guid employeeId, int? year = null);

    /// <summary>
    /// Regenerates a single payslip (for corrections)
    /// </summary>
    /// <param name="payslipId">Payslip ID to regenerate</param>
    /// <param name="updatedBy">Username of the user regenerating</param>
    Task RegeneratePayslipAsync(Guid payslipId, string updatedBy);

    // ==================== MAURITIUS STATUTORY CALCULATIONS ====================

    /// <summary>
    /// Calculates CSG employee contribution (1.5% or 3% based on salary threshold MUR 50,000)
    /// </summary>
    /// <param name="monthlySalary">Monthly gross salary</param>
    /// <returns>CSG employee contribution amount</returns>
    Task<decimal> CalculateCSGEmployeeAsync(decimal monthlySalary);

    /// <summary>
    /// Calculates CSG employer contribution (3% or 6% based on salary threshold MUR 50,000)
    /// </summary>
    /// <param name="monthlySalary">Monthly gross salary</param>
    /// <returns>CSG employer contribution amount</returns>
    Task<decimal> CalculateCSGEmployerAsync(decimal monthlySalary);

    /// <summary>
    /// Calculates NSF employee contribution (1% of basic salary)
    /// </summary>
    /// <param name="basicSalary">Employee's basic monthly salary</param>
    /// <returns>NSF employee contribution amount</returns>
    Task<decimal> CalculateNSFEmployeeAsync(decimal basicSalary);

    /// <summary>
    /// Calculates NSF employer contribution (2.5% of basic salary)
    /// </summary>
    /// <param name="basicSalary">Employee's basic monthly salary</param>
    /// <returns>NSF employer contribution amount</returns>
    Task<decimal> CalculateNSFEmployerAsync(decimal basicSalary);

    /// <summary>
    /// Calculates NPF employee contribution - LEGACY (3% of basic salary)
    /// </summary>
    /// <param name="basicSalary">Employee's basic monthly salary</param>
    /// <returns>NPF employee contribution amount</returns>
    Task<decimal> CalculateNPFEmployeeAsync(decimal basicSalary);

    /// <summary>
    /// Calculates NPF employer contribution - LEGACY (6% of basic salary)
    /// </summary>
    /// <param name="basicSalary">Employee's basic monthly salary</param>
    /// <returns>NPF employer contribution amount</returns>
    Task<decimal> CalculateNPFEmployerAsync(decimal basicSalary);

    /// <summary>
    /// Calculates PRGF employer contribution (progressive based on years of service)
    /// Only for employees hired after January 1, 2020
    /// Rates: ≤5 years = 4.3%, ≤10 years = 5%, >10 years = 6.8%
    /// </summary>
    /// <param name="grossSalary">Monthly gross salary</param>
    /// <param name="yearsOfService">Years of service</param>
    /// <param name="joiningDate">Employee joining date</param>
    /// <returns>PRGF contribution amount</returns>
    Task<decimal> CalculatePRGFAsync(decimal grossSalary, int yearsOfService, DateTime joiningDate);

    /// <summary>
    /// Calculates Training Levy employer contribution (1.5% of basic salary)
    /// </summary>
    /// <param name="basicSalary">Employee's basic monthly salary</param>
    /// <returns>Training levy amount</returns>
    Task<decimal> CalculateTrainingLevyAsync(decimal basicSalary);

    /// <summary>
    /// Calculates PAYE tax (progressive income tax)
    /// Tax brackets (2025):
    /// - Up to MUR 390,000: 0%
    /// - MUR 390,001 - 550,000: 10%
    /// - MUR 550,001 - 650,000: 12%
    /// - Above MUR 650,000: 20%
    /// </summary>
    /// <param name="annualGrossSalary">Annual gross salary</param>
    /// <param name="annualDeductions">Annual deductions (NPF/CSG, NSF)</param>
    /// <returns>Monthly PAYE tax amount</returns>
    Task<decimal> CalculatePAYEAsync(decimal annualGrossSalary, decimal annualDeductions);

    // ==================== EARNINGS CALCULATIONS ====================

    /// <summary>
    /// Calculates complete payroll from approved timesheets for a given period
    /// NEW METHOD - Replaces manual hour entry with timesheet-based calculation
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="periodStart">Start date of payroll period</param>
    /// <param name="periodEnd">End date of payroll period</param>
    /// <returns>Complete payroll calculation result with all deductions</returns>
    Task<PayrollResult> CalculatePayrollFromTimesheetsAsync(Guid employeeId, DateTime periodStart, DateTime periodEnd);

    /// <summary>
    /// Calculates overtime pay from attendance records with sector-aware rates
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="month">Month (1-12)</param>
    /// <param name="year">Year</param>
    /// <returns>Total overtime pay amount</returns>
    Task<decimal> CalculateOvertimePayAsync(Guid employeeId, int month, int year);

    /// <summary>
    /// Calculates 13th month bonus (1/12 of annual gross earnings)
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="year">Year</param>
    /// <returns>13th month bonus amount</returns>
    Task<decimal> Calculate13thMonthBonusAsync(Guid employeeId, int year);

    /// <summary>
    /// Calculates gratuity payment for employees leaving (hired before 2020)
    /// Formula: 15 days per year of service
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="resignationDate">Resignation/termination date</param>
    /// <returns>Gratuity payment amount</returns>
    Task<decimal> CalculateGratuityAsync(Guid employeeId, DateTime resignationDate);

    /// <summary>
    /// Calculates leave encashment for unused annual leave
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <returns>Leave encashment amount</returns>
    Task<decimal> CalculateLeaveEncashmentAsync(Guid employeeId);

    // ==================== REPORTS & EXPORTS ====================

    /// <summary>
    /// Generates comprehensive payroll summary report
    /// </summary>
    /// <param name="payrollCycleId">Payroll cycle ID</param>
    /// <returns>Detailed payroll summary</returns>
    Task<PayrollSummaryDto> GetPayrollSummaryAsync(Guid payrollCycleId);

    /// <summary>
    /// Generates bank transfer file for bulk payment processing
    /// Format: CSV with employee bank details and net salary amounts
    /// </summary>
    /// <param name="payrollCycleId">Payroll cycle ID</param>
    /// <returns>Bank transfer file content as byte array</returns>
    Task<byte[]> GenerateBankTransferFileAsync(Guid payrollCycleId);

    /// <summary>
    /// Generates PDF payslip for an employee
    /// </summary>
    /// <param name="payslipId">Payslip ID</param>
    /// <returns>PDF file content as byte array</returns>
    Task<byte[]> GeneratePayslipPdfAsync(Guid payslipId);
}
