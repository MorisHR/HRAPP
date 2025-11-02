namespace HRMS.Core.Settings;

/// <summary>
/// JWT authentication configuration settings
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// JWT signing secret key (MUST be stored in Secret Manager in production)
    /// Minimum length: 32 characters (256 bits)
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// JWT issuer (should match your API domain)
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// JWT audience (should match your client application)
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time in minutes
    /// Recommended: 15-60 minutes for production
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration time in days
    /// Recommended: 7-30 days for production
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Enable token rotation (issue new refresh token on each refresh)
    /// </summary>
    public bool EnableTokenRotation { get; set; } = true;
}
