namespace HRMS.Core.DTOs.ComplianceReports;

/// <summary>
/// SOX compliance report (Sarbanes-Oxley Act Section 404)
/// </summary>
public class SoxComplianceReport
{
    public DateTime ReportGeneratedAt { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }

    public FinancialDataAccessSummary FinancialDataAccess { get; set; } = new();
    public UserAccessChangesSummary UserAccessChanges { get; set; } = new();
    public ITGCSummary ITGCControls { get; set; } = new();
    public List<SoxViolation> Violations { get; set; } = new();

    public int TotalFinancialTransactions { get; set; }
    public int UnauthorizedAccessAttempts { get; set; }
    public int SegregationOfDutiesViolations { get; set; }
    public bool IsCompliant { get; set; }
}

/// <summary>
/// Financial data access summary
/// </summary>
public class FinancialDataAccessSummary
{
    public int TotalAccessEvents { get; set; }
    public int UniqueUsersAccessed { get; set; }
    public int SalaryAccessEvents { get; set; }
    public int PayrollAccessEvents { get; set; }
    public int FinancialReportAccessEvents { get; set; }
    public int AfterHoursAccessEvents { get; set; }
    public List<TopAccessor> TopAccessors { get; set; } = new();
}

/// <summary>
/// User access changes summary (segregation of duties)
/// </summary>
public class UserAccessChangesSummary
{
    public int TotalRoleChanges { get; set; }
    public int PrivilegeEscalations { get; set; }
    public int PermissionModifications { get; set; }
    public int UserCreations { get; set; }
    public int UserDeletions { get; set; }
    public List<AccessChangeDetail> RecentChanges { get; set; } = new();
}

/// <summary>
/// IT General Controls summary
/// </summary>
public class ITGCSummary
{
    public int TotalSecurityEvents { get; set; }
    public int FailedLoginAttempts { get; set; }
    public int SuccessfulLogins { get; set; }
    public int DataExportEvents { get; set; }
    public int ConfigurationChanges { get; set; }
    public int AuditLogAccessEvents { get; set; }
    public double SystemAvailability { get; set; }
    public bool ChangeManagementCompliant { get; set; }
}

/// <summary>
/// SOX violation detail
/// </summary>
public class SoxViolation
{
    public Guid Id { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? AuditLogId { get; set; }
}

/// <summary>
/// Top accessor detail
/// </summary>
public class TopAccessor
{
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public int AccessCount { get; set; }
}

/// <summary>
/// Access change detail
/// </summary>
public class AccessChangeDetail
{
    public DateTime ChangedAt { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? ChangedBy { get; set; }
}
