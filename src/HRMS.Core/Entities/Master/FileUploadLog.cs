using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// File Upload Tracking Entity
/// FORTUNE 500 PATTERN: AWS S3 Access Logs, Azure Blob Analytics, Google Cloud Storage Insights
/// COMPLIANCE: GDPR Article 30 (Records of Processing), SOC 2 (Access Logging)
/// BUSINESS VALUE: Storage analytics, capacity planning, cost optimization
/// </summary>
public class FileUploadLog : BaseEntity
{
    /// <summary>
    /// Tenant that owns this file
    /// CRITICAL: Multi-tenant isolation - indexed for performance
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Original filename (sanitized)
    /// MAX 500 chars to prevent database overflow attacks
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// INDEXED: For storage analytics and top consumers queries
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// MIME type or file extension
    /// Examples: "application/pdf", "image/jpeg", ".csv"
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// HRMS module that uploaded the file
    /// VALUES: "Employees", "Payroll", "Attendance", "Leaves", "Documents", "Reports"
    /// INDEXED: For per-module storage breakdown
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Cloud storage path
    /// FORMAT: "gs://bucket-name/tenant-id/module/unique-filename"
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Storage class/tier for cost optimization
    /// VALUES: "STANDARD", "NEARLINE", "COLDLINE", "ARCHIVE"
    /// PATTERN: AWS S3 Storage Classes, GCS Storage Classes
    /// </summary>
    public string StorageClass { get; set; } = "STANDARD";

    /// <summary>
    /// User who uploaded the file
    /// AUDIT: Required for compliance (who uploaded what, when)
    /// </summary>
    public Guid UploadedBy { get; set; }

    /// <summary>
    /// User email for quick reference
    /// DENORMALIZED: Faster audit queries without JOIN
    /// </summary>
    public string UploadedByEmail { get; set; } = string.Empty;

    /// <summary>
    /// Upload timestamp (UTC)
    /// INDEXED: For time-series analytics and growth tracking
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// File access count
    /// ANALYTICS: Identify frequently vs rarely accessed files
    /// </summary>
    public int AccessCount { get; set; } = 0;

    /// <summary>
    /// Last access timestamp
    /// CLEANUP: Identify unused files for archival/deletion
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Soft delete flag (file still in storage but marked deleted)
    /// RECOVERY: 30-day grace period before permanent deletion
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Soft delete timestamp
    /// COMPLIANCE: Track data retention periods
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User who deleted the file
    /// AUDIT: Required for compliance
    /// </summary>
    public Guid? DeletedBy { get; set; }

    /// <summary>
    /// Permanent deletion date (when file removed from storage)
    /// SCHEDULE: DeletedAt + 30 days
    /// </summary>
    public DateTime? PermanentlyDeletedAt { get; set; }

    /// <summary>
    /// File hash (SHA-256) for duplicate detection
    /// OPTIMIZATION: Identify duplicate files to save storage
    /// </summary>
    public string? FileHash { get; set; }

    /// <summary>
    /// Is this file a duplicate of another?
    /// ANALYTICS: Track storage waste from duplicates
    /// </summary>
    public bool IsDuplicate { get; set; } = false;

    /// <summary>
    /// Reference to original file if duplicate
    /// DEDUPLICATION: Point to canonical copy
    /// </summary>
    public Guid? OriginalFileId { get; set; }

    /// <summary>
    /// File description/purpose (optional)
    /// USER INPUT: Helps with file management
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Related entity type
    /// VALUES: "Employee", "Payslip", "LeaveApplication", "AttendanceRecord"
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Related entity ID (polymorphic reference)
    /// EXAMPLE: Employee ID, Payslip ID, etc.
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// File tags for categorization (JSON array)
    /// EXAMPLE: ["contract", "confidential", "permanent"]
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Encryption status
    /// SECURITY: Track encrypted vs unencrypted files
    /// </summary>
    public bool IsEncrypted { get; set; } = false;

    /// <summary>
    /// Virus scan status
    /// SECURITY: Malware detection results
    /// </summary>
    public FileScanStatus ScanStatus { get; set; } = FileScanStatus.PENDING;

    /// <summary>
    /// Scan timestamp
    /// </summary>
    public DateTime? ScannedAt { get; set; }

    /// <summary>
    /// Cost in USD for storing this file (monthly)
    /// BILLING: Track storage costs per tenant
    /// </summary>
    public decimal MonthlyCostUSD { get; set; } = 0;

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
}

/// <summary>
/// File scan status for security
/// </summary>
public enum FileScanStatus
{
    PENDING,
    CLEAN,
    INFECTED,
    SCAN_FAILED
}
