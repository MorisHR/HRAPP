using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Employee entity - stored in Tenant schema
/// Each tenant has their own isolated Employee records
/// Supports both Local and Expatriate workers with full compliance tracking
/// </summary>
public class Employee : BaseEntity
{
    // ==========================================
    // BASIC INFORMATION
    // ==========================================

    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PersonalEmail { get; set; }
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public MaritalStatus MaritalStatus { get; set; }

    // ==========================================
    // ADDRESS INFORMATION (Mauritius Compliant)
    // ==========================================

    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string? Village { get; set; }
    public string? District { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "Mauritius";

    public string? BloodGroup { get; set; }

    // ==========================================
    // EMPLOYEE TYPE & NATIONALITY
    // ==========================================

    /// <summary>
    /// Employee classification: Local or Expatriate
    /// Determines which fields are mandatory
    /// </summary>
    public EmployeeType EmployeeType { get; set; } = EmployeeType.Local;

    /// <summary>
    /// Nationality (ISO country code or full name)
    /// Required for all employees
    /// </summary>
    public string Nationality { get; set; } = "Mauritius";

    /// <summary>
    /// Country of origin
    /// Required for expatriates
    /// </summary>
    public string? CountryOfOrigin { get; set; }

    // ==========================================
    // IDENTIFICATION DOCUMENTS
    // ==========================================

    /// <summary>
    /// National ID Card (for Mauritians)
    /// Format: A1234567 or similar
    /// </summary>
    public string? NationalIdCard { get; set; }

    /// <summary>
    /// Passport Number
    /// Required for expatriates, optional for locals
    /// </summary>
    public string? PassportNumber { get; set; }

    /// <summary>
    /// Passport issue date
    /// </summary>
    public DateTime? PassportIssueDate { get; set; }

    /// <summary>
    /// Passport expiry date
    /// CRITICAL: Required for expatriates
    /// Triggers automated alerts at 90, 60, 30, 15, 7 days before expiry
    /// </summary>
    public DateTime? PassportExpiryDate { get; set; }

    // ==========================================
    // VISA / WORK PERMIT (EXPATRIATES)
    // ==========================================

    /// <summary>
    /// Type of visa/permit held by expatriate
    /// Required for expatriates
    /// </summary>
    public VisaType? VisaType { get; set; }

    /// <summary>
    /// Visa/Permit number
    /// </summary>
    public string? VisaNumber { get; set; }

    /// <summary>
    /// Visa issue date
    /// </summary>
    public DateTime? VisaIssueDate { get; set; }

    /// <summary>
    /// Visa expiry date
    /// CRITICAL: Required for expatriates
    /// Triggers automated alerts at 90, 60, 45, 30, 15 days before expiry
    /// Auto-suspends employee access if expired
    /// </summary>
    public DateTime? VisaExpiryDate { get; set; }

    /// <summary>
    /// Work Permit number (if different from visa number)
    /// </summary>
    public string? WorkPermitNumber { get; set; }

    /// <summary>
    /// Work Permit expiry date
    /// </summary>
    public DateTime? WorkPermitExpiryDate { get; set; }

    // ==========================================
    // TAX & STATUTORY
    // ==========================================

    /// <summary>
    /// Tax residency status
    /// Determines tax treatment
    /// </summary>
    public TaxResidentStatus TaxResidentStatus { get; set; } = TaxResidentStatus.Resident;

    /// <summary>
    /// Tax ID / Revenue Number
    /// </summary>
    public string? TaxIdNumber { get; set; }

    /// <summary>
    /// CSG (Contribution Sociale Généralisée) Number
    /// </summary>
    public string? CSGNumber { get; set; }

    /// <summary>
    /// NPF (National Pensions Fund) Number
    /// May not apply to all expatriates
    /// </summary>
    public string? NPFNumber { get; set; }

    /// <summary>
    /// NSF (National Savings Fund) Number
    /// Usually not applicable to expatriates
    /// </summary>
    public string? NSFNumber { get; set; }

    /// <summary>
    /// PRGF (Portable Retirement Gratuity Fund) Number
    /// Check eligibility based on employment type
    /// </summary>
    public string? PRGFNumber { get; set; }

    /// <summary>
    /// Is this employee eligible for NPF contributions?
    /// </summary>
    public bool IsNPFEligible { get; set; } = true;

    /// <summary>
    /// Is this employee eligible for NSF contributions?
    /// </summary>
    public bool IsNSFEligible { get; set; } = true;

    // ==========================================
    // EMPLOYMENT DETAILS
    // ==========================================

    /// <summary>
    /// Employment Type: Permanent, Contract, Temporary, PartTime
    /// </summary>
    public string EmploymentContractType { get; set; } = "Permanent";

    /// <summary>
    /// Employment Status: Active, Probation, Terminated, Resigned
    /// </summary>
    public string EmploymentStatus { get; set; } = "Active";

    /// <summary>
    /// Industry Sector (inherited from tenant, can be overridden)
    /// </summary>
    public string IndustrySector { get; set; } = string.Empty;

    /// <summary>
    /// Designation/Position
    /// </summary>
    public string Designation { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public string? WorkLocation { get; set; }
    public DateTime JoiningDate { get; set; }
    public int? ProbationPeriodMonths { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public DateTime? ConfirmationDate { get; set; }
    public DateTime? ResignationDate { get; set; }
    public DateTime? LastWorkingDate { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Employment contract end date (for fixed-term contracts)
    /// Common for expatriates
    /// </summary>
    public DateTime? ContractEndDate { get; set; }

    // ==========================================
    // SALARY & BANK DETAILS
    // ==========================================

    public decimal BasicSalary { get; set; }
    public string? PaymentFrequency { get; set; } = "Monthly";
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankBranch { get; set; }
    public string? BankSwiftCode { get; set; }

    /// <summary>
    /// Currency code for salary (MUR, USD, EUR, etc.)
    /// Useful for expatriates paid in foreign currency
    /// </summary>
    public string SalaryCurrency { get; set; } = "MUR";

    // ==========================================
    // ALLOWANCES
    // ==========================================

    public decimal? TransportAllowance { get; set; }
    public decimal? HousingAllowance { get; set; }
    public decimal? MealAllowance { get; set; }

    // ==========================================
    // EMERGENCY CONTACTS
    // ==========================================

    /// <summary>
    /// Collection of emergency contacts
    /// Expatriates should have both local and home country contacts
    /// </summary>
    public virtual ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();

    // ==========================================
    // LEAVE BALANCES & ENTITLEMENTS
    // ==========================================

    public int AnnualLeaveDays { get; set; } = 20;
    public int SickLeaveDays { get; set; } = 15;
    public int CasualLeaveDays { get; set; } = 5;
    public bool CarryForwardAllowed { get; set; } = true;
    public decimal AnnualLeaveBalance { get; set; }
    public decimal SickLeaveBalance { get; set; }
    public decimal CasualLeaveBalance { get; set; }

    // ==========================================
    // QUALIFICATIONS & SKILLS
    // ==========================================

    public string? HighestQualification { get; set; }
    public string? University { get; set; }
    public string? Skills { get; set; }
    public string? Languages { get; set; }

    // ==========================================
    // DOCUMENTS & FILE PATHS
    // ==========================================

    public string? ResumeFilePath { get; set; }
    public string? IdCopyFilePath { get; set; }
    public string? CertificatesFilePath { get; set; }
    public string? ContractFilePath { get; set; }

    // ==========================================
    // ADDITIONAL INFORMATION
    // ==========================================

    public string? Notes { get; set; }

    // ==========================================
    // DOCUMENT EXPIRY TRACKING
    // ==========================================

    /// <summary>
    /// Passport expiry status (calculated property)
    /// Used for dashboard widgets and alerts
    /// </summary>
    public DocumentExpiryStatus PassportExpiryStatus
    {
        get
        {
            if (!PassportExpiryDate.HasValue)
                return EmployeeType == EmployeeType.Expatriate ? DocumentExpiryStatus.Expired : DocumentExpiryStatus.NotApplicable;

            var daysUntilExpiry = (PassportExpiryDate.Value - DateTime.UtcNow).Days;

            if (daysUntilExpiry < 0) return DocumentExpiryStatus.Expired;
            if (daysUntilExpiry < 15) return DocumentExpiryStatus.Critical;
            if (daysUntilExpiry < 30) return DocumentExpiryStatus.ExpiringVerySoon;
            if (daysUntilExpiry < 90) return DocumentExpiryStatus.ExpiringSoon;
            return DocumentExpiryStatus.Valid;
        }
    }

    /// <summary>
    /// Visa/Work Permit expiry status (calculated property)
    /// </summary>
    public DocumentExpiryStatus VisaExpiryStatus
    {
        get
        {
            if (!VisaExpiryDate.HasValue)
                return EmployeeType == EmployeeType.Expatriate ? DocumentExpiryStatus.Expired : DocumentExpiryStatus.NotApplicable;

            var daysUntilExpiry = (VisaExpiryDate.Value - DateTime.UtcNow).Days;

            if (daysUntilExpiry < 0) return DocumentExpiryStatus.Expired;
            if (daysUntilExpiry < 15) return DocumentExpiryStatus.Critical;
            if (daysUntilExpiry < 30) return DocumentExpiryStatus.ExpiringVerySoon;
            if (daysUntilExpiry < 90) return DocumentExpiryStatus.ExpiringSoon;
            return DocumentExpiryStatus.Valid;
        }
    }

    /// <summary>
    /// Are any critical documents expired?
    /// If true, should auto-suspend employee access
    /// </summary>
    public bool HasExpiredDocuments => PassportExpiryStatus == DocumentExpiryStatus.Expired || VisaExpiryStatus == DocumentExpiryStatus.Expired;

    /// <summary>
    /// Are any documents expiring soon (within 30 days)?
    /// </summary>
    public bool HasDocumentsExpiringSoon =>
        PassportExpiryStatus == DocumentExpiryStatus.Critical ||
        PassportExpiryStatus == DocumentExpiryStatus.ExpiringVerySoon ||
        VisaExpiryStatus == DocumentExpiryStatus.Critical ||
        VisaExpiryStatus == DocumentExpiryStatus.ExpiringVerySoon;

    // ==========================================
    // OFFBOARDING
    // ==========================================

    public string? OffboardingReason { get; set; }
    public bool IsOffboarded { get; set; }
    public DateTime? OffboardingDate { get; set; }
    public string? OffboardingNotes { get; set; }

    // ==========================================
    // COMPUTED PROPERTIES
    // ==========================================

    public string FullName => string.IsNullOrEmpty(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";

    public int YearsOfService
    {
        get
        {
            var endDate = LastWorkingDate ?? DateTime.UtcNow;
            var years = endDate.Year - JoiningDate.Year;
            if (endDate.Month < JoiningDate.Month || (endDate.Month == JoiningDate.Month && endDate.Day < JoiningDate.Day))
                years--;
            return years;
        }
    }

    public int Age
    {
        get
        {
            var today = DateTime.UtcNow;
            var age = today.Year - DateOfBirth.Year;
            if (today.Month < DateOfBirth.Month || (today.Month == DateOfBirth.Month && today.Day < DateOfBirth.Day))
                age--;
            return age;
        }
    }

    /// <summary>
    /// Is this employee an expatriate?
    /// </summary>
    public bool IsExpatriate => EmployeeType == EmployeeType.Expatriate;

    // ==============================================
    // SECURITY: Authentication & Account Lockout Fields
    // ==============================================

    /// <summary>
    /// Argon2 hashed password for employee login authentication
    /// Required for all employees who need system access
    /// </summary>
    public string? PasswordHash { get; set; }

    public bool LockoutEnabled { get; set; } = true;
    public DateTime? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; } = 0;

    /// <summary>
    /// Password reset token for secure password setup/reset flow
    /// Single-use, time-limited (24 hours expiry)
    /// Used for: Initial password setup, forgot password flow
    /// </summary>
    public string? PasswordResetToken { get; set; }

    /// <summary>
    /// Password reset token expiration timestamp
    /// Default: 24 hours from token generation
    /// Fortune 500 best practice: Short-lived tokens for security
    /// </summary>
    public DateTime? PasswordResetTokenExpiry { get; set; }

    /// <summary>
    /// Force Password Change: Flag to force password change on next login
    /// Used for: Initial login after activation, security breach, admin-forced reset
    /// Fortune 500 compliance: Ensures users set their own passwords
    /// </summary>
    public bool MustChangePassword { get; set; } = false;

    /// <summary>
    /// Last password change date for password rotation policies
    /// Fortune 500 requirement: Track password age for compliance
    /// </summary>
    public DateTime? LastPasswordChangeDate { get; set; }

    /// <summary>
    /// Password history: JSON array of last 5 password hashes
    /// FORTUNE 500 COMPLIANCE: Prevents password reuse (PCI-DSS, NIST 800-63B)
    /// Format: ["hash1", "hash2", "hash3", "hash4", "hash5"]
    /// Maintained as rolling window - oldest removed when 6th password added
    /// </summary>
    public string? PasswordHistory { get; set; }

    /// <summary>
    /// Two-Factor Authentication: Is 2FA enabled for this employee?
    /// Fortune 500 best practice: MFA for privileged/admin users
    /// </summary>
    public bool IsTwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// Two-Factor Authentication: TOTP secret key (encrypted)
    /// Used for Google Authenticator, Authy, etc.
    /// </summary>
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// Is this employee an administrator for the tenant?
    /// Admins have full access to tenant data and settings
    /// </summary>
    public bool IsAdmin { get; set; } = false;

    // ==============================================
    // LOCATION & BIOMETRIC ACCESS
    // ==============================================

    /// <summary>
    /// Primary work location - employee can use all biometric devices at this location
    /// CRITICAL for multi-location attendance tracking
    /// </summary>
    public Guid? PrimaryLocationId { get; set; }
    public Location? PrimaryLocation { get; set; }

    /// <summary>
    /// Biometric enrollment ID from the device (fingerprint/face template ID)
    /// Used to match device punch records to this employee
    /// </summary>
    public string? BiometricEnrollmentId { get; set; }

    /// <summary>
    /// Date when employee was enrolled on biometric device
    /// </summary>
    public DateTime? BiometricEnrollmentDate { get; set; }

    // Navigation Properties
    public ICollection<EmployeeDeviceAccess> DeviceAccesses { get; set; } = new List<EmployeeDeviceAccess>();
    public ICollection<AttendanceAnomaly> AttendanceAnomalies { get; set; } = new List<AttendanceAnomaly>();
}
