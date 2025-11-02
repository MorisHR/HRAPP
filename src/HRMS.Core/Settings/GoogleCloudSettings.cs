namespace HRMS.Core.Settings;

/// <summary>
/// Google Cloud Platform configuration settings
/// </summary>
public class GoogleCloudSettings
{
    /// <summary>
    /// Google Cloud Project ID
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Enable Google Secret Manager for retrieving secrets
    /// </summary>
    public bool SecretManagerEnabled { get; set; }

    /// <summary>
    /// Cloud SQL Instance Connection Name (format: project:region:instance)
    /// </summary>
    public string CloudSqlInstanceConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Enable Google Cloud Logging and Monitoring
    /// </summary>
    public bool EnableCloudLogging { get; set; }

    /// <summary>
    /// Service Account Key JSON file path (for local development)
    /// </summary>
    public string? ServiceAccountKeyPath { get; set; }
}
