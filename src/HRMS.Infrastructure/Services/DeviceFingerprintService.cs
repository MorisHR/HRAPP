using System.Security.Cryptography;
using System.Text;
using HRMS.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UAParser;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Device fingerprinting service for detecting token theft
/// FORTUNE 500 IMPLEMENTATION: Multi-factor device identification
/// Combines User-Agent, Accept-Language, and platform characteristics
/// </summary>
public class DeviceFingerprintService : IDeviceFingerprintService
{
    private readonly ILogger<DeviceFingerprintService> _logger;

    public DeviceFingerprintService(ILogger<DeviceFingerprintService> logger)
    {
        _logger = logger;
    }

    public string GenerateFingerprint(HttpContext httpContext)
    {
        try
        {
            var request = httpContext.Request;

            // Collect fingerprint components
            var userAgent = request.Headers.UserAgent.ToString();
            var acceptLanguage = request.Headers.AcceptLanguage.ToString();
            var acceptEncoding = request.Headers.AcceptEncoding.ToString();

            // Combine components (order matters for consistency)
            var fingerprintData = $"{userAgent}|{acceptLanguage}|{acceptEncoding}";

            // Generate SHA256 hash
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprintData));
            var fingerprint = Convert.ToHexString(hashBytes).ToLower();

            _logger.LogDebug("Device fingerprint generated: {Fingerprint} from {UserAgent}",
                fingerprint.Substring(0, 8) + "...", userAgent);

            return fingerprint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating device fingerprint");
            // Return a default fingerprint on error (fail open)
            return "unknown";
        }
    }

    public bool ValidateFingerprint(HttpContext httpContext, string tokenFingerprint)
    {
        try
        {
            if (string.IsNullOrEmpty(tokenFingerprint) || tokenFingerprint == "unknown")
            {
                // Legacy tokens without fingerprint - allow for backwards compatibility
                _logger.LogDebug("Token has no fingerprint, allowing access (legacy token)");
                return true;
            }

            var currentFingerprint = GenerateFingerprint(httpContext);

            var isValid = currentFingerprint.Equals(tokenFingerprint, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Device fingerprint mismatch! Token={TokenFingerprint}, Current={CurrentFingerprint}, UserAgent={UserAgent}",
                    tokenFingerprint.Substring(0, 8) + "...",
                    currentFingerprint.Substring(0, 8) + "...",
                    GetUserAgent(httpContext));
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating device fingerprint");
            // Fail open on errors to prevent lockouts
            return true;
        }
    }

    public string GetDeviceInfo(HttpContext httpContext)
    {
        try
        {
            var userAgent = GetUserAgent(httpContext);

            if (string.IsNullOrEmpty(userAgent) || userAgent == "Unknown")
            {
                return "Unknown Device";
            }

            // Parse user agent using UAParser library
            var uaParser = Parser.GetDefault();
            var clientInfo = uaParser.Parse(userAgent);

            var browser = !string.IsNullOrEmpty(clientInfo.UA.Family)
                ? $"{clientInfo.UA.Family} {clientInfo.UA.Major}"
                : "Unknown Browser";

            var os = !string.IsNullOrEmpty(clientInfo.OS.Family)
                ? $"{clientInfo.OS.Family} {clientInfo.OS.Major}"
                : "Unknown OS";

            var device = !string.IsNullOrEmpty(clientInfo.Device.Family) && clientInfo.Device.Family != "Other"
                ? clientInfo.Device.Family
                : null;

            var deviceInfo = device != null
                ? $"{browser} on {device} ({os})"
                : $"{browser} on {os}";

            return deviceInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing device info");
            return "Unknown Device";
        }
    }

    public string GetUserAgent(HttpContext httpContext)
    {
        try
        {
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            return string.IsNullOrEmpty(userAgent) ? "Unknown" : userAgent;
        }
        catch
        {
            return "Unknown";
        }
    }
}
