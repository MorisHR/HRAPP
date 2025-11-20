using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Jira integration configuration per tenant
/// Stores OAuth credentials and project mappings
/// </summary>
public class JiraIntegration : BaseEntity
{
    public required Guid TenantId { get; set; }

    /// <summary>
    /// Jira instance URL (e.g., "https://company.atlassian.net")
    /// </summary>
    public required string JiraInstanceUrl { get; set; }

    /// <summary>
    /// Encrypted OAuth access token
    /// SECURITY: Must be encrypted at rest using IEncryptionService
    /// </summary>
    public required string JiraApiTokenEncrypted { get; set; }

    /// <summary>
    /// Jira user email for API authentication
    /// </summary>
    public string? JiraUserEmail { get; set; }

    /// <summary>
    /// Whether Jira integration is enabled for this tenant
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Last successful sync timestamp
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>
    /// Webhook secret for validating incoming webhooks
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// JSON mapping of Jira Project Keys to HRMS Project IDs
    /// Example: {"PROJ": "guid1", "DEV": "guid2"}
    /// </summary>
    public string? ProjectMappingsJson { get; set; }

    /// <summary>
    /// Sync configuration
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 60; // Default: hourly

    /// <summary>
    /// Auto-sync enabled
    /// </summary>
    public bool AutoSyncEnabled { get; set; } = true;

    /// <summary>
    /// Last error message (for diagnostics)
    /// </summary>
    public string? LastSyncError { get; set; }

    /// <summary>
    /// Number of work logs synced
    /// </summary>
    public int TotalWorkLogsSynced { get; set; }

    /// <summary>
    /// Number of issues synced
    /// </summary>
    public int TotalIssuesSynced { get; set; }
}
