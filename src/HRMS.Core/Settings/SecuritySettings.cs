namespace HRMS.Core.Settings;

/// <summary>
/// Security configuration settings for production
/// </summary>
public class SecuritySettings
{
    /// <summary>
    /// Require HTTPS for JWT metadata validation (MUST be true in production)
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Enable API key validation for additional security
    /// </summary>
    public bool EnableApiKeyValidation { get; set; }

    /// <summary>
    /// List of allowed API keys (hashed)
    /// </summary>
    public List<string> AllowedApiKeys { get; set; } = new();

    /// <summary>
    /// Maximum login attempts before account lockout
    /// </summary>
    public int MaxLoginAttempts { get; set; } = 5;

    /// <summary>
    /// Account lockout duration in minutes
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Enable two-factor authentication
    /// </summary>
    public bool EnableTwoFactorAuth { get; set; }

    /// <summary>
    /// Minimum password length
    /// </summary>
    public int MinPasswordLength { get; set; } = 12;

    /// <summary>
    /// Require special character in password
    /// </summary>
    public bool RequireSpecialCharacter { get; set; } = true;

    /// <summary>
    /// Require digit in password
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    /// Require uppercase letter in password
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Password expiry days (0 = never expire)
    /// </summary>
    public int PasswordExpiryDays { get; set; } = 90;
}
