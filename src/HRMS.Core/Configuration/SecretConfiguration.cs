namespace HRMS.Core.Configuration;

/// <summary>
/// Configuration model for application secrets
/// All values MUST come from environment variables or Secret Manager
/// NEVER hardcode production secrets
/// </summary>
public class SecretConfiguration
{
    /// <summary>
    /// JWT signing secret - MUST be 32+ characters
    /// Environment variable: JWT_SECRET
    /// </summary>
    public string JwtSecret { get; set; } = string.Empty;

    /// <summary>
    /// Encryption key for PII data - MUST be 32+ characters for AES-256
    /// Environment variable: ENCRYPTION_KEY
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    /// <summary>
    /// SuperAdmin secret path for system login
    /// Environment variable: SUPERADMIN_SECRET_PATH
    /// </summary>
    public string SuperAdminSecretPath { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password for email service
    /// Environment variable: SMTP_PASSWORD
    /// </summary>
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>
    /// Redis connection string (if Redis is enabled)
    /// Environment variable: REDIS_CONNECTION_STRING
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// SIEM API key for security logging (if SIEM is enabled)
    /// Environment variable: SIEM_API_KEY
    /// </summary>
    public string? SiemApiKey { get; set; }

    /// <summary>
    /// Database password
    /// Environment variable: DB_PASSWORD
    /// </summary>
    public string DatabasePassword { get; set; } = string.Empty;

    /// <summary>
    /// Validate that all required secrets are configured
    /// </summary>
    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(JwtSecret) || JwtSecret.Length < 32)
            errors.Add("JWT_SECRET must be set and at least 32 characters");

        if (string.IsNullOrWhiteSpace(EncryptionKey) || EncryptionKey.Length < 32)
            errors.Add("ENCRYPTION_KEY must be set and at least 32 characters");

        if (string.IsNullOrWhiteSpace(SuperAdminSecretPath))
            errors.Add("SUPERADMIN_SECRET_PATH must be set");

        if (string.IsNullOrWhiteSpace(DatabasePassword))
            errors.Add("DB_PASSWORD must be set");

        if (errors.Any())
        {
            throw new InvalidOperationException(
                "❌ CRITICAL SECURITY ERROR - Required secrets not configured:\n" +
                string.Join("\n", errors.Select(e => $"  • {e}")) +
                "\n\nSet these environment variables before starting the application.");
        }
    }

    /// <summary>
    /// Check if optional secrets are configured
    /// </summary>
    public bool HasSmtpPassword => !string.IsNullOrWhiteSpace(SmtpPassword);
    public bool HasRedisConnectionString => !string.IsNullOrWhiteSpace(RedisConnectionString);
    public bool HasSiemApiKey => !string.IsNullOrWhiteSpace(SiemApiKey);
}
