using System.Globalization;
using System.Text;
using HRMS.Application.DTOs.PayrollDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for managing payroll operations and Mauritius statutory calculations
/// </summary>
public class PayrollService : IPayrollService
{
    private readonly TenantDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ISalaryComponentService _salaryComponentService;
    private readonly ILogger<PayrollService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPayslipPdfGenerator _pdfGenerator; // FIXED: Use interface instead of concrete class (CRITICAL-2)

    // Role constants for authorization
    private const string RoleAdmin = "Admin";
    private const string RoleHR = "HR";
    private const string RoleManager = "Manager";
    private const string RoleEmployee = "Employee";

    // Mauritius 2025 Constants
    private const decimal CSG_THRESHOLD = 50000m; // MUR 50,000
    private const decimal CSG_EMPLOYEE_LOW = 0.015m; // 1.5%
    private const decimal CSG_EMPLOYEE_HIGH = 0.03m; // 3%
    private const decimal CSG_EMPLOYER_LOW = 0.03m; // 3%
    private const decimal CSG_EMPLOYER_HIGH = 0.06m; // 6%

    private const decimal NSF_EMPLOYEE = 0.01m; // 1%
    private const decimal NSF_EMPLOYER = 0.025m; // 2.5%

    private const decimal NPF_EMPLOYEE = 0.03m; // 3% (legacy)
    private const decimal NPF_EMPLOYER = 0.06m; // 6% (legacy)

    private const decimal TRAINING_LEVY = 0.015m; // 1.5%

    private const decimal PRGF_RATE_0_5_YEARS = 0.043m; // 4.3%
    private const decimal PRGF_RATE_6_10_YEARS = 0.050m; // 5%
    private const decimal PRGF_RATE_ABOVE_10_YEARS = 0.068m; // 6.8%

    private static readonly DateTime PRGF_IMPLEMENTATION_DATE = new DateTime(2020, 1, 1);

    // PAYE Tax Brackets 2025 (Annual)
    private const decimal PAYE_THRESHOLD = 390000m; // Tax-free threshold
    private const decimal PAYE_BRACKET_1_LIMIT = 550000m;
    private const decimal PAYE_BRACKET_2_LIMIT = 650000m;

    private const decimal PAYE_RATE_1 = 0.10m; // 10%
    private const decimal PAYE_RATE_2 = 0.12m; // 12%
    private const decimal PAYE_RATE_3 = 0.20m; // 20%

    private const decimal STANDARD_MONTHLY_HOURS = 173.33m; // For hourly rate calculation

    public PayrollService(
        TenantDbContext context,
        ITenantService tenantService,
        ISalaryComponentService salaryComponentService,
        ILogger<PayrollService> logger,
        ICurrentUserService currentUserService,
        IPayslipPdfGenerator pdfGenerator) // FIXED: Inject via constructor (CRITICAL-2)
    {
        _context = context;
        _tenantService = tenantService;
        _salaryComponentService = salaryComponentService;
        _logger = logger;
        _currentUserService = currentUserService;
        _pdfGenerator = pdfGenerator; // FIXED: Use injected instance (CRITICAL-2)
    }

    #region Authorization Helper Methods

    /// <summary>
    /// Checks if the current user is HR or Admin
    /// SECURITY: HR and Admin have full access to payroll operations
    /// </summary>
    private bool IsHROrAdmin()
    {
        return _currentUserService.HasAnyRole(RoleAdmin, RoleHR);
    }

    /// <summary>
    /// Checks if the current user can view a specific payslip
    /// SECURITY:
    /// - Admin/HR can view all payslips
    /// - Manager can view payslips for their direct reports
    /// - Employee can view only their own payslips
    /// </summary>
    private async Task<bool> CanViewPayslipAsync(Guid payslipId)
    {
        // Admin and HR can view all payslips
        if (IsHROrAdmin())
            return true;

        // Get the payslip to check employee ownership
        var payslip = await _context.Payslips
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == payslipId && !p.IsDeleted);

        if (payslip == null)
            return false;

        // Get current user's employee record
        var currentEmployee = await GetEmployeeByUserIdAsync(_currentUserService.UserId ?? "");
        if (currentEmployee == null)
            return false;

        // Employee can view their own payslip
        if (payslip.EmployeeId == currentEmployee.Id)
            return true;

        // Manager can view direct reports' payslips
        if (_currentUserService.HasRole(RoleManager))
        {
            var targetEmployee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == payslip.EmployeeId);

            if (targetEmployee?.ManagerId == currentEmployee.Id)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the current user can view payslips for a specific employee
    /// SECURITY: Same rules as CanViewPayslipAsync but checks employee directly
    /// </summary>
    private async Task<bool> CanViewEmployeePayslipsAsync(Guid employeeId)
    {
        // Admin and HR can view all
        if (IsHROrAdmin())
            return true;

        // Get current user's employee record
        var currentEmployee = await GetEmployeeByUserIdAsync(_currentUserService.UserId ?? "");
        if (currentEmployee == null)
            return false;

        // Employee can view their own payslips
        if (employeeId == currentEmployee.Id)
            return true;

        // Manager can view direct reports' payslips
        if (_currentUserService.HasRole(RoleManager))
        {
            var targetEmployee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (targetEmployee?.ManagerId == currentEmployee.Id)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the current user can generate payroll
    /// SECURITY: Only Admin and HR can generate payroll
    /// </summary>
    private bool CanGeneratePayroll()
    {
        return IsHROrAdmin();
    }

    /// <summary>
    /// Checks if the current user can approve payroll
    /// SECURITY: Only Admin and HR can approve payroll
    /// </summary>
    private bool CanApprovePayroll()
    {
        return IsHROrAdmin();
    }

    /// <summary>
    /// Gets employee record by user ID (from authentication)
    /// </summary>
    private async Task<Employee?> GetEmployeeByUserIdAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        // Map by email since that's the common link
        return await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Email == _currentUserService.Email && e.IsActive);
    }

    /// <summary>
    /// Gets list of employee IDs that the current user can view payroll for
    /// SECURITY: Returns filtered list based on user role
    /// </summary>
    private async Task<List<Guid>> GetAuthorizedEmployeeIdsAsync()
    {
        // Admin and HR can view all
        if (IsHROrAdmin())
            return new List<Guid>(); // Empty list means no filtering needed

        var currentEmployee = await GetEmployeeByUserIdAsync(_currentUserService.UserId ?? "");
        if (currentEmployee == null)
            return new List<Guid>(); // No access

        var authorizedIds = new List<Guid> { currentEmployee.Id };

        // Manager can view direct reports
        if (_currentUserService.HasRole(RoleManager))
        {
            var directReports = await _context.Employees
                .AsNoTracking()
                .Where(e => e.ManagerId == currentEmployee.Id && e.IsActive)
                .Select(e => e.Id)
                .ToListAsync();

            authorizedIds.AddRange(directReports);
        }

        return authorizedIds;
    }

    /// <summary>
    /// Logs authorization failure for security audit
    /// </summary>
    private void LogAuthorizationFailure(string operation, Guid? resourceId = null)
    {
        _logger.LogWarning(
            "AUTHORIZATION FAILURE: User {UserId} ({Username}) attempted {Operation} on {ResourceId}. Roles: {Roles}",
            _currentUserService.UserId,
            _currentUserService.Username,
            operation,
            resourceId,
            string.Join(", ", _currentUserService.Roles));
    }

    /// <summary>
    /// Logs sensitive payroll operations for audit trail
    /// </summary>
    private void LogSensitiveOperation(string operation, Guid? resourceId = null, string? additionalInfo = null)
    {
        _logger.LogInformation(
            "PAYROLL OPERATION: User {UserId} performed {Operation} on {ResourceId}. Additional Info: {Info}",
            _currentUserService.UserId,
            operation,
            resourceId,
            additionalInfo ?? "N/A");
    }

    #endregion

    // ==================== PAYROLL CYCLE MANAGEMENT ====================

    public async Task<Guid> CreatePayrollCycleAsync(CreatePayrollCycleDto dto, string createdBy)
    {
        _logger.LogInformation("Creating payroll cycle for {Month}/{Year}", dto.Month, dto.Year);

        // Check if payroll cycle already exists
        var existing = await _context.PayrollCycles
            .AnyAsync(p => p.Month == dto.Month && p.Year == dto.Year && !p.IsDeleted);

        if (existing)
        {
            throw new InvalidOperationException($"Payroll cycle for {dto.Month}/{dto.Year} already exists");
        }

        var payrollCycle = new PayrollCycle
        {
            Id = Guid.NewGuid(),
            Month = dto.Month,
            Year = dto.Year,
            Status = PayrollCycleStatus.Draft,
            PaymentDate = dto.PaymentDate,
            Notes = dto.Notes,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.PayrollCycles.Add(payrollCycle);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payroll cycle {CycleId} created successfully", payrollCycle.Id);

        return payrollCycle.Id;
    }

    public async Task<PayrollCycleDto?> GetPayrollCycleAsync(Guid id)
    {
        var cycle = await _context.PayrollCycles
            .AsNoTracking()
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync();

        if (cycle == null)
            return null;

        return new PayrollCycleDto
        {
            Id = cycle.Id,
            Month = cycle.Month,
            Year = cycle.Year,
            MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(cycle.Month),
            Status = cycle.Status,
            StatusDisplay = cycle.Status.ToString(),
            TotalGrossSalary = cycle.TotalGrossSalary,
            TotalDeductions = cycle.TotalDeductions,
            TotalNetSalary = cycle.TotalNetSalary,
            TotalNPFEmployee = cycle.TotalNPFEmployee,
            TotalNPFEmployer = cycle.TotalNPFEmployer,
            TotalNSFEmployee = cycle.TotalNSFEmployee,
            TotalNSFEmployer = cycle.TotalNSFEmployer,
            TotalCSGEmployee = cycle.TotalCSGEmployee,
            TotalCSGEmployer = cycle.TotalCSGEmployer,
            TotalPRGF = cycle.TotalPRGF,
            TotalTrainingLevy = cycle.TotalTrainingLevy,
            TotalPAYE = cycle.TotalPAYE,
            TotalOvertimePay = cycle.TotalOvertimePay,
            EmployeeCount = cycle.EmployeeCount,
            ProcessedBy = cycle.ProcessedBy,
            ProcessedAt = cycle.ProcessedAt,
            ApprovedBy = cycle.ApprovedBy,
            ApprovedAt = cycle.ApprovedAt,
            PaymentDate = cycle.PaymentDate,
            Notes = cycle.Notes,
            CreatedAt = cycle.CreatedAt,
            UpdatedAt = cycle.UpdatedAt
        };
    }

    /// <summary>
    /// Gets all payroll cycles with role-based filtering
    /// SECURITY:
    /// - Admin/HR: View all cycles
    /// - Manager: View cycles (limited to summary info)
    /// - Employee: View cycles (limited to summary info)
    /// - Public: No access
    /// </summary>
    public async Task<List<PayrollCycleSummaryDto>> GetPayrollCyclesAsync(int? year = null)
    {
        // AUTHORIZATION CHECK: Users must be authenticated
        if (!_currentUserService.IsAuthenticated)
        {
            LogAuthorizationFailure("GetPayrollCycles");
            throw new UnauthorizedAccessException("Authentication required to view payroll cycles");
        }

        var query = _context.PayrollCycles
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (year.HasValue)
            query = query.Where(p => p.Year == year.Value);

        var cycles = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ToListAsync();

        _logger.LogInformation("User {UserId} retrieved {Count} payroll cycles",
            _currentUserService.UserId, cycles.Count);

        return cycles.Select(c => new PayrollCycleSummaryDto
        {
            Id = c.Id,
            Month = c.Month,
            Year = c.Year,
            MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(c.Month),
            PeriodDisplay = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(c.Month)} {c.Year}",
            Status = c.Status,
            StatusDisplay = c.Status.ToString(),
            EmployeeCount = c.EmployeeCount,
            TotalNetSalary = c.TotalNetSalary,
            PaymentDate = c.PaymentDate,
            ProcessedAt = c.ProcessedAt,
            ApprovedAt = c.ApprovedAt,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    /// <summary>
    /// Processes payroll for a cycle
    /// SECURITY: Only Admin and HR can generate/process payroll
    /// </summary>
    public async Task ProcessPayrollAsync(Guid payrollCycleId, ProcessPayrollDto dto, string processedBy)
    {
        // AUTHORIZATION CHECK: Only HR/Admin can generate payroll
        if (!CanGeneratePayroll())
        {
            LogAuthorizationFailure("ProcessPayroll", payrollCycleId);
            throw new UnauthorizedAccessException("Only HR and Admin roles can generate/process payroll");
        }

        LogSensitiveOperation("ProcessPayroll", payrollCycleId, $"EmployeeCount: {dto.EmployeeIds?.Count ?? 0}");
        _logger.LogInformation("Processing payroll cycle {CycleId}", payrollCycleId);

        var cycle = await _context.PayrollCycles
            .Include(p => p.Payslips)
            .FirstOrDefaultAsync(p => p.Id == payrollCycleId && !p.IsDeleted);

        if (cycle == null)
            throw new InvalidOperationException("Payroll cycle not found");

        if (cycle.Status != PayrollCycleStatus.Draft && cycle.Status != PayrollCycleStatus.Processing)
            throw new InvalidOperationException($"Cannot process payroll in {cycle.Status} status");

        cycle.Status = PayrollCycleStatus.Processing;
        await _context.SaveChangesAsync();

        try
        {
            // Get employees to process
            var employeesQuery = _context.Employees.Where(e => !e.IsDeleted);

            if (dto.EmployeeIds != null && dto.EmployeeIds.Any())
                employeesQuery = employeesQuery.Where(e => dto.EmployeeIds.Contains(e.Id));

            var employees = await employeesQuery.ToListAsync();

            _logger.LogInformation("Processing payroll for {EmployeeCount} employees", employees.Count);

            // Delete existing payslips for this cycle (if reprocessing)
            var existingPayslips = cycle.Payslips.ToList();
            _context.Payslips.RemoveRange(existingPayslips);

            // Generate payslips
            var payslips = new List<Payslip>();
            foreach (var employee in employees)
            {
                var payslip = await GeneratePayslipAsync(employee, cycle.Month, cycle.Year, cycle.Id);
                payslips.Add(payslip);
            }

            _context.Payslips.AddRange(payslips);

            // Update cycle totals
            cycle.EmployeeCount = payslips.Count;
            cycle.TotalGrossSalary = payslips.Sum(p => p.TotalGrossSalary);
            cycle.TotalDeductions = payslips.Sum(p => p.TotalDeductions);
            cycle.TotalNetSalary = payslips.Sum(p => p.NetSalary);
            cycle.TotalNPFEmployee = payslips.Sum(p => p.NPF_Employee);
            cycle.TotalNPFEmployer = payslips.Sum(p => p.NPF_Employer);
            cycle.TotalNSFEmployee = payslips.Sum(p => p.NSF_Employee);
            cycle.TotalNSFEmployer = payslips.Sum(p => p.NSF_Employer);
            cycle.TotalCSGEmployee = payslips.Sum(p => p.CSG_Employee);
            cycle.TotalCSGEmployer = payslips.Sum(p => p.CSG_Employer);
            cycle.TotalPRGF = payslips.Sum(p => p.PRGF_Contribution);
            cycle.TotalTrainingLevy = payslips.Sum(p => p.TrainingLevy);
            cycle.TotalPAYE = payslips.Sum(p => p.PAYE_Tax);
            cycle.TotalOvertimePay = payslips.Sum(p => p.OvertimePay);

            cycle.Status = PayrollCycleStatus.Calculated;
            cycle.ProcessedBy = processedBy; // FIXED: Store username directly instead of parsing as Guid
            cycle.ProcessedAt = DateTime.UtcNow;
            cycle.UpdatedBy = processedBy;
            cycle.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(dto.Notes))
                cycle.Notes = dto.Notes;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Payroll processing completed. {PayslipCount} payslips generated", payslips.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payroll cycle {CycleId}", payrollCycleId);
            cycle.Status = PayrollCycleStatus.Draft;
            await _context.SaveChangesAsync();
            throw;
        }
    }

    private async Task<Payslip> GeneratePayslipAsync(Employee employee, int month, int year, Guid payrollCycleId)
    {
        var payslipNumber = $"PS-{year}-{month:D2}-{employee.EmployeeCode}";

        // Calculate basic salary
        var basicSalary = employee.BasicSalary;

        // Get additional allowances from SalaryComponents
        var allowances = await _salaryComponentService.GetTotalAllowancesAsync(employee.Id, month, year);
        var customDeductions = await _salaryComponentService.GetTotalDeductionsAsync(employee.Id, month, year);

        // Calculate overtime pay from attendance
        var overtimePay = await CalculateOvertimePayAsync(employee.Id, month, year);
        var overtimeHours = await GetOvertimeHoursAsync(employee.Id, month, year);

        // Calculate gross salary
        var grossSalary = basicSalary + allowances + overtimePay;

        // Calculate attendance-based deductions
        var (workingDays, actualDaysWorked, paidLeaveDays, unpaidLeaveDays) = await GetAttendanceDataAsync(employee.Id, month, year);
        var leaveDeductions = CalculateLeaveDeductions(basicSalary, workingDays, unpaidLeaveDays);

        // Adjust gross for unpaid leave
        grossSalary -= leaveDeductions;

        // Calculate statutory contributions
        var yearsOfService = CalculateYearsOfService(employee.JoiningDate);

        var csgEmployee = await CalculateCSGEmployeeAsync(grossSalary);
        var csgEmployer = await CalculateCSGEmployerAsync(grossSalary);
        var nsfEmployee = await CalculateNSFEmployeeAsync(basicSalary);
        var nsfEmployer = await CalculateNSFEmployerAsync(basicSalary);
        var npfEmployee = await CalculateNPFEmployeeAsync(basicSalary);
        var npfEmployer = await CalculateNPFEmployerAsync(basicSalary);
        var prgf = await CalculatePRGFAsync(grossSalary, yearsOfService, employee.JoiningDate);
        var trainingLevy = await CalculateTrainingLevyAsync(basicSalary);

        // Calculate PAYE
        var annualGross = grossSalary * 12;
        var annualStatutory = (csgEmployee + nsfEmployee) * 12;
        var paye = await CalculatePAYEAsync(annualGross, annualStatutory);

        // Total deductions
        var statutoryDeductions = csgEmployee + nsfEmployee + paye;
        var totalDeductions = statutoryDeductions + customDeductions + leaveDeductions;

        // Net salary
        var netSalary = grossSalary - totalDeductions;

        var payslip = new Payslip
        {
            Id = Guid.NewGuid(),
            PayrollCycleId = payrollCycleId,
            EmployeeId = employee.Id,
            Month = month,
            Year = year,
            PayslipNumber = payslipNumber,

            // Earnings
            BasicSalary = basicSalary,
            HousingAllowance = 0, // From SalaryComponents
            TransportAllowance = 0, // From SalaryComponents
            MealAllowance = 0, // From SalaryComponents
            MobileAllowance = 0, // From SalaryComponents
            OtherAllowances = allowances,
            OvertimeHours = overtimeHours,
            OvertimePay = overtimePay,
            TotalGrossSalary = grossSalary,

            // Attendance
            WorkingDays = workingDays,
            ActualDaysWorked = actualDaysWorked,
            PaidLeaveDays = paidLeaveDays,
            UnpaidLeaveDays = unpaidLeaveDays,
            LeaveDeductions = leaveDeductions,

            // Statutory - Employee
            NPF_Employee = npfEmployee,
            NSF_Employee = nsfEmployee,
            CSG_Employee = csgEmployee,
            PAYE_Tax = paye,

            // Statutory - Employer
            NPF_Employer = npfEmployer,
            NSF_Employer = nsfEmployer,
            CSG_Employer = csgEmployer,
            PRGF_Contribution = prgf,
            TrainingLevy = trainingLevy,

            // Other Deductions
            OtherDeductions = customDeductions,
            TotalDeductions = totalDeductions,

            // Net
            NetSalary = netSalary,

            // Payment
            PaymentStatus = PaymentStatus.Pending,
            BankAccountNumber = employee.BankAccountNumber,

            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        return payslip;
    }

    /// <summary>
    /// Approves or rejects a payroll cycle
    /// SECURITY: Only Admin and HR can approve payroll
    /// </summary>
    public async Task ApprovePayrollAsync(Guid payrollCycleId, ApprovePayrollDto dto, string approvedBy)
    {
        // AUTHORIZATION CHECK: Only HR/Admin can approve payroll
        if (!CanApprovePayroll())
        {
            LogAuthorizationFailure("ApprovePayroll", payrollCycleId);
            throw new UnauthorizedAccessException("Only HR and Admin roles can approve payroll");
        }

        var cycle = await _context.PayrollCycles
            .FirstOrDefaultAsync(p => p.Id == payrollCycleId && !p.IsDeleted);

        if (cycle == null)
            throw new InvalidOperationException("Payroll cycle not found");

        if (cycle.Status != PayrollCycleStatus.Calculated)
            throw new InvalidOperationException($"Cannot approve payroll in {cycle.Status} status");

        if (dto.IsApproved)
        {
            if (!dto.PaymentDate.HasValue)
                throw new InvalidOperationException("Payment date is required for approval");

            cycle.Status = PayrollCycleStatus.Approved;
            cycle.ApprovedBy = approvedBy; // FIXED: Store username directly instead of parsing as Guid
            cycle.ApprovedAt = DateTime.UtcNow;
            cycle.PaymentDate = dto.PaymentDate.Value;

            LogSensitiveOperation("ApprovePayroll", payrollCycleId, $"Approved, PaymentDate: {dto.PaymentDate.Value:yyyy-MM-dd}");
            _logger.LogInformation("Payroll cycle {CycleId} approved by {ApprovedBy}", payrollCycleId, approvedBy);
        }
        else
        {
            cycle.Status = PayrollCycleStatus.Draft;
            cycle.Notes = dto.Notes;

            LogSensitiveOperation("RejectPayroll", payrollCycleId, $"Rejected, Reason: {dto.Notes}");
            _logger.LogInformation("Payroll cycle {CycleId} rejected by {ApprovedBy}", payrollCycleId, approvedBy);
        }

        cycle.UpdatedBy = approvedBy;
        cycle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task CancelPayrollAsync(Guid payrollCycleId)
    {
        var cycle = await _context.PayrollCycles
            .FirstOrDefaultAsync(p => p.Id == payrollCycleId && !p.IsDeleted);

        if (cycle == null)
            throw new InvalidOperationException("Payroll cycle not found");

        if (cycle.Status == PayrollCycleStatus.Paid)
            throw new InvalidOperationException("Cannot cancel a paid payroll cycle");

        cycle.Status = PayrollCycleStatus.Cancelled;
        cycle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payroll cycle {CycleId} cancelled", payrollCycleId);
    }

    public async Task MarkPayrollAsPaidAsync(Guid payrollCycleId)
    {
        var cycle = await _context.PayrollCycles
            .Include(p => p.Payslips)
            .FirstOrDefaultAsync(p => p.Id == payrollCycleId && !p.IsDeleted);

        if (cycle == null)
            throw new InvalidOperationException("Payroll cycle not found");

        if (cycle.Status != PayrollCycleStatus.Approved)
            throw new InvalidOperationException("Payroll must be approved before marking as paid");

        foreach (var payslip in cycle.Payslips)
        {
            payslip.PaymentStatus = PaymentStatus.Paid;
            payslip.PaidAt = DateTime.UtcNow;
            payslip.PaymentMethod = "BankTransfer";
        }

        cycle.Status = PayrollCycleStatus.Paid;
        cycle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payroll cycle {CycleId} marked as paid", payrollCycleId);
    }

    // ==================== PAYSLIP OPERATIONS ====================

    /// <summary>
    /// Gets payslip details with role-based authorization
    /// SECURITY:
    /// - Admin/HR: Can view all payslips
    /// - Manager: Can view payslips for direct reports
    /// - Employee: Can view only their own payslips
    /// - Public: No access
    /// </summary>
    public async Task<PayslipDetailsDto?> GetPayslipAsync(Guid payslipId)
    {
        // AUTHORIZATION CHECK: Verify user can view this payslip
        if (!await CanViewPayslipAsync(payslipId))
        {
            LogAuthorizationFailure("GetPayslip", payslipId);
            throw new UnauthorizedAccessException("You are not authorized to view this payslip");
        }

        var payslip = await _context.Payslips
            .AsNoTracking()
            .Include(p => p.Employee)
            .ThenInclude(e => e.Department)
            .Where(p => p.Id == payslipId && !p.IsDeleted)
            .FirstOrDefaultAsync();

        if (payslip == null)
            return null;

        _logger.LogInformation("User {UserId} accessed payslip {PayslipId} for employee {EmployeeId}",
            _currentUserService.UserId, payslipId, payslip.EmployeeId);

        return new PayslipDetailsDto
        {
            Id = payslip.Id,
            PayrollCycleId = payslip.PayrollCycleId,
            EmployeeId = payslip.EmployeeId,
            EmployeeCode = payslip.Employee.EmployeeCode,
            EmployeeName = $"{payslip.Employee.FirstName} {payslip.Employee.LastName}",
            PayslipNumber = payslip.PayslipNumber,
            Department = payslip.Employee.Department?.Name ?? "N/A",
            JobTitle = payslip.Employee.JobTitle ?? "N/A",
            Month = payslip.Month,
            Year = payslip.Year,
            PeriodDisplay = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(payslip.Month)} {payslip.Year}",
            BasicSalary = payslip.BasicSalary,
            HousingAllowance = payslip.HousingAllowance,
            TransportAllowance = payslip.TransportAllowance,
            MealAllowance = payslip.MealAllowance,
            MobileAllowance = payslip.MobileAllowance,
            OtherAllowances = payslip.OtherAllowances,
            OvertimeHours = payslip.OvertimeHours,
            OvertimePay = payslip.OvertimePay,
            ThirteenthMonthBonus = payslip.ThirteenthMonthBonus,
            LeaveEncashment = payslip.LeaveEncashment,
            GratuityPayment = payslip.GratuityPayment,
            Commission = payslip.Commission,
            TotalGrossSalary = payslip.TotalGrossSalary,
            WorkingDays = payslip.WorkingDays,
            ActualDaysWorked = payslip.ActualDaysWorked,
            PaidLeaveDays = payslip.PaidLeaveDays,
            UnpaidLeaveDays = payslip.UnpaidLeaveDays,
            LeaveDeductions = payslip.LeaveDeductions,
            NPF_Employee = payslip.NPF_Employee,
            NSF_Employee = payslip.NSF_Employee,
            CSG_Employee = payslip.CSG_Employee,
            PAYE_Tax = payslip.PAYE_Tax,
            NPF_Employer = payslip.NPF_Employer,
            NSF_Employer = payslip.NSF_Employer,
            CSG_Employer = payslip.CSG_Employer,
            PRGF_Contribution = payslip.PRGF_Contribution,
            TrainingLevy = payslip.TrainingLevy,
            LoanDeduction = payslip.LoanDeduction,
            AdvanceDeduction = payslip.AdvanceDeduction,
            MedicalInsurance = payslip.MedicalInsurance,
            OtherDeductions = payslip.OtherDeductions,
            TotalDeductions = payslip.TotalDeductions,
            NetSalary = payslip.NetSalary,
            PaymentStatus = payslip.PaymentStatus,
            PaymentStatusDisplay = payslip.PaymentStatus.ToString(),
            PaidAt = payslip.PaidAt,
            PaymentMethod = payslip.PaymentMethod,
            PaymentReference = payslip.PaymentReference,
            BankAccountNumber = payslip.BankAccountNumber,
            Remarks = payslip.Remarks,
            IsDelivered = payslip.IsDelivered,
            DeliveredAt = payslip.DeliveredAt,
            CreatedAt = payslip.CreatedAt
        };
    }

    /// <summary>
    /// Gets all payslips for a cycle with role-based filtering
    /// SECURITY:
    /// - Admin/HR: Can view all payslips
    /// - Manager: Can view only their direct reports' payslips
    /// - Employee: Can view only their own payslip
    /// - Public: No access
    /// </summary>
    public async Task<List<PayslipDto>> GetPayslipsForCycleAsync(Guid payrollCycleId)
    {
        // AUTHORIZATION CHECK: Must be authenticated
        if (!_currentUserService.IsAuthenticated)
        {
            LogAuthorizationFailure("GetPayslipsForCycle", payrollCycleId);
            throw new UnauthorizedAccessException("Authentication required to view payslips");
        }

        var query = _context.Payslips
            .AsNoTracking()
            .Include(p => p.Employee)
            .Where(p => p.PayrollCycleId == payrollCycleId && !p.IsDeleted);

        // Apply role-based filtering
        var authorizedEmployeeIds = await GetAuthorizedEmployeeIdsAsync();

        // If not empty, filter by authorized employee IDs (non-HR/Admin users)
        if (authorizedEmployeeIds.Any())
        {
            query = query.Where(p => authorizedEmployeeIds.Contains(p.EmployeeId));
        }

        var payslips = await query
            .OrderBy(p => p.Employee.EmployeeCode)
            .ToListAsync();

        _logger.LogInformation("User {UserId} retrieved {Count} payslips for cycle {CycleId}",
            _currentUserService.UserId, payslips.Count, payrollCycleId);

        return payslips.Select(p => new PayslipDto
        {
            Id = p.Id,
            PayrollCycleId = p.PayrollCycleId,
            EmployeeId = p.EmployeeId,
            EmployeeCode = p.Employee.EmployeeCode,
            EmployeeName = $"{p.Employee.FirstName} {p.Employee.LastName}",
            PayslipNumber = p.PayslipNumber,
            Month = p.Month,
            Year = p.Year,
            PeriodDisplay = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(p.Month)} {p.Year}",
            BasicSalary = p.BasicSalary,
            TotalGrossSalary = p.TotalGrossSalary,
            TotalDeductions = p.TotalDeductions,
            NetSalary = p.NetSalary,
            PaymentStatus = p.PaymentStatus,
            PaymentStatusDisplay = p.PaymentStatus.ToString(),
            PaidAt = p.PaidAt,
            IsDelivered = p.IsDelivered
        }).ToList();
    }

    /// <summary>
    /// Gets all payslips for a specific employee with authorization
    /// SECURITY:
    /// - Admin/HR: Can view all employee payslips
    /// - Manager: Can view direct reports' payslips
    /// - Employee: Can view only their own payslips
    /// - Public: No access
    /// </summary>
    public async Task<List<EmployeePayslipDto>> GetEmployeePayslipsAsync(Guid employeeId, int? year = null)
    {
        // AUTHORIZATION CHECK: Verify user can view this employee's payslips
        if (!await CanViewEmployeePayslipsAsync(employeeId))
        {
            LogAuthorizationFailure("GetEmployeePayslips", employeeId);
            throw new UnauthorizedAccessException("You are not authorized to view this employee's payslips");
        }

        var query = _context.Payslips
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId && !p.IsDeleted);

        if (year.HasValue)
            query = query.Where(p => p.Year == year.Value);

        var payslips = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ToListAsync();

        _logger.LogInformation("User {UserId} retrieved {Count} payslips for employee {EmployeeId}",
            _currentUserService.UserId, payslips.Count, employeeId);

        return payslips.Select(p => new EmployeePayslipDto
        {
            Id = p.Id,
            PayslipNumber = p.PayslipNumber,
            Month = p.Month,
            Year = p.Year,
            PeriodDisplay = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(p.Month)} {p.Year}",
            TotalGrossSalary = p.TotalGrossSalary,
            TotalDeductions = p.TotalDeductions,
            NetSalary = p.NetSalary,
            PaymentStatus = p.PaymentStatus,
            PaymentStatusDisplay = p.PaymentStatus.ToString(),
            PaidAt = p.PaidAt,
            IsDelivered = p.IsDelivered,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task RegeneratePayslipAsync(Guid payslipId, string updatedBy)
    {
        var payslip = await _context.Payslips
            .Include(p => p.Employee)
            .Include(p => p.PayrollCycle)
            .FirstOrDefaultAsync(p => p.Id == payslipId && !p.IsDeleted);

        if (payslip == null)
            throw new InvalidOperationException("Payslip not found");

        if (payslip.PaymentStatus == PaymentStatus.Paid)
            throw new InvalidOperationException("Cannot regenerate a paid payslip");

        // Generate new payslip
        var newPayslip = await GeneratePayslipAsync(payslip.Employee, payslip.Month, payslip.Year, payslip.PayrollCycleId);
        newPayslip.Id = payslip.Id; // Keep same ID
        newPayslip.PayslipNumber = payslip.PayslipNumber; // Keep same number

        // Update properties
        _context.Entry(payslip).CurrentValues.SetValues(newPayslip);
        payslip.UpdatedBy = updatedBy;
        payslip.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Payslip {PayslipId} regenerated", payslipId);
    }

    // ==================== MAURITIUS STATUTORY CALCULATIONS ====================

    public Task<decimal> CalculateCSGEmployeeAsync(decimal monthlySalary)
    {
        var csg = monthlySalary <= CSG_THRESHOLD
            ? monthlySalary * CSG_EMPLOYEE_LOW
            : monthlySalary * CSG_EMPLOYEE_HIGH;

        return Task.FromResult(Math.Round(csg, 2));
    }

    public Task<decimal> CalculateCSGEmployerAsync(decimal monthlySalary)
    {
        var csg = monthlySalary <= CSG_THRESHOLD
            ? monthlySalary * CSG_EMPLOYER_LOW
            : monthlySalary * CSG_EMPLOYER_HIGH;

        return Task.FromResult(Math.Round(csg, 2));
    }

    public Task<decimal> CalculateNSFEmployeeAsync(decimal basicSalary)
    {
        return Task.FromResult(Math.Round(basicSalary * NSF_EMPLOYEE, 2));
    }

    public Task<decimal> CalculateNSFEmployerAsync(decimal basicSalary)
    {
        return Task.FromResult(Math.Round(basicSalary * NSF_EMPLOYER, 2));
    }

    public Task<decimal> CalculateNPFEmployeeAsync(decimal basicSalary)
    {
        return Task.FromResult(Math.Round(basicSalary * NPF_EMPLOYEE, 2));
    }

    public Task<decimal> CalculateNPFEmployerAsync(decimal basicSalary)
    {
        return Task.FromResult(Math.Round(basicSalary * NPF_EMPLOYER, 2));
    }

    public Task<decimal> CalculatePRGFAsync(decimal grossSalary, int yearsOfService, DateTime joiningDate)
    {
        // PRGF only applies to employees hired after January 1, 2020
        if (joiningDate < PRGF_IMPLEMENTATION_DATE)
            return Task.FromResult(0m);

        decimal rate = yearsOfService switch
        {
            <= 5 => PRGF_RATE_0_5_YEARS,
            <= 10 => PRGF_RATE_6_10_YEARS,
            _ => PRGF_RATE_ABOVE_10_YEARS
        };

        return Task.FromResult(Math.Round(grossSalary * rate, 2));
    }

    public Task<decimal> CalculateTrainingLevyAsync(decimal basicSalary)
    {
        return Task.FromResult(Math.Round(basicSalary * TRAINING_LEVY, 2));
    }

    public Task<decimal> CalculatePAYEAsync(decimal annualGrossSalary, decimal annualDeductions)
    {
        var taxableIncome = annualGrossSalary - annualDeductions;

        // Tax-free threshold
        if (taxableIncome <= PAYE_THRESHOLD)
            return Task.FromResult(0m);

        decimal annualTax = 0m;

        if (taxableIncome <= PAYE_BRACKET_1_LIMIT)
        {
            // First bracket: 10%
            annualTax = (taxableIncome - PAYE_THRESHOLD) * PAYE_RATE_1;
        }
        else if (taxableIncome <= PAYE_BRACKET_2_LIMIT)
        {
            // Second bracket: 12%
            var firstBracketTax = (PAYE_BRACKET_1_LIMIT - PAYE_THRESHOLD) * PAYE_RATE_1;
            var secondBracketTax = (taxableIncome - PAYE_BRACKET_1_LIMIT) * PAYE_RATE_2;
            annualTax = firstBracketTax + secondBracketTax;
        }
        else
        {
            // Third bracket: 20%
            var firstBracketTax = (PAYE_BRACKET_1_LIMIT - PAYE_THRESHOLD) * PAYE_RATE_1;
            var secondBracketTax = (PAYE_BRACKET_2_LIMIT - PAYE_BRACKET_1_LIMIT) * PAYE_RATE_2;
            var thirdBracketTax = (taxableIncome - PAYE_BRACKET_2_LIMIT) * PAYE_RATE_3;
            annualTax = firstBracketTax + secondBracketTax + thirdBracketTax;
        }

        // Return monthly PAYE
        var monthlyPaye = annualTax / 12;
        return Task.FromResult(Math.Round(monthlyPaye, 2));
    }

    // ==================== EARNINGS CALCULATIONS ====================

    public async Task<PayrollResult> CalculatePayrollFromTimesheetsAsync(Guid employeeId, DateTime periodStart, DateTime periodEnd)
    {
        _logger.LogInformation("Calculating payroll for employee {EmployeeId} from timesheets for period {Start} to {End}",
            employeeId, periodStart, periodEnd);

        var result = new PayrollResult
        {
            EmployeeId = employeeId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CalculatedAt = DateTime.UtcNow,
            Warnings = new List<string>()
        };

        // STEP 1: Get employee details
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (employee == null)
        {
            throw new InvalidOperationException($"Employee {employeeId} not found");
        }

        result.EmployeeCode = employee.EmployeeCode;
        result.EmployeeName = $"{employee.FirstName} {employee.LastName}";
        result.Department = employee.Department?.Name ?? string.Empty;
        result.JobTitle = employee.JobTitle ?? string.Empty;
        result.BasicSalary = employee.BasicSalary;

        // STEP 2: Get ALL approved timesheets for the period
        var timesheets = await _context.Timesheets
            .Include(t => t.Entries)
            .Where(t => t.EmployeeId == employeeId &&
                       t.Status == TimesheetStatus.Approved &&
                       t.PeriodStart >= periodStart &&
                       t.PeriodEnd <= periodEnd &&
                       !t.IsDeleted)
            .ToListAsync();

        if (!timesheets.Any())
        {
            result.HasWarnings = true;
            result.Warnings.Add("No approved timesheets found for this period. Payroll calculation may be incomplete.");
        }

        result.TimesheetIds = timesheets.Select(t => t.Id).ToList();
        result.TimesheetsProcessed = timesheets.Count;

        // STEP 3: Calculate total hours from timesheets
        result.TotalRegularHours = timesheets.Sum(t => t.TotalRegularHours);
        result.TotalOvertimeHours = timesheets.Sum(t => t.TotalOvertimeHours);
        result.TotalHolidayHours = timesheets.Sum(t => t.TotalHolidayHours);

        // Calculate leave hours from entries
        var allEntries = timesheets.SelectMany(t => t.Entries).ToList();
        result.TotalLeaveHours = allEntries.Sum(e => e.SickLeaveHours + e.AnnualLeaveHours);

        result.TotalPayableHours = result.TotalRegularHours + result.TotalOvertimeHours +
                                    result.TotalHolidayHours + result.TotalLeaveHours;

        // STEP 4: Calculate working days information
        var totalDays = (periodEnd - periodStart).Days + 1;
        var workingDaysInPeriod = 0;
        var currentDate = periodStart;
        while (currentDate <= periodEnd)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                workingDaysInPeriod++;
            currentDate = currentDate.AddDays(1);
        }

        result.WorkingDays = workingDaysInPeriod;

        // Get leave days from timesheet entries (reuse allEntries from above)
        result.PaidLeaveDays = allEntries.Count(e => e.DayType == DayType.AnnualLeave ||
                                                     e.DayType == DayType.SickLeave ||
                                                     e.DayType == DayType.CasualLeave);
        result.UnpaidLeaveDays = allEntries.Count(e => e.DayType == DayType.UnpaidLeave);
        result.ActualDaysWorked = allEntries.Count(e => e.DayType == DayType.Regular &&
                                                        (e.RegularHours > 0 || e.OvertimeHours > 0));

        // STEP 5: Calculate hourly rate
        result.HourlyRate = employee.BasicSalary / STANDARD_MONTHLY_HOURS;

        // STEP 6: Calculate gross pay components
        result.RegularPay = Math.Round(result.TotalRegularHours * result.HourlyRate, 2);

        // Overtime at 1.5x rate (standard)
        result.OvertimePay = Math.Round(result.TotalOvertimeHours * result.HourlyRate * 1.5m, 2);

        // Holiday pay at 2x rate
        result.HolidayPay = Math.Round(result.TotalHolidayHours * result.HourlyRate * 2m, 2);

        // Leave pay at regular rate (already paid through basic salary)
        result.LeavePay = Math.Round(result.TotalLeaveHours * result.HourlyRate, 2);

        // STEP 7: Get allowances from employee record
        result.HousingAllowance = employee.HousingAllowance ?? 0m;
        result.TransportAllowance = employee.TransportAllowance ?? 0m;
        result.MealAllowance = employee.MealAllowance ?? 0m;
        result.MobileAllowance = 0m; // Not available in Employee entity
        result.OtherAllowances = 0m; // Not available in Employee entity

        // Special payments (set to 0 for now - these are calculated separately)
        result.ThirteenthMonthBonus = 0m;
        result.LeaveEncashment = 0m;
        result.GratuityPayment = 0m;
        result.Commission = 0m;

        // STEP 8: Calculate total gross salary
        result.TotalGrossSalary = result.RegularPay +
                                  result.OvertimePay +
                                  result.HolidayPay +
                                  result.LeavePay +
                                  result.HousingAllowance +
                                  result.TransportAllowance +
                                  result.MealAllowance +
                                  result.MobileAllowance +
                                  result.OtherAllowances +
                                  result.ThirteenthMonthBonus +
                                  result.LeaveEncashment +
                                  result.GratuityPayment +
                                  result.Commission;

        // STEP 9: Calculate Mauritius statutory deductions
        var deductions = new MauritiusDeductions();

        // CSG (employee and employer)
        var monthlySalary = result.TotalGrossSalary;
        deductions.IsBelowCSG_Threshold = monthlySalary <= CSG_THRESHOLD;
        deductions.CSG_EmployeeRate = deductions.IsBelowCSG_Threshold ? CSG_EMPLOYEE_LOW : CSG_EMPLOYEE_HIGH;
        deductions.CSG_EmployerRate = deductions.IsBelowCSG_Threshold ? CSG_EMPLOYER_LOW : CSG_EMPLOYER_HIGH;

        deductions.CSG_Employee = await CalculateCSGEmployeeAsync(monthlySalary);
        deductions.CSG_Employer = await CalculateCSGEmployerAsync(monthlySalary);

        // NSF (employee and employer)
        deductions.NSF_Employee = await CalculateNSFEmployeeAsync(employee.BasicSalary);
        deductions.NSF_Employer = await CalculateNSFEmployerAsync(employee.BasicSalary);

        // NPF (legacy - only if hired before 2020)
        if (employee.JoiningDate < PRGF_IMPLEMENTATION_DATE)
        {
            deductions.NPF_Employee = await CalculateNPFEmployeeAsync(employee.BasicSalary);
            deductions.NPF_Employer = await CalculateNPFEmployerAsync(employee.BasicSalary);
        }

        // PRGF (only if hired after Jan 2020)
        var yearsOfService = CalculateYearsOfService(employee.JoiningDate, DateTime.UtcNow);
        deductions.PRGF_Contribution = await CalculatePRGFAsync(monthlySalary, yearsOfService, employee.JoiningDate);

        if (employee.JoiningDate >= PRGF_IMPLEMENTATION_DATE)
        {
            if (yearsOfService <= 5)
                deductions.PRGF_Rate = PRGF_RATE_0_5_YEARS;
            else if (yearsOfService <= 10)
                deductions.PRGF_Rate = PRGF_RATE_6_10_YEARS;
            else
                deductions.PRGF_Rate = PRGF_RATE_ABOVE_10_YEARS;
        }

        // Training Levy
        deductions.TrainingLevy = await CalculateTrainingLevyAsync(employee.BasicSalary);

        // PAYE Tax
        var annualGross = monthlySalary * 12;
        var annualStatutoryDeductions = (deductions.CSG_Employee + deductions.NSF_Employee + deductions.NPF_Employee) * 12;
        deductions.PAYE_Tax = await CalculatePAYEAsync(annualGross, annualStatutoryDeductions);

        // Determine tax bracket for display
        var taxableIncome = annualGross - annualStatutoryDeductions;
        if (taxableIncome <= PAYE_THRESHOLD)
            deductions.TaxBracket = "0% (Below MUR 390,000)";
        else if (taxableIncome <= PAYE_BRACKET_1_LIMIT)
            deductions.TaxBracket = "10% (MUR 390,001 - 550,000)";
        else if (taxableIncome <= PAYE_BRACKET_2_LIMIT)
            deductions.TaxBracket = "12% (MUR 550,001 - 650,000)";
        else
            deductions.TaxBracket = "20% (Above MUR 650,000)";

        // Employee contributions (deducted from salary)
        deductions.TotalEmployeeContributions = deductions.NPF_Employee +
                                                 deductions.NSF_Employee +
                                                 deductions.CSG_Employee +
                                                 deductions.PAYE_Tax;

        // Employer contributions (not deducted, but recorded)
        deductions.TotalEmployerContributions = deductions.NPF_Employer +
                                                 deductions.NSF_Employer +
                                                 deductions.CSG_Employer +
                                                 deductions.PRGF_Contribution +
                                                 deductions.TrainingLevy;

        result.StatutoryDeductions = deductions;

        // STEP 10: Other deductions (set to 0 for now - would come from employee records)
        result.LeaveDeductions = 0m; // Would calculate from unpaid leave days
        result.LoanDeduction = 0m;
        result.AdvanceDeduction = 0m;
        result.MedicalInsurance = 0m;
        result.OtherDeductions = 0m;

        // STEP 11: Calculate total deductions
        result.TotalDeductions = deductions.TotalEmployeeContributions +
                                 result.LeaveDeductions +
                                 result.LoanDeduction +
                                 result.AdvanceDeduction +
                                 result.MedicalInsurance +
                                 result.OtherDeductions;

        // STEP 12: Calculate net salary
        result.NetSalary = result.TotalGrossSalary - result.TotalDeductions;

        _logger.LogInformation("Payroll calculated successfully for employee {EmployeeId}. Gross: {Gross}, Net: {Net}",
            employeeId, result.TotalGrossSalary, result.NetSalary);

        return result;
    }

    public async Task<decimal> CalculateOvertimePayAsync(Guid employeeId, int month, int year)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (employee == null)
        {
            _logger.LogWarning("SECURITY: Employee {EmployeeId} not found in current tenant context", employeeId);
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found or access denied");
        }

        var hourlyRate = employee.BasicSalary / STANDARD_MONTHLY_HOURS;

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var attendances = await _context.Attendances
            .Where(a => a.EmployeeId == employeeId)
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .Where(a => !a.IsDeleted)
            .ToListAsync();

        decimal totalOvertimePay = 0m;

        foreach (var attendance in attendances)
        {
            if (attendance.OvertimeHours > 0 && attendance.OvertimeRate.HasValue)
            {
                // OvertimeRate comes from SECTOR RULES (1.5x, 2x, 3x)
                totalOvertimePay += attendance.OvertimeHours * hourlyRate * attendance.OvertimeRate.Value;
            }
        }

        return Math.Round(totalOvertimePay, 2);
    }

    public async Task<decimal> Calculate13thMonthBonusAsync(Guid employeeId, int year)
    {
        // SECURITY: Validate employee exists in current tenant context (CRITICAL FIX - IDOR Prevention)
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (employee == null)
        {
            _logger.LogWarning("SECURITY: Employee {EmployeeId} not found in current tenant context during 13th month bonus calculation", employeeId);
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found or access denied");
        }

        var payslips = await _context.Payslips
            .Where(p => p.EmployeeId == employeeId)
            .Where(p => p.Year == year)
            .Where(p => !p.IsDeleted)
            .ToListAsync();

        var annualGross = payslips.Sum(p => p.TotalGrossSalary);

        _logger.LogInformation("Calculated 13th month bonus for employee {EmployeeId}, year {Year}: {Amount}",
            employeeId, year, annualGross / 12);

        return Math.Round(annualGross / 12, 2);
    }

    public async Task<decimal> CalculateGratuityAsync(Guid employeeId, DateTime resignationDate)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (employee == null)
        {
            _logger.LogWarning("SECURITY: Employee {EmployeeId} not found in current tenant context", employeeId);
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found or access denied");
        }

        // Only for employees hired before PRGF (Jan 2020)
        if (employee.JoiningDate >= PRGF_IMPLEMENTATION_DATE)
            return 0m;

        var yearsOfService = CalculateYearsOfService(employee.JoiningDate, resignationDate);

        if (yearsOfService < 1)
            return 0m;

        // Mauritius law: 15 days per year of service
        var dailyRate = employee.BasicSalary / 26m; // Working days per month
        var gratuityDays = yearsOfService * 15m;

        return Math.Round(gratuityDays * dailyRate, 2);
    }

    public async Task<decimal> CalculateLeaveEncashmentAsync(Guid employeeId)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (employee == null)
        {
            _logger.LogWarning("SECURITY: Employee {EmployeeId} not found in current tenant context", employeeId);
            throw new KeyNotFoundException($"Employee with ID {employeeId} not found or access denied");
        }

        var currentYear = DateTime.UtcNow.Year;
        var leaveBalance = await _context.LeaveBalances
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.EmployeeId == employeeId)
            .Where(lb => lb.Year == currentYear)
            .Where(lb => lb.LeaveType!.TypeCode == LeaveTypeEnum.AnnualLeave)
            .FirstOrDefaultAsync();

        if (leaveBalance == null || leaveBalance.AvailableDays <= 0)
            return 0m;

        var dailyRate = employee.BasicSalary / 26m;
        return Math.Round(leaveBalance.AvailableDays * dailyRate, 2);
    }

    // ==================== REPORTS & EXPORTS ====================

    /// <summary>
    /// Gets payroll summary with role-based filtering
    /// SECURITY:
    /// - Admin/HR: Full summary with all employees
    /// - Manager: Summary filtered to direct reports only
    /// - Employee: Summary filtered to self only
    /// - Public: No access
    /// </summary>
    public async Task<PayrollSummaryDto> GetPayrollSummaryAsync(Guid payrollCycleId)
    {
        // AUTHORIZATION CHECK: Must be authenticated
        if (!_currentUserService.IsAuthenticated)
        {
            LogAuthorizationFailure("GetPayrollSummary", payrollCycleId);
            throw new UnauthorizedAccessException("Authentication required to view payroll summary");
        }

        var cycle = await _context.PayrollCycles
            .AsNoTracking()
            .Include(p => p.Payslips)
            .ThenInclude(ps => ps.Employee)
            .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(p => p.Id == payrollCycleId && !p.IsDeleted);

        if (cycle == null)
            throw new InvalidOperationException("Payroll cycle not found");

        // Apply role-based filtering to payslips
        var authorizedEmployeeIds = await GetAuthorizedEmployeeIdsAsync();
        var filteredPayslips = cycle.Payslips.AsEnumerable();

        // If not empty, filter by authorized employee IDs (non-HR/Admin users)
        if (authorizedEmployeeIds.Any())
        {
            filteredPayslips = filteredPayslips.Where(p => authorizedEmployeeIds.Contains(p.EmployeeId));
        }

        var payslipList = filteredPayslips.ToList();

        _logger.LogInformation("User {UserId} accessed payroll summary for cycle {CycleId}, viewing {Count} payslips",
            _currentUserService.UserId, payrollCycleId, payslipList.Count);

        var summary = new PayrollSummaryDto
        {
            PayrollCycleId = cycle.Id,
            Month = cycle.Month,
            Year = cycle.Year,
            PeriodDisplay = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(cycle.Month)} {cycle.Year}",
            TotalEmployees = payslipList.Count,
            ProcessedEmployees = payslipList.Count,
            PaidEmployees = payslipList.Count(p => p.PaymentStatus == PaymentStatus.Paid),
            PendingPayments = payslipList.Count(p => p.PaymentStatus == PaymentStatus.Pending),
            TotalBasicSalary = payslipList.Sum(p => p.BasicSalary),
            TotalAllowances = payslipList.Sum(p => p.OtherAllowances),
            TotalOvertimePay = payslipList.Sum(p => p.OvertimePay),
            TotalGrossSalary = payslipList.Sum(p => p.TotalGrossSalary),
            TotalNPFEmployee = payslipList.Sum(p => p.NPF_Employee),
            TotalNSFEmployee = payslipList.Sum(p => p.NSF_Employee),
            TotalCSGEmployee = payslipList.Sum(p => p.CSG_Employee),
            TotalPAYE = payslipList.Sum(p => p.PAYE_Tax),
            TotalStatutoryDeductions = payslipList.Sum(p => p.CSG_Employee + p.NSF_Employee + p.PAYE_Tax),
            TotalNPFEmployer = payslipList.Sum(p => p.NPF_Employer),
            TotalNSFEmployer = payslipList.Sum(p => p.NSF_Employer),
            TotalCSGEmployer = payslipList.Sum(p => p.CSG_Employer),
            TotalPRGF = payslipList.Sum(p => p.PRGF_Contribution),
            TotalTrainingLevy = payslipList.Sum(p => p.TrainingLevy),
            TotalEmployerContributions = payslipList.Sum(p => p.CSG_Employer + p.NSF_Employer + p.PRGF_Contribution + p.TrainingLevy),
            TotalDeductions = payslipList.Sum(p => p.TotalDeductions),
            TotalNetSalary = payslipList.Sum(p => p.NetSalary),
            TotalCostToCompany = payslipList.Sum(p => p.TotalGrossSalary + p.CSG_Employer + p.NSF_Employer + p.PRGF_Contribution + p.TrainingLevy)
        };

        // Department breakdown (using filtered payslips)
        var departmentGroups = payslipList
            .GroupBy(p => p.Employee.Department?.Name ?? "Unassigned")
            .Select(g => new DepartmentPayrollSummary
            {
                DepartmentName = g.Key,
                EmployeeCount = g.Count(),
                TotalGrossSalary = g.Sum(p => p.TotalGrossSalary),
                TotalDeductions = g.Sum(p => p.TotalDeductions),
                TotalNetSalary = g.Sum(p => p.NetSalary),
                TotalCostToCompany = g.Sum(p => p.TotalGrossSalary + p.CSG_Employer + p.NSF_Employer + p.PRGF_Contribution + p.TrainingLevy)
            })
            .ToList();

        summary.DepartmentBreakdown = departmentGroups;

        return summary;
    }

    public async Task<byte[]> GenerateBankTransferFileAsync(Guid payrollCycleId)
    {
        var payslips = await _context.Payslips
            .AsNoTracking()
            .Include(p => p.Employee)
            .Where(p => p.PayrollCycleId == payrollCycleId && !p.IsDeleted)
            .Where(p => !string.IsNullOrEmpty(p.BankAccountNumber))
            .OrderBy(p => p.Employee.EmployeeCode)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("EmployeeCode,EmployeeName,BankName,AccountNumber,NetSalary,Reference");

        foreach (var payslip in payslips)
        {
            sb.AppendLine($"{payslip.Employee.EmployeeCode}," +
                         $"\"{payslip.Employee.FirstName} {payslip.Employee.LastName}\"," +
                         $"{payslip.Employee.BankName ?? "N/A"}," +
                         $"{payslip.BankAccountNumber}," +
                         $"{payslip.NetSalary:F2}," +
                         $"{payslip.PayslipNumber}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> GeneratePayslipPdfAsync(Guid payslipId)
    {
        _logger.LogInformation("Generating PDF for payslip {PayslipId}", payslipId);

        try
        {
            // Fetch payslip with all related data
            var payslip = await _context.Payslips
                .AsNoTracking()
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .Include(p => p.PayrollCycle)
                .FirstOrDefaultAsync(p => p.Id == payslipId && !p.IsDeleted);

            if (payslip == null)
            {
                _logger.LogError("Payslip {PayslipId} not found", payslipId);
                throw new InvalidOperationException($"Payslip with ID {payslipId} not found");
            }

            if (payslip.Employee == null)
            {
                _logger.LogError("Employee data missing for payslip {PayslipId}", payslipId);
                throw new InvalidOperationException("Employee data is missing for this payslip");
            }

            // Get company name from tenant service
            var tenantId = _tenantService.GetCurrentTenantId();
            var companyName = "Company Name"; // Default fallback

            // Note: In production, tenant info would be fetched from MasterDbContext
            // For now, using a fallback value as MasterDbContext is not injected here

            // Generate PDF using QuestPDF
            var pdfBytes = _pdfGenerator.GeneratePayslipPdf(payslip, payslip.Employee, companyName);

            _logger.LogInformation("PDF generated successfully for payslip {PayslipId}, size: {Size} bytes",
                payslipId, pdfBytes.Length);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for payslip {PayslipId}", payslipId);
            throw;
        }
    }

    // ==================== HELPER METHODS ====================

    private async Task<decimal> GetOvertimeHoursAsync(Guid employeeId, int month, int year)
    {
        // SECURITY: Defense-in-depth - Validate employee exists before querying attendance
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (!employeeExists)
        {
            _logger.LogWarning("SECURITY: Attempt to query overtime for non-existent employee {EmployeeId}", employeeId);
            return 0m; // Fail-safe: Return 0 instead of throwing to avoid breaking payroll generation
        }

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var totalOvertimeHours = await _context.Attendances
            .Where(a => a.EmployeeId == employeeId)
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .Where(a => !a.IsDeleted)
            .SumAsync(a => a.OvertimeHours);

        return totalOvertimeHours;
    }

    private async Task<(int workingDays, int actualDaysWorked, decimal paidLeaveDays, decimal unpaidLeaveDays)> GetAttendanceDataAsync(
        Guid employeeId, int month, int year)
    {
        // SECURITY: Defense-in-depth - Validate employee exists before querying attendance and leave data
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (!employeeExists)
        {
            _logger.LogWarning("SECURITY: Attempt to query attendance data for non-existent employee {EmployeeId}", employeeId);
            return (0, 0, 0m, 0m); // Fail-safe: Return zeros to avoid breaking payroll generation
        }

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Calculate working days (excluding Sundays)
        var workingDays = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Sunday)
                workingDays++;
        }

        var attendances = await _context.Attendances
            .Where(a => a.EmployeeId == employeeId)
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .Where(a => !a.IsDeleted)
            .ToListAsync();

        var actualDaysWorked = attendances.Count(a => a.Status == AttendanceStatus.Present);

        // Get leave data
        var leaves = await _context.LeaveApplications
            .Where(la => la.EmployeeId == employeeId)
            .Where(la => la.Status == LeaveStatus.Approved)
            .Where(la => la.StartDate <= endDate && la.EndDate >= startDate)
            .Include(la => la.LeaveType)
            .ToListAsync();

        decimal paidLeaveDays = leaves
            .Where(la => la.LeaveType != null && la.LeaveType.IsPaid)
            .Sum(la => la.TotalDays);

        decimal unpaidLeaveDays = leaves
            .Where(la => la.LeaveType != null && !la.LeaveType.IsPaid)
            .Sum(la => la.TotalDays);

        return (workingDays, actualDaysWorked, paidLeaveDays, unpaidLeaveDays);
    }

    private decimal CalculateLeaveDeductions(decimal basicSalary, int workingDays, decimal unpaidLeaveDays)
    {
        if (unpaidLeaveDays <= 0 || workingDays <= 0)
            return 0m;

        var dailyRate = basicSalary / workingDays;
        return Math.Round(dailyRate * unpaidLeaveDays, 2);
    }

    private int CalculateYearsOfService(DateTime joiningDate, DateTime? endDate = null)
    {
        var compareDate = endDate ?? DateTime.UtcNow;
        var years = compareDate.Year - joiningDate.Year;

        if (compareDate.Month < joiningDate.Month ||
            (compareDate.Month == joiningDate.Month && compareDate.Day < joiningDate.Day))
        {
            years--;
        }

        return Math.Max(0, years);
    }
}
