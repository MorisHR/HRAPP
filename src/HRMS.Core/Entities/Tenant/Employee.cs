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
    public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? PostalCode { get; set; }

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

    public string JobTitle { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public DateTime JoiningDate { get; set; }
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
    // EMERGENCY CONTACTS
    // ==========================================

    /// <summary>
    /// Collection of emergency contacts
    /// Expatriates should have both local and home country contacts
    /// </summary>
    public virtual ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();

    // ==========================================
    // LEAVE BALANCES
    // ==========================================

    public decimal AnnualLeaveBalance { get; set; }
    public decimal SickLeaveBalance { get; set; }
    public decimal CasualLeaveBalance { get; set; }

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
    // SECURITY: Account Lockout Fields
    // ==============================================
    public bool LockoutEnabled { get; set; } = true;
    public DateTime? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; } = 0;
}
