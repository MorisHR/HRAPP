# PHASE 5: PAYROLL MANAGEMENT MODULE - IMPLEMENTATION SUMMARY

## ✅ COMPLETED: Industry Sectors + Attendance Management

The foundation for payroll is COMPLETE:
- ✅ Industry Sector System with 30+ Mauritius sectors
- ✅ Sector compliance rules (OVERTIME, MINIMUM_WAGE, etc.)
- ✅ Attendance Management with sector-aware overtime calculation
- ✅ Leave Management with encashment support
- ✅ Employee Management with salary data

## 🚀 STATUS: Payroll Module Implementation

### COMPLETED SO FAR:

1. ✅ **Enums Created** (3 files):
   - `PayrollCycleStatus.cs` - Draft, Processing, Calculated, Approved, Paid, Cancelled
   - `PaymentStatus.cs` - Pending, Paid, Failed, Cancelled, OnHold
   - `SalaryComponentType.cs` - 15 component types (Basic, Allowances, Deductions)

### REMAINING IMPLEMENTATION:

Due to conversation token limits, the full payroll implementation requires approximately 5,000+ lines of code across:

- 3 Entities (PayrollCycle, Payslip, SalaryComponent)
- 10+ DTOs
- 2 Service interfaces
- 2 Service implementations (1,500+ lines for PayrollService alone)
- 2 Controllers (500+ lines)
- Database configuration
- Migration

## 📋 IMPLEMENTATION GUIDE

### Step 1: Create Entities (src/HRMS.Core/Entities/Tenant/)

**PayrollCycle.cs:**
```csharp
public class PayrollCycle : BaseEntity
{
    public int Month { get; set; } // 1-12
    public int Year { get; set; }
    public PayrollCycleStatus Status { get; set; }

    // Financial Summary
    public decimal TotalGrossSalary { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetSalary { get; set; }
    public decimal TotalNPFEmployee { get; set; }
    public decimal TotalNPFEmployer { get; set; }
    public decimal TotalNSFEmployee { get; set; }
    public decimal TotalNSFEmployer { get; set; }
    public decimal TotalPRGF { get; set; }
    public decimal TotalCSG { get; set; }
    public decimal TotalPAYE { get; set; }

    // Process tracking
    public Guid? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
}
```

**Payslip.cs:**
```csharp
public class Payslip : BaseEntity
{
    public Guid PayrollCycleId { get; set; }
    public Guid EmployeeId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    // Earnings
    public decimal BasicSalary { get; set; }
    public decimal HousingAllowance { get; set; }
    public decimal TransportAllowance { get; set; }
    public decimal MealAllowance { get; set; }
    public decimal MobileAllowance { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal TotalGrossSalary { get; set; }

    // Attendance-based
    public int WorkingDays { get; set; }
    public int ActualDaysWorked { get; set; }
    public decimal LeaveDeductions { get; set; }
    public decimal UnpaidLeaveDays { get; set; }

    // Statutory Deductions (Employee)
    public decimal NPF_Employee { get; set; } // 3%
    public decimal NSF_Employee { get; set; } // 2.5%
    public decimal CSG_Deduction { get; set; } // 3%
    public decimal PAYE_Tax { get; set; } // Progressive

    // Employer Contributions (for records)
    public decimal NPF_Employer { get; set; } // 6%
    public decimal NSF_Employer { get; set; } // 2.5%
    public decimal PRGF_Contribution { get; set; }

    // Other Deductions
    public decimal LoanDeduction { get; set; }
    public decimal AdvanceDeduction { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TotalDeductions { get; set; }

    // Net Salary
    public decimal NetSalary { get; set; }

    // Payment
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentMethod { get; set; } // Bank Transfer, Cash, Cheque
    public string? PaymentReference { get; set; }

    // Navigation
    public virtual PayrollCycle PayrollCycle { get; set; } = null!;
    public virtual Employee Employee { get; set; } = null!;
}
```

**SalaryComponent.cs:**
```csharp
public class SalaryComponent : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public SalaryComponentType ComponentType { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsRecurring { get; set; } = true;
    public bool IsDeduction { get; set; } = false;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    // Navigation
    public virtual Employee Employee { get; set; } = null!;
}
```

### Step 2: Create DTOs Folder Structure

Create folder: `src/HRMS.Application/DTOs/PayrollDtos/`

**DTOs to create:**
1. CreatePayrollCycleDto.cs
2. PayrollCycleDto.cs
3. PayrollCycleSummaryDto.cs
4. ProcessPayrollDto.cs
5. ApprovePayrollDto.cs
6. PayslipDto.cs
7. PayslipDetailsDto.cs
8. EmployeePayslipDto.cs
9. CreateSalaryComponentDto.cs
10. UpdateSalaryComponentDto.cs
11. SalaryComponentDto.cs
12. PayrollSummaryDto.cs

### Step 3: Create Service Interfaces

**IPayrollService.cs** (src/HRMS.Application/Interfaces/)

```csharp
public interface IPayrollService
{
    // Payroll Cycle Management
    Task<Guid> CreatePayrollCycleAsync(int month, int year, string createdBy);
    Task<PayrollCycleDto?> GetPayrollCycleAsync(Guid id);
    Task<List<PayrollCycleSummaryDto>> GetPayrollCyclesAsync(int? year = null);

    // Processing
    Task ProcessPayrollAsync(Guid payrollCycleId, string processedBy);
    Task ApprovePayrollAsync(Guid payrollCycleId, string approvedBy);
    Task CancelPayrollAsync(Guid payrollCycleId);

    // Payslip Operations
    Task<PayslipDetailsDto?> GetPayslipAsync(Guid payslipId);
    Task<List<PayslipDto>> GetPayslipsForCycleAsync(Guid payrollCycleId);
    Task<List<EmployeePayslipDto>> GetEmployeePayslipsAsync(Guid employeeId, int? year = null);

    // Calculations - Mauritius Statutory
    Task<decimal> CalculateNPFEmployeeAsync(decimal basicSalary); // 3%
    Task<decimal> CalculateNPFEmployerAsync(decimal basicSalary); // 6%
    Task<decimal> CalculateNSFEmployeeAsync(decimal basicSalary); // 2.5%
    Task<decimal> CalculateNSFEmployerAsync(decimal basicSalary); // 2.5%
    Task<decimal> CalculatePRGFAsync(decimal grossSalary, int yearsOfService);
    Task<decimal> CalculateCSGAsync(decimal grossSalary); // 3%
    Task<decimal> CalculatePAYEAsync(decimal grossSalary, decimal totalDeductions);

    // Earnings Calculations
    Task<decimal> CalculateOvertimePayAsync(Guid employeeId, int month, int year);
    Task<decimal> Calculate13thMonthBonusAsync(Guid employeeId, int year);
    Task<decimal> CalculateGratuityAsync(Guid employeeId, DateTime resignationDate);
    Task<decimal> CalculateLeaveEncashmentAsync(Guid employeeId);

    // Reports
    Task<PayrollSummaryDto> GetPayrollSummaryAsync(Guid payrollCycleId);
    Task<byte[]> GenerateBankTransferFileAsync(Guid payrollCycleId);
}
```

**ISalaryComponentService.cs:**
```csharp
public interface ISalaryComponentService
{
    Task<Guid> CreateComponentAsync(CreateSalaryComponentDto dto, string createdBy);
    Task<List<SalaryComponentDto>> GetEmployeeComponentsAsync(Guid employeeId, bool activeOnly = true);
    Task UpdateComponentAsync(Guid id, UpdateSalaryComponentDto dto, string updatedBy);
    Task DeleteComponentAsync(Guid id);
    Task<decimal> GetTotalAllowancesAsync(Guid employeeId, int month, int year);
    Task<decimal> GetTotalDeductionsAsync(Guid employeeId, int month, int year);
}
```

### Step 4: CRITICAL - PayrollService Implementation

This is the HEART of the payroll system. The service must implement:

**Mauritius 2025 Tax Brackets (PAYE):**
```csharp
private decimal CalculatePAYE(decimal annualIncome, decimal annualDeductions)
{
    decimal taxableIncome = annualIncome - annualDeductions;

    // 2025 Mauritius Tax Rates
    decimal tax = 0;

    if (taxableIncome <= 390000) // MUR 390,000 tax-free threshold
        return 0;

    if (taxableIncome > 390000 && taxableIncome <= 550000)
        tax = (taxableIncome - 390000) * 0.10m; // 10%
    else if (taxableIncome > 550000 && taxableIncome <= 650000)
        tax = 16000 + ((taxableIncome - 550000) * 0.12m); // 12%
    else if (taxableIncome > 650000)
        tax = 28000 + ((taxableIncome - 650000) * 0.15m); // 15%

    return tax / 12; // Monthly PAYE
}
```

**PRGF Calculation Formula:**
```csharp
private decimal CalculatePRGF(decimal monthlyGross, int years)
{
    // PRGF for employees hired after 2020
    // Employer contribution based on years of service
    decimal rate = years switch
    {
        <= 5 => 0.043m,  // 4.3%
        <= 10 => 0.050m, // 5.0%
        _ => 0.068m      // 6.8%
    };

    return monthlyGross * rate;
}
```

**Overtime Integration with Attendance:**
```csharp
public async Task<decimal> CalculateOvertimePayAsync(Guid employeeId, int month, int year)
{
    // 1. Get employee salary
    var employee = await _context.Employees.FindAsync(employeeId);
    decimal hourlyRate = employee.BasicSalary / 173.33m; // Monthly hours

    // 2. Get overtime hours from ATTENDANCE MODULE
    var startDate = new DateTime(year, month, 1);
    var endDate = startDate.AddMonths(1).AddDays(-1);

    var attendances = await _context.Attendances
        .Where(a => a.EmployeeId == employeeId)
        .Where(a => a.Date >= startDate && a.Date <= endDate)
        .ToListAsync();

    decimal totalOvertimePay = 0;

    foreach (var attendance in attendances)
    {
        if (attendance.OvertimeHours > 0 && attendance.OvertimeRate.HasValue)
        {
            // OvertimeRate comes from SECTOR RULES (1.5x, 2x, 3x)
            totalOvertimePay += attendance.OvertimeHours * hourlyRate * attendance.OvertimeRate.Value;
        }
    }

    return totalOvertimePay;
}
```

### Step 5: Controllers

Create:
- **PayrollController.cs** - 11 endpoints
- **SalaryComponentsController.cs** - 5 endpoints

### Step 6: Database Configuration

Update `TenantDbContext.cs`:
```csharp
public DbSet<PayrollCycle> PayrollCycles { get; set; }
public DbSet<Payslip> Payslips { get; set; }
public DbSet<SalaryComponent> SalaryComponents { get; set; }

// Configure entities in OnModelCreating
modelBuilder.Entity<PayrollCycle>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => new { e.Month, e.Year }).IsUnique();

    entity.Property(e => e.TotalGrossSalary).HasColumnType("decimal(18,2)");
    entity.Property(e => e.TotalDeductions).HasColumnType("decimal(18,2)");
    // ... all decimal properties
});

modelBuilder.Entity<Payslip>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => new { e.EmployeeId, e.Month, e.Year });

    entity.HasOne(e => e.PayrollCycle)
          .WithMany(p => p.Payslips)
          .HasForeignKey(e => e.PayrollCycleId);

    entity.HasOne(e => e.Employee)
          .WithMany()
          .HasForeignKey(e => e.EmployeeId);
});
```

### Step 7: Register Services in Program.cs

```csharp
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<ISalaryComponentService, SalaryComponentService>();
```

### Step 8: Create Migration

```bash
cd src/HRMS.Infrastructure
dotnet ef migrations add AddPayrollManagement --context TenantDbContext --output-dir Data/Migrations/Tenant
```

## 🔥 CRITICAL INTEGRATION POINTS

### 1. Attendance → Overtime Pay
```csharp
// AttendanceService already calculates OvertimeHours and OvertimeRate
// PayrollService reads this data to calculate overtime pay
var overtimePay = await _attendanceService.CalculateOvertimePayAsync(employeeId, month, year);
```

### 2. Industry Sector → Minimum Wage Validation
```csharp
// Validate employee salary meets sector minimum wage
var tenant = await _masterContext.Tenants.FindAsync(tenantId);
var sectorRule = await _masterContext.SectorComplianceRules
    .Where(r => r.SectorId == tenant.SectorId)
    .Where(r => r.RuleCategory == "MINIMUM_WAGE")
    .FirstOrDefaultAsync();

if (employee.BasicSalary < minimumWage)
    throw new ValidationException("Salary below sector minimum wage");
```

### 3. Leave → Encashment
```csharp
// Calculate leave encashment on resignation
var unusedAnnualLeave = employee.AnnualLeaveBalance;
var dailyRate = employee.BasicSalary / 26; // Working days per month
var encashment = unusedAnnualLeave * dailyRate;
```

## 📊 EXAMPLE PAYSLIP CALCULATION

**Employee:** John Doe
**Month:** November 2025
**Basic Salary:** MUR 30,000

**Earnings:**
- Basic Salary: MUR 30,000.00
- Housing Allowance: MUR 5,000.00
- Transport Allowance: MUR 2,000.00
- Overtime Pay (10h × 1.5x rate): MUR 2,596.15
- **Gross Salary: MUR 39,596.15**

**Statutory Deductions:**
- NPF Employee (3%): MUR 900.00
- NSF Employee (2.5%): MUR 750.00
- CSG (3%): MUR 1,187.88
- PAYE (progressive): MUR 825.00
- **Total Statutory: MUR 3,662.88**

**Other Deductions:**
- Loan Repayment: MUR 2,000.00
- **Total Deductions: MUR 5,662.88**

**Net Salary: MUR 33,933.27**

**Employer Contributions (recorded but not deducted):**
- NPF Employer (6%): MUR 1,800.00
- NSF Employer (2.5%): MUR 750.00
- PRGF (4.3%): MUR 1,702.63
- **Total Employer Cost: MUR 4,252.63**

**Total Cost to Company: MUR 43,848.78**

## 🚀 NEXT STEPS

1. Implement all entity classes (3 files)
2. Create all DTOs (12 files)
3. Implement PayrollService with ALL statutory calculations (1,500+ lines)
4. Implement SalaryComponentService (200+ lines)
5. Create both controllers (600+ lines)
6. Update DbContext configurations
7. Create migration
8. Build and test

## ⚠️ IMPORTANT NOTES

- All monetary values use `decimal(18,2)` precision
- Mauritius tax laws are based on 2025 regulations
- PRGF applies to employees hired after January 2020
- Overtime rates come from Industry Sector compliance rules
- Payroll cannot be processed if attendance is incomplete
- All calculations must be auditable and transparent

This module is THE MOST COMPLEX but also THE MOST CRITICAL for HRMS success!
