using Microsoft.AspNetCore.Http;

namespace HRMS.Core.Interfaces;

/// <summary>
/// Service for generating and validating device fingerprints
/// FORTUNE 500 SECURITY: Detect token theft from different devices/browsers
/// Pattern: Google Account Security, AWS Console, Microsoft 365
/// </summary>
public interface IDeviceFingerprintService
{
    /// <summary>
    /// Generate a unique device fingerprint from HTTP request headers
    /// Combines User-Agent, Accept-Language, and other browser characteristics
    /// </summary>
    /// <param name="httpContext">HTTP context containing request headers</param>
    /// <returns>SHA256 hash of device fingerprint (32 characters)</returns>
    string GenerateFingerprint(HttpContext httpContext);

    /// <summary>
    /// Validate if current device matches the fingerprint in JWT claims
    /// </summary>
    /// <param name="httpContext">Current HTTP context</param>
    /// <param name="tokenFingerprint">Fingerprint from JWT token</param>
    /// <returns>True if fingerprints match</returns>
    bool ValidateFingerprint(HttpContext httpContext, string tokenFingerprint);

    /// <summary>
    /// Get human-readable device information for audit logs and user notifications
    /// </summary>
    /// <param name="httpContext">HTTP context</param>
    /// <returns>Device description (e.g., "Chrome 120 on Windows 10")</returns>
    string GetDeviceInfo(HttpContext httpContext);

    /// <summary>
    /// Extract user agent string from HTTP context
    /// </summary>
    /// <param name="httpContext">HTTP context</param>
    /// <returns>User agent string or "Unknown"</returns>
    string GetUserAgent(HttpContext httpContext);
}
