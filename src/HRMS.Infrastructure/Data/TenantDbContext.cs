using Microsoft.EntityFrameworkCore;
using HRMS.Core.Entities.Tenant;
using HRMS.Application.Interfaces;
using HRMS.Infrastructure.Persistence.ValueConverters;

namespace HRMS.Infrastructure.Data;

/// <summary>
/// Tenant Database Context - handles tenant-specific entities
/// Schema: Dynamic (tenant_{id}) - isolated per tenant
///
/// SECURITY: Column-level encryption enabled for sensitive PII data
/// - Encrypted fields use AES-256-GCM via IEncryptionService
/// - Encryption is transparent (automatic encrypt on save, decrypt on load)
/// - Supports key rotation and graceful migration
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly string _tenantSchema;
    private readonly IEncryptionService? _encryptionService;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, string tenantSchema)
        : base(options)
    {
        _tenantSchema = tenantSchema;
    }

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        string tenantSchema,
        IEncryptionService? encryptionService)
        : base(options)
    {
        _tenantSchema = tenantSchema;
        _encryptionService = encryptionService;
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<EmployeeDraft> EmployeeDrafts { get; set; }
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

    // Timesheet Management
    public DbSet<Timesheet> Timesheets { get; set; }
    public DbSet<TimesheetEntry> TimesheetEntries { get; set; }
    public DbSet<TimesheetAdjustment> TimesheetAdjustments { get; set; }
    public DbSet<TimesheetComment> TimesheetComments { get; set; }

    // Multi-Device Biometric Attendance System
    public DbSet<Location> Locations { get; set; }
    public DbSet<EmployeeDeviceAccess> EmployeeDeviceAccesses { get; set; }
    public DbSet<DeviceSyncLog> DeviceSyncLogs { get; set; }
    public DbSet<AttendanceAnomaly> AttendanceAnomalies { get; set; }

    // Device API Key Authentication (Fortune 500-grade security)
    public DbSet<DeviceApiKey> DeviceApiKeys { get; set; }

    // Biometric Attendance Capture (Fortune 500-grade)
    public DbSet<BiometricPunchRecord> BiometricPunchRecords { get; set; }

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

            // ==============================================
            // COLUMN-LEVEL ENCRYPTION (P0-CRITICAL SECURITY)
            // ==============================================
            // Apply AES-256-GCM encryption to sensitive PII fields
            // Encrypted columns will store Base64-encoded ciphertext
            // IMPORTANT: Encrypted fields cannot be efficiently indexed or searched
            // Use hashed columns for searchable encrypted data
            if (_encryptionService != null)
            {
                // Bank details (highly sensitive)
                entity.Property(e => e.BankAccountNumber)
                    .HasConversion(new EncryptedStringConverter(_encryptionService))
                    .HasMaxLength(500); // Increased for encrypted data

                entity.Property(e => e.BankName)
                    .HasConversion(new EncryptedStringConverter(_encryptionService))
                    .HasMaxLength(500);

                // Salary (financial PII)
                entity.Property(e => e.BasicSalary)
                    .HasConversion(new EncryptedDecimalConverter(_encryptionService))
                    .HasColumnType("text"); // Store encrypted decimal as text

                // Government IDs (identity theft risk)
                entity.Property(e => e.TaxIdNumber)
                    .HasConversion(new EncryptedStringConverter(_encryptionService))
                    .HasMaxLength(500);

                entity.Property(e => e.PassportNumber)
                    .HasConversion(new EncryptedStringConverter(_encryptionService))
                    .HasMaxLength(500);

                entity.Property(e => e.NationalIdCard)
                    .HasConversion(new EncryptedStringConverter(_encryptionService))
                    .HasMaxLength(500);

                // Address (physical security risk)
                entity.Property(e => e.AddressLine1)
                    .HasConversion(new EncryptedStringConverter(_encryptionService))
                    .HasMaxLength(1000);

                entity.Property(e => e.AddressLine2)
                    .HasConversion(new EncryptedStringConverter(_encryptionService))
                    .HasMaxLength(1000);
            }

            // Add CHECK constraints for data validation
            entity.HasCheckConstraint(
                "chk_Employees_PasswordHash_Length",
                "\"PasswordHash\" IS NULL OR LENGTH(\"PasswordHash\") >= 32");

            entity.HasCheckConstraint(
                "chk_Employees_BasicSalary_NonNegative",
                "\"BasicSalary\" >= 0");

            entity.HasCheckConstraint(
                "chk_Employees_AnnualLeaveBalance_NonNegative",
                "\"AnnualLeaveBalance\" >= 0");

            entity.HasCheckConstraint(
                "chk_Employees_SickLeaveBalance_NonNegative",
                "\"SickLeaveBalance\" >= 0");

            entity.HasCheckConstraint(
                "chk_Employees_CasualLeaveBalance_NonNegative",
                "\"CasualLeaveBalance\" >= 0");

            // Address (Mauritius Compliant)
            entity.Property(e => e.AddressLine1).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AddressLine2).HasMaxLength(500);
            entity.Property(e => e.Village).HasMaxLength(100);
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.Country).IsRequired().HasMaxLength(100);

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

            // Primary location relationship (multi-device biometric system)
            entity.HasOne(e => e.PrimaryLocation)
                  .WithMany(l => l.Employees)
                  .HasForeignKey(e => e.PrimaryLocationId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Biometric enrollment fields
            entity.Property(e => e.BiometricEnrollmentId).HasMaxLength(100);

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

            // ==============================================
            // COLUMN-LEVEL ENCRYPTION (Emergency Contact PII)
            // ==============================================
            if (_encryptionService != null)
            {
                // Contact phone numbers (sensitive PII)
                entity.Property(e => e.PhoneNumber)
                    .HasConversion(new EncryptedStringConverter(_encryptionService))
                    .HasMaxLength(500);

                entity.Property(e => e.AlternatePhoneNumber)
                    .HasConversion(new EncryptedStringConverter(_encryptionService))
                    .HasMaxLength(500);

                // Contact address (physical security)
                entity.Property(e => e.Address)
                    .HasConversion(new EncryptedStringConverter(_encryptionService))
                    .HasMaxLength(1000);
            }

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

            // Add composite index for leave balance queries
            entity.HasIndex(e => new { e.EmployeeId, e.Year, e.LeaveTypeId })
                  .HasFilter("\"IsDeleted\" = false");

            entity.Property(e => e.TotalEntitlement).HasColumnType("decimal(10,2)");
            entity.Property(e => e.UsedDays).HasColumnType("decimal(10,2)");
            entity.Property(e => e.PendingDays).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CarriedForward).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Accrued).HasColumnType("decimal(10,2)");

            // Add CHECK constraints
            entity.HasCheckConstraint(
                "chk_LeaveBalances_Days_NonNegative",
                "\"TotalEntitlement\" >= 0 AND \"UsedDays\" >= 0 AND \"PendingDays\" >= 0");

            entity.HasCheckConstraint(
                "chk_LeaveBalances_Accrual_NonNegative",
                "\"CarriedForward\" >= 0 AND \"Accrued\" >= 0");

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

            // Add CHECK constraints
            entity.HasCheckConstraint(
                "chk_LeaveEncashments_Days_NonNegative",
                "\"UnusedAnnualLeaveDays\" >= 0 AND \"UnusedSickLeaveDays\" >= 0 AND \"TotalEncashableDays\" >= 0");

            entity.HasCheckConstraint(
                "chk_LeaveEncashments_Amount_NonNegative",
                "\"DailySalary\" >= 0 AND \"TotalEncashmentAmount\" >= 0");

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
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.LocationId);
            entity.HasIndex(e => new { e.EmployeeId, e.DeviceId, e.Date });

            // Add composite indexes for performance
            entity.HasIndex(e => new { e.EmployeeId, e.Date, e.Status })
                  .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => new { e.DeviceId, e.Date })
                  .HasFilter("\"IsDeleted\" = false");

            entity.Property(e => e.WorkingHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.OvertimeHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.OvertimeRate).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Remarks).HasMaxLength(500);

            // Add CHECK constraints
            entity.HasCheckConstraint(
                "chk_Attendances_WorkingHours_NonNegative",
                "\"WorkingHours\" >= 0");

            entity.HasCheckConstraint(
                "chk_Attendances_OvertimeHours_NonNegative",
                "\"OvertimeHours\" >= 0");

            entity.HasCheckConstraint(
                "chk_Attendances_OvertimeRate_NonNegative",
                "\"OvertimeRate\" >= 0");

            // Multi-device tracking fields
            entity.Property(e => e.PunchSource).HasMaxLength(50);
            entity.Property(e => e.VerificationMethod).HasMaxLength(50);
            entity.Property(e => e.DeviceUserId).HasMaxLength(100);
            entity.Property(e => e.AuthorizationNote).HasMaxLength(1000);

            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Device relationship (multi-device biometric system)
            entity.HasOne(e => e.Device)
                  .WithMany(d => d.Attendances)
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Location relationship (multi-device biometric system)
            entity.HasOne(e => e.Location)
                  .WithMany(l => l.Attendances)
                  .HasForeignKey(e => e.LocationId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure AttendanceMachine entity
        modelBuilder.Entity<AttendanceMachine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IpAddress);
            entity.HasIndex(e => e.ZKTecoDeviceId);
            entity.HasIndex(e => e.SerialNumber);
            entity.HasIndex(e => e.DeviceCode).IsUnique();
            entity.HasIndex(e => e.LocationId);
            entity.HasIndex(e => new { e.LocationId, e.DeviceStatus });

            entity.Property(e => e.MachineName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.MachineId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.LegacyLocation).HasMaxLength(200); // Legacy field - kept for backward compatibility
            entity.Property(e => e.ZKTecoDeviceId).HasMaxLength(50);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.Model).HasMaxLength(100);

            // Multi-device system fields
            entity.Property(e => e.DeviceCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DeviceType).HasMaxLength(100);
            entity.Property(e => e.MacAddress).HasMaxLength(50);
            entity.Property(e => e.FirmwareVersion).HasMaxLength(50);
            entity.Property(e => e.ConnectionMethod).HasMaxLength(50);
            entity.Property(e => e.DeviceConfigJson).HasColumnType("jsonb");
            entity.Property(e => e.DeviceStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastSyncStatus).HasMaxLength(50);

            // Location relationship (multi-device biometric system)
            entity.HasOne(e => e.Location)
                  .WithMany(l => l.BiometricDevices)
                  .HasForeignKey(e => e.LocationId)
                  .OnDelete(DeleteBehavior.SetNull);

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

            // Add composite indexes for payroll reports
            entity.HasIndex(e => new { e.Year, e.Month })
                  .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => new { e.Status, e.PaymentDate })
                  .HasFilter("\"IsDeleted\" = false");

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

            // Add CHECK constraints
            entity.HasCheckConstraint(
                "chk_PayrollCycles_Salary_NonNegative",
                "\"TotalGrossSalary\" >= 0 AND \"TotalDeductions\" >= 0 AND \"TotalNetSalary\" >= 0");

            entity.HasCheckConstraint(
                "chk_PayrollCycles_Month_Valid",
                "\"Month\" >= 1 AND \"Month\" <= 12");

            entity.HasCheckConstraint(
                "chk_PayrollCycles_Year_Valid",
                "\"Year\" > 1900");

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

            // ==============================================
            // COLUMN-LEVEL ENCRYPTION (Payslip Financial Data)
            // ==============================================
            // Encrypt salary components to protect employee financial data
            if (_encryptionService != null)
            {
                // Core earnings (highly sensitive)
                entity.Property(e => e.BasicSalary)
                    .HasConversion(new EncryptedDecimalConverter(_encryptionService))
                    .HasColumnType("text");

                entity.Property(e => e.TotalGrossSalary)
                    .HasConversion(new EncryptedDecimalConverter(_encryptionService))
                    .HasColumnType("text");

                entity.Property(e => e.TotalDeductions)
                    .HasConversion(new EncryptedDecimalConverter(_encryptionService))
                    .HasColumnType("text");

                entity.Property(e => e.NetSalary)
                    .HasConversion(new EncryptedDecimalConverter(_encryptionService))
                    .HasColumnType("text");
            }
            else
            {
                // Fallback to standard decimal columns if encryption not enabled
                entity.Property(e => e.BasicSalary).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalGrossSalary).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalDeductions).HasColumnType("decimal(18,2)");
                entity.Property(e => e.NetSalary).HasColumnType("decimal(18,2)");
            }

            // Allowances (standard decimal - not encrypted for reporting performance)
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

            // Add CHECK constraints
            entity.HasCheckConstraint(
                "chk_Payslips_Salary_NonNegative",
                "\"BasicSalary\" >= 0 AND \"TotalGrossSalary\" >= 0 AND \"TotalDeductions\" >= 0");

            entity.HasCheckConstraint(
                "chk_Payslips_Overtime_NonNegative",
                "\"OvertimeHours\" >= 0 AND \"OvertimePay\" >= 0");

            entity.HasCheckConstraint(
                "chk_Payslips_LeaveDays_NonNegative",
                "\"PaidLeaveDays\" >= 0 AND \"UnpaidLeaveDays\" >= 0");

            entity.HasCheckConstraint(
                "chk_Payslips_Allowances_NonNegative",
                "\"HousingAllowance\" >= 0 AND \"TransportAllowance\" >= 0 AND \"MealAllowance\" >= 0 AND \"MobileAllowance\" >= 0 AND \"OtherAllowances\" >= 0");

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

            // Add CHECK constraint
            entity.HasCheckConstraint(
                "chk_SalaryComponents_Amount_NonNegative",
                "\"Amount\" >= 0");

            // Relationship
            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Timesheet entity
        modelBuilder.Entity<Timesheet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.PeriodStart, e.PeriodEnd });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.PeriodStart, e.PeriodEnd });

            // Add composite indexes for approval workflow
            entity.HasIndex(e => new { e.Status, e.PeriodStart })
                  .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => new { e.EmployeeId, e.Status, e.PeriodStart })
                  .HasFilter("\"IsDeleted\" = false");

            entity.Property(e => e.TotalRegularHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.TotalOvertimeHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.TotalHolidayHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.TotalSickLeaveHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.TotalAnnualLeaveHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.TotalAbsentHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.ApprovedByName).HasMaxLength(200);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.Property(e => e.Notes).HasMaxLength(2000);

            // Add CHECK constraints
            entity.HasCheckConstraint(
                "chk_Timesheets_Hours_NonNegative",
                "\"TotalRegularHours\" >= 0 AND \"TotalOvertimeHours\" >= 0 AND \"TotalHolidayHours\" >= 0 AND \"TotalSickLeaveHours\" >= 0 AND \"TotalAnnualLeaveHours\" >= 0 AND \"TotalAbsentHours\" >= 0");

            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.TotalPayableHours);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure TimesheetEntry entity
        modelBuilder.Entity<TimesheetEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TimesheetId, e.Date });
            entity.HasIndex(e => e.AttendanceId);
            entity.HasIndex(e => e.Date);

            // Add composite index for filtering entries
            entity.HasIndex(e => new { e.TimesheetId, e.Date })
                  .HasFilter("\"IsDeleted\" = false");

            entity.Property(e => e.ActualHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.RegularHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.OvertimeHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.HolidayHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.SickLeaveHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.AnnualLeaveHours).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Notes).HasMaxLength(1000);

            // Add CHECK constraints
            entity.HasCheckConstraint(
                "chk_TimesheetEntries_Hours_NonNegative",
                "\"ActualHours\" >= 0 AND \"RegularHours\" >= 0 AND \"OvertimeHours\" >= 0 AND \"HolidayHours\" >= 0 AND \"SickLeaveHours\" >= 0 AND \"AnnualLeaveHours\" >= 0");

            entity.HasOne(e => e.Timesheet)
                  .WithMany(t => t.Entries)
                  .HasForeignKey(e => e.TimesheetId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Attendance)
                  .WithMany()
                  .HasForeignKey(e => e.AttendanceId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure TimesheetAdjustment entity
        modelBuilder.Entity<TimesheetAdjustment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TimesheetEntryId);
            entity.HasIndex(e => e.AdjustedBy);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OldValue).HasMaxLength(500);
            entity.Property(e => e.NewValue).HasMaxLength(500);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.AdjustedByName).HasMaxLength(200);
            entity.Property(e => e.ApprovedByName).HasMaxLength(200);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);

            entity.HasOne(e => e.TimesheetEntry)
                  .WithMany(te => te.Adjustments)
                  .HasForeignKey(e => e.TimesheetEntryId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure TimesheetComment entity
        modelBuilder.Entity<TimesheetComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TimesheetId);
            entity.HasIndex(e => e.CommentedAt);

            entity.Property(e => e.UserName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Comment).IsRequired().HasMaxLength(2000);

            entity.HasOne(e => e.Timesheet)
                  .WithMany(t => t.Comments)
                  .HasForeignKey(e => e.TimesheetId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ==============================================
        // MULTI-DEVICE BIOMETRIC ATTENDANCE SYSTEM
        // ==============================================

        // Configure Location entity
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LocationCode).IsUnique();
            entity.HasIndex(e => e.LocationName);
            entity.HasIndex(e => e.LocationType);
            entity.HasIndex(e => new { e.Latitude, e.Longitude });

            entity.Property(e => e.LocationCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LocationName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LocationType).HasMaxLength(100);
            entity.Property(e => e.AddressLine1).HasMaxLength(500);
            entity.Property(e => e.AddressLine2).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.Country).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.WorkingHoursJson).HasColumnType("jsonb");
            entity.Property(e => e.Timezone).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Latitude).HasColumnType("decimal(10,8)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(11,8)");

            // Self-referencing relationship for location manager
            entity.HasOne(e => e.LocationManager)
                  .WithMany()
                  .HasForeignKey(e => e.LocationManagerId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure EmployeeDeviceAccess entity
        modelBuilder.Entity<EmployeeDeviceAccess>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EmployeeId, e.DeviceId, e.IsActive });
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.AccessType);
            entity.HasIndex(e => new { e.ValidFrom, e.ValidUntil });

            entity.Property(e => e.AccessType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AccessReason).HasMaxLength(500);
            entity.Property(e => e.AllowedDaysJson).HasColumnType("jsonb");

            // Relationships
            entity.HasOne(e => e.Employee)
                  .WithMany(emp => emp.DeviceAccesses)
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Device)
                  .WithMany(d => d.EmployeeDeviceAccesses)
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure DeviceSyncLog entity
        modelBuilder.Entity<DeviceSyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.SyncStartTime);
            entity.HasIndex(e => e.SyncStatus);
            entity.HasIndex(e => new { e.DeviceId, e.SyncStartTime });

            entity.Property(e => e.SyncStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SyncMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.ErrorDetailsJson).HasColumnType("jsonb");

            // Relationship
            entity.HasOne(e => e.Device)
                  .WithMany(d => d.SyncLogs)
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure AttendanceAnomaly entity
        modelBuilder.Entity<AttendanceAnomaly>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.AttendanceId);
            entity.HasIndex(e => new { e.AnomalyType, e.AnomalySeverity });
            entity.HasIndex(e => e.AnomalyDate);
            entity.HasIndex(e => e.ResolutionStatus);
            entity.HasIndex(e => new { e.EmployeeId, e.AnomalyDate });

            entity.Property(e => e.AnomalyType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AnomalySeverity).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AnomalyDescription).HasMaxLength(1000);
            entity.Property(e => e.AnomalyDetailsJson).HasColumnType("jsonb");
            entity.Property(e => e.ResolutionStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ResolutionNote).HasMaxLength(1000);
            entity.Property(e => e.NotificationRecipientsJson).HasColumnType("jsonb");
            entity.Property(e => e.AutoResolutionRule).HasMaxLength(200);

            // Add CHECK constraint
            entity.HasCheckConstraint(
                "chk_AttendanceAnomalies_Description_NotEmpty",
                "\"AnomalyDescription\" IS NULL OR LENGTH(TRIM(\"AnomalyDescription\")) > 0");

            // Relationships
            entity.HasOne(e => e.Employee)
                  .WithMany(emp => emp.AttendanceAnomalies)
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Attendance)
                  .WithMany(a => a.Anomalies)
                  .HasForeignKey(e => e.AttendanceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Device)
                  .WithMany()
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Location)
                  .WithMany()
                  .HasForeignKey(e => e.LocationId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ExpectedLocation)
                  .WithMany()
                  .HasForeignKey(e => e.ExpectedLocationId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ==============================================
        // DEVICE API KEY AUTHENTICATION
        // ==============================================

        // Configure DeviceApiKey entity
        modelBuilder.Entity<DeviceApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.ApiKeyHash).IsUnique();
            entity.HasIndex(e => new { e.DeviceId, e.IsActive });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.LastUsedAt);
            entity.HasIndex(e => new { e.IsActive, e.ExpiresAt });

            entity.Property(e => e.ApiKeyHash).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AllowedIpAddresses).HasColumnType("jsonb");

            // Add CHECK constraints
            entity.HasCheckConstraint(
                "chk_DeviceApiKeys_Hash_Length",
                "LENGTH(\"ApiKeyHash\") >= 64");

            entity.HasCheckConstraint(
                "chk_DeviceApiKeys_Description_NotEmpty",
                "LENGTH(TRIM(\"Description\")) > 0");

            // Relationship with AttendanceMachine (Device)
            entity.HasOne(e => e.Device)
                  .WithMany()
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Computed properties are ignored
            entity.Ignore(e => e.IsValid);
            entity.Ignore(e => e.IsExpired);
            entity.Ignore(e => e.DaysUntilExpiration);
            entity.Ignore(e => e.IsExpiringSoon);
            entity.Ignore(e => e.IsStale);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ==============================================
        // BIOMETRIC ATTENDANCE CAPTURE SYSTEM
        // ==============================================

        // Configure BiometricPunchRecord entity
        modelBuilder.Entity<BiometricPunchRecord>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Performance indexes for Fortune 500-grade query optimization
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.EmployeeId);
            entity.HasIndex(e => e.PunchTime);
            entity.HasIndex(e => e.ProcessingStatus);
            entity.HasIndex(e => new { e.TenantId, e.DeviceId, e.PunchTime });
            entity.HasIndex(e => new { e.EmployeeId, e.PunchTime });
            entity.HasIndex(e => new { e.ProcessingStatus, e.PunchTime });
            entity.HasIndex(e => new { e.DeviceId, e.DeviceUserId, e.PunchTime });
            entity.HasIndex(e => e.AttendanceId);
            entity.HasIndex(e => e.HashChain);

            // String field constraints
            entity.Property(e => e.DeviceUserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DeviceSerialNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PunchType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.VerificationMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PhotoPath).HasMaxLength(500);
            entity.Property(e => e.ProcessingStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProcessingError).HasMaxLength(2000);
            entity.Property(e => e.HashChain).IsRequired().HasMaxLength(256);

            // JSON field for raw data (PostgreSQL JSONB for efficient queries)
            entity.Property(e => e.RawData).HasColumnType("jsonb");

            // Decimal precision for GPS coordinates
            entity.Property(e => e.Latitude).HasColumnType("decimal(10,8)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(11,8)");

            // Foreign key relationships
            entity.HasOne(e => e.Device)
                  .WithMany()
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Employee)
                  .WithMany()
                  .HasForeignKey(e => e.EmployeeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Attendance)
                  .WithMany()
                  .HasForeignKey(e => e.AttendanceId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Soft delete query filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }
}
