namespace HRMS.Application.Interfaces;

/// <summary>
/// GDPR Article 15 & 20 - Right to Data Portability
/// FORTUNE 500 PATTERN: Complete user data package export
///
/// COMPLIANCE:
/// - GDPR Article 15: Right to access
/// - GDPR Article 20: Right to data portability
/// - CCPA: Consumer data access request
///
/// FUNCTIONALITY:
/// - Aggregates ALL user data from all tables
/// - Exports in JSON, CSV, PDF formats
/// - Includes audit logs, consents, employee data
/// - Machine-readable structured format
/// </summary>
public interface IGDPRDataExportService
{
    /// <summary>
    /// Generate complete user data export package
    /// PERFORMANCE: Parallel queries across multiple tables
    /// SECURITY: Authorization check before export
    /// </summary>
    Task<UserDataExportPackage> GenerateUserDataExportAsync(
        Guid userId,
        string format = "json", // json, csv, pdf
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get export as downloadable file bytes
    /// FORMATS: JSON, CSV, PDF
    /// </summary>
    Task<ExportFileResult> ExportUserDataToFileAsync(
        Guid userId,
        string format = "json",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Complete user data export package
/// Contains ALL personal data across the system
/// </summary>
public class UserDataExportPackage
{
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime ExportGeneratedAt { get; set; }
    public string ExportVersion { get; set; } = "1.0";

    // Identity Data
    public PersonalIdentityData? Identity { get; set; }

    // Employment Data (if employee)
    public EmployeeData? EmployeeProfile { get; set; }

    // Audit Logs
    public List<AuditLogEntry> AuditLogs { get; set; } = new();

    // Consents
    public List<ConsentEntry> Consents { get; set; } = new();

    // Session Data
    public List<SessionEntry> Sessions { get; set; } = new();

    // Leave Requests
    public List<LeaveRequestEntry> LeaveRequests { get; set; } = new();

    // Payroll Data
    public List<PayslipEntry> Payslips { get; set; } = new();

    // Attendance Records
    public List<AttendanceEntry> AttendanceRecords { get; set; } = new();

    // Timesheet Data
    public List<TimesheetExportEntry> Timesheets { get; set; } = new();

    // Security Alerts
    public List<SecurityAlertEntry> SecurityAlerts { get; set; } = new();

    // File Uploads
    public List<FileUploadEntry> FileUploads { get; set; } = new();

    // Processing Activities
    public List<DataProcessingActivityEntry> ProcessingActivities { get; set; } = new();

    // Data Recipients (who has accessed this data)
    public List<DataRecipientEntry> DataRecipients { get; set; } = new();

    // Metadata
    public ExportMetadata Metadata { get; set; } = new();
}

public class PersonalIdentityData
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class EmployeeData
{
    public string? EmployeeNumber { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public DateTime? HireDate { get; set; }
    public string? EmploymentStatus { get; set; }
    public string? Salary { get; set; }
    public string? Manager { get; set; }
}

public class AuditLogEntry
{
    public DateTime Timestamp { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? Description { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

public class ConsentEntry
{
    public DateTime GivenAt { get; set; }
    public string ConsentType { get; set; } = string.Empty;
    public string ConsentCategory { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? WithdrawnAt { get; set; }
}

public class SessionEntry
{
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivity { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public bool IsActive { get; set; }
}

public class LeaveRequestEntry
{
    public DateTime RequestedAt { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Days { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PayslipEntry
{
    public DateTime PayPeriodStart { get; set; }
    public DateTime PayPeriodEnd { get; set; }
    public decimal GrossPay { get; set; }
    public decimal NetPay { get; set; }
    public DateTime PaymentDate { get; set; }
}

public class AttendanceEntry
{
    public DateTime Date { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TimesheetExportEntry
{
    public DateTime WeekStarting { get; set; }
    public decimal TotalHours { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SecurityAlertEntry
{
    public DateTime DetectedAt { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class FileUploadEntry
{
    public DateTime UploadedAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileType { get; set; } = string.Empty;
}

public class DataProcessingActivityEntry
{
    public DateTime ProcessedAt { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = string.Empty;
}

public class DataRecipientEntry
{
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientType { get; set; } = string.Empty;
    public DateTime SharedAt { get; set; }
    public string Purpose { get; set; } = string.Empty;
}

public class ExportMetadata
{
    public int TotalAuditLogEntries { get; set; }
    public int TotalConsents { get; set; }
    public int TotalSessions { get; set; }
    public int TotalLeaveRequests { get; set; }
    public int TotalPayslips { get; set; }
    public int TotalAttendanceRecords { get; set; }
    public int TotalTimesheets { get; set; }
    public int TotalFilesUploaded { get; set; }
    public long TotalStorageUsedBytes { get; set; }
    public DateTime DataCollectionStartDate { get; set; }
    public DateTime DataCollectionEndDate { get; set; }
}

public class ExportFileResult
{
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
