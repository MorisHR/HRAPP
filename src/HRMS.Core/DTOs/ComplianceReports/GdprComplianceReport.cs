namespace HRMS.Core.DTOs.ComplianceReports;

/// <summary>
/// GDPR Right to Access report (Article 15)
/// </summary>
public class RightToAccessReport
{
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime ReportGeneratedAt { get; set; }
    public List<PersonalDataItem> PersonalData { get; set; } = new();
    public List<DataProcessingActivity> ProcessingActivities { get; set; } = new();
    public List<DataRecipient> DataRecipients { get; set; } = new();
    public int TotalAuditLogEntries { get; set; }
}

/// <summary>
/// GDPR Right to Be Forgotten report (Article 17)
/// </summary>
public class RightToBeForgottenReport
{
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime ReportGeneratedAt { get; set; }
    public List<DataDeletionItem> DataToDelete { get; set; } = new();
    public List<string> EntitiesAffected { get; set; } = new();
    public int TotalRecordsToDelete { get; set; }
    public bool HasLegalHoldConflict { get; set; }
    public string? LegalHoldReason { get; set; }
}

/// <summary>
/// GDPR Data Breach Notification report (Articles 33/34)
/// </summary>
public class DataBreachNotificationReport
{
    public Guid IncidentId { get; set; }
    public DateTime BreachDetectedAt { get; set; }
    public DateTime ReportGeneratedAt { get; set; }
    public string BreachType { get; set; } = string.Empty;
    public string BreachDescription { get; set; } = string.Empty;
    public int AffectedUsersCount { get; set; }
    public List<Guid> AffectedUserIds { get; set; } = new();
    public string DataCategories { get; set; } = string.Empty;
    public string ConsequencesAssessment { get; set; } = string.Empty;
    public string MeasuresTaken { get; set; } = string.Empty;
    public bool RequiresRegulatoryNotification { get; set; }
    public bool RequiresUserNotification { get; set; }
}

/// <summary>
/// GDPR Consent Audit report (Article 7)
/// </summary>
public class ConsentAuditReport
{
    public DateTime ReportGeneratedAt { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Guid? TenantId { get; set; }
    public int TotalConsents { get; set; }
    public int ActiveConsents { get; set; }
    public int WithdrawnConsents { get; set; }
    public List<ConsentRecord> ConsentRecords { get; set; } = new();
}

/// <summary>
/// Personal data item
/// </summary>
public class PersonalDataItem
{
    public string DataType { get; set; } = string.Empty;
    public string DataValue { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CollectedAt { get; set; }
}

/// <summary>
/// Data processing activity
/// </summary>
public class DataProcessingActivity
{
    public string ActivityType { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public string LegalBasis { get; set; } = string.Empty;
}

/// <summary>
/// Data recipient
/// </summary>
public class DataRecipient
{
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientType { get; set; } = string.Empty;
    public DateTime SharedAt { get; set; }
    public string Purpose { get; set; } = string.Empty;
}

/// <summary>
/// Data deletion item
/// </summary>
public class DataDeletionItem
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string DataDescription { get; set; } = string.Empty;
}

/// <summary>
/// Consent record
/// </summary>
public class ConsentRecord
{
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public string ConsentType { get; set; } = string.Empty;
    public DateTime ConsentGivenAt { get; set; }
    public DateTime? ConsentWithdrawnAt { get; set; }
    public bool IsActive { get; set; }
}
