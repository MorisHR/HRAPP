using Microsoft.EntityFrameworkCore;
using HRMS.Core.Entities.Tenant;

namespace HRMS.Infrastructure.Data;

/// <summary>
/// Tenant Database Context - handles tenant-specific entities
/// Schema: Dynamic (tenant_{id}) - isolated per tenant
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly string _tenantSchema;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, string tenantSchema)
        : base(options)
    {
        _tenantSchema = tenantSchema;
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<EmergencyContact> EmergencyContacts { get; set; }

    // Leave Management
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeaveBalance> LeaveBalances { get; set; }
    public DbSet<LeaveApplication> LeaveApplications { get; set; }
    public DbSet<LeaveApproval> LeaveApprovals { get; set; }
    public DbSet<PublicHoliday> PublicHolidays { get; set; }
    public DbSet<LeaveEncashment> LeaveEncashments { get; set; }

    // Industry Sector Configuration (tenant-specific)
    public DbSet<TenantSectorConfiguration> TenantSectorConfigurations { get; set; }
    public DbSet<TenantCustomComplianceRule> TenantCustomComplianceRules { get; set; }

    // Attendance Management
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<AttendanceMachine> AttendanceMachines { get; set; }
    public DbSet<AttendanceCorrection> AttendanceCorrections { get; set; }

    // Payroll Management
    public DbSet<PayrollCycle> PayrollCycles { get; set; }
    public DbSet<Payslip> Payslips { get; set; }
    public DbSet<SalaryComponent> SalaryComponents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set the schema dynamically based on tenant
        modelBuilder.HasDefaultSchema(_tenantSchema);

        // Configure Employee entity
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeCode).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.NationalIdCard);
            entity.HasIndex(e => e.PassportNumber);

            // Basic information
            entity.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);

            // Nationality & Type
            entity.Property(e => e.Nationality).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CountryOfOrigin).HasMaxLength(100);

            // Identification
            entity.Property(e => e.NationalIdCard).HasMaxLength(50);
            entity.Property(e => e.PassportNumber).HasMaxLength(50);

            // Visa/Work Permit
            entity.Property(e => e.VisaNumber).HasMaxLength(100);
            entity.Property(e => e.WorkPermitNumber).HasMaxLength(100);

            // Tax & Statutory
            entity.Property(e => e.TaxIdNumber).HasMaxLength(50);
            entity.Property(e => e.NPFNumber).HasMaxLength(50);
            entity.Property(e => e.NSFNumber).HasMaxLength(50);
            entity.Property(e => e.PRGFNumber).HasMaxLength(50);

            // Employment
            entity.Property(e => e.JobTitle).IsRequired().HasMaxLength(200);

            // Salary & Bank
            entity.Property(e => e.BasicSalary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SalaryCurrency).HasMaxLength(10);
            entity.Property(e => e.BankName).HasMaxLength(200);
            entity.Property(e => e.BankAccountNumber).HasMaxLength(100);
            entity.Property(e => e.BankBranch).HasMaxLength(200);
            entity.Property(e => e.BankSwiftCode).HasMaxLength(50);

            // Leave balances
            entity.Property(e => e.AnnualLeaveBalance).HasColumnType("decimal(10,2)");
            entity.Property(e => e.SickLeaveBalance).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CasualLeaveBalance).HasColumnType("decimal(10,2)");

            // Offboarding
            entity.Property(e => e.OffboardingReason).HasMaxLength(500);
            entity.Property(e => e.OffboardingNotes).HasMaxLength(2000);

            // Relationships
            entity.HasOne(e => e.Department)
                  .WithMany(d => d.Employees)
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Manager self-referencing relationship
            entity.HasOne(e => e.Manager)
                  .WithMany()
                  .HasForeignKey(e => e.ManagerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Ignore calculated properties
            entity.Ignore(e => e.FullName);
            entity.Ignore(e => e.YearsOfService);
            entity.Ignore(e => e.Age);
            entity.Ignore(e => e.IsExpatriate);
            entity.Ignore(e => e.PassportExpiryStatus);
            entity.Ignore(e => e.VisaExpiryStatus);
            entity.Ignore(e => e.HasExpiredDocuments);
            entity.Ignore(e => e.HasDocumentsExpiringSoon);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Department entity
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.Code).IsUnique();
            entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Code).IsRequired().HasMaxLength(50);

            // Self-referencing relationship for hierarchy
            entity.HasOne(d => d.ParentDepartment)
                  .WithMany(d => d.SubDepartments)
                  .HasForeignKey(d => d.ParentDepartmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Department Head relationship
            entity.HasOne(d => d.DepartmentHead)
                  .WithMany()
                  .HasForeignKey(d => d.DepartmentHeadId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(d => !d.IsDeleted);
        });

        // Configure EmergencyContact entity
        modelBuilder.Entity<EmergencyContact>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ContactName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AlternatePhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Relationship).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ContactType).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Country).HasMaxLength(100);

            // Relationship with Employee
            entity.HasOne(e => e.Employee)
                  .WithMany(emp => emp.EmergencyContacts)
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure LeaveType entity
        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TypeCode);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DefaultEntitlement).HasColumnType("decimal(10,2)");

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure LeaveBalance entity
        modelBuilder.Entity<LeaveBalance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId, e.Year }).IsUnique();

            entity.Property(e => e.TotalEntitlement).HasColumnType("decimal(10,2)");
            entity.Property(e => e.UsedDays).HasColumnType("decimal(10,2)");
            entity.Property(e => e.PendingDays).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CarriedForward).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Accrued).HasColumnType("decimal(10,2)");

            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LeaveType)
                  .WithMany(lt => lt.LeaveBalances)
                  .HasForeignKey(e => e.LeaveTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Ignore(e => e.AvailableDays);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure LeaveApplication entity
        modelBuilder.Entity<LeaveApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ApplicationNumber).IsUnique();
            entity.HasIndex(e => new { e.EmployeeId, e.StartDate, e.EndDate });

            entity.Property(e => e.ApplicationNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalDays).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);

            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LeaveType)
                  .WithMany(lt => lt.LeaveApplications)
                  .HasForeignKey(e => e.LeaveTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Approver)
                  .WithMany()
                  .HasForeignKey(e => e.ApprovedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Rejector)
                  .WithMany()
                  .HasForeignKey(e => e.RejectedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure LeaveApproval entity
        modelBuilder.Entity<LeaveApproval>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.LeaveApplicationId, e.ApprovalLevel });

            entity.Property(e => e.ApproverRole).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.LeaveApplication)
                  .WithMany(la => la.Approvals)
                  .HasForeignKey(e => e.LeaveApplicationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Approver)
                  .WithMany()
                  .HasForeignKey(e => e.ApproverId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure PublicHoliday entity
        modelBuilder.Entity<PublicHoliday>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Date, e.Year });
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure LeaveEncashment entity
        modelBuilder.Entity<LeaveEncashment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);

            entity.Property(e => e.UnusedAnnualLeaveDays).HasColumnType("decimal(10,2)");
            entity.Property(e => e.UnusedSickLeaveDays).HasColumnType("decimal(10,2)");
            entity.Property(e => e.TotalEncashableDays).HasColumnType("decimal(10,2)");
            entity.Property(e => e.DailySalary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalEncashmentAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure TenantSectorConfiguration entity
        modelBuilder.Entity<TenantSectorConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SectorId);

            entity.Property(e => e.SectorName).HasMaxLength(300);
            entity.Property(e => e.SectorCode).HasMaxLength(100);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure TenantCustomComplianceRule entity
        modelBuilder.Entity<TenantCustomComplianceRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SectorComplianceRuleId);
            entity.HasIndex(e => e.RuleCategory);

            entity.Property(e => e.CustomRuleConfig).HasColumnType("jsonb");
            entity.Property(e => e.RuleCategory).HasMaxLength(50);
            entity.Property(e => e.RuleName).HasMaxLength(200);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Attendance entity
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.Date }).IsUnique();
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.WorkingHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.OvertimeHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.OvertimeRate).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Remarks).HasMaxLength(500);

            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure AttendanceMachine entity
        modelBuilder.Entity<AttendanceMachine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IpAddress);
            entity.HasIndex(e => e.ZKTecoDeviceId);
            entity.HasIndex(e => e.SerialNumber);

            entity.Property(e => e.MachineName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.MachineId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.ZKTecoDeviceId).HasMaxLength(50);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.Model).HasMaxLength(100);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure AttendanceCorrection entity
        modelBuilder.Entity<AttendanceCorrection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AttendanceId, e.Status });
            entity.HasIndex(e => e.RequestedBy);

            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);

            entity.HasOne(e => e.Attendance)
                  .WithMany()
                  .HasForeignKey(e => e.AttendanceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure PayrollCycle entity
        modelBuilder.Entity<PayrollCycle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Month, e.Year }).IsUnique();
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.TotalGrossSalary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalDeductions).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalNetSalary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalNPFEmployee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalNPFEmployer).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalNSFEmployee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalNSFEmployer).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCSGEmployee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCSGEmployer).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPRGF).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalTrainingLevy).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPAYE).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalOvertimePay).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(2000);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Payslip entity
        modelBuilder.Entity<Payslip>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.Month, e.Year });
            entity.HasIndex(e => e.PayrollCycleId);
            entity.HasIndex(e => e.PayslipNumber).IsUnique();
            entity.HasIndex(e => e.PaymentStatus);

            entity.Property(e => e.PayslipNumber).IsRequired().HasMaxLength(100);

            // Earnings
            entity.Property(e => e.BasicSalary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.HousingAllowance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TransportAllowance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MealAllowance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MobileAllowance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OtherAllowances).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OvertimeHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.OvertimePay).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ThirteenthMonthBonus).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LeaveEncashment).HasColumnType("decimal(18,2)");
            entity.Property(e => e.GratuityPayment).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Commission).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalGrossSalary).HasColumnType("decimal(18,2)");

            // Attendance
            entity.Property(e => e.PaidLeaveDays).HasColumnType("decimal(10,2)");
            entity.Property(e => e.UnpaidLeaveDays).HasColumnType("decimal(10,2)");
            entity.Property(e => e.LeaveDeductions).HasColumnType("decimal(18,2)");

            // Deductions
            entity.Property(e => e.NPF_Employee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NSF_Employee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CSG_Employee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PAYE_Tax).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NPF_Employer).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NSF_Employer).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CSG_Employer).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PRGF_Contribution).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TrainingLevy).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LoanDeduction).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AdvanceDeduction).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MedicalInsurance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OtherDeductions).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalDeductions).HasColumnType("decimal(18,2)");

            // Net
            entity.Property(e => e.NetSalary).HasColumnType("decimal(18,2)");

            // Payment
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentReference).HasMaxLength(200);
            entity.Property(e => e.BankAccountNumber).HasMaxLength(100);
            entity.Property(e => e.Remarks).HasMaxLength(1000);

            // Relationships
            entity.HasOne(e => e.PayrollCycle)
                  .WithMany(p => p.Payslips)
                  .HasForeignKey(e => e.PayrollCycleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure SalaryComponent entity
        modelBuilder.Entity<SalaryComponent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.ComponentType);
            entity.HasIndex(e => new { e.IsActive, e.EffectiveFrom });

            entity.Property(e => e.ComponentName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CalculationMethod).HasMaxLength(50);
            entity.Property(e => e.PercentageBase).HasMaxLength(50);

            // Relationship
            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }
}
