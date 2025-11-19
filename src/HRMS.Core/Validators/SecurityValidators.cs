using System;

namespace HRMS.Core.Validators;

/// <summary>
/// Fortune 500-grade security validators for input sanitization
/// Defense-in-depth: Secondary protection layer against injection attacks
/// Created: Nov 19, 2025 - SQL Injection Prevention Initiative
/// </summary>
public static class SecurityValidators
{
    /// <summary>
    /// Validates tenant ID format to prevent SQL injection and other attacks
    /// Ensures only valid GUID format is accepted
    /// </summary>
    /// <param name="tenantId">Tenant ID to validate</param>
    /// <param name="paramName">Parameter name for exception message</param>
    /// <exception cref="ArgumentNullException">Thrown when tenantId is null or whitespace</exception>
    /// <exception cref="ArgumentException">Thrown when tenantId is not a valid GUID</exception>
    public static void ValidateTenantId(string? tenantId, string paramName = "tenantId")
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentNullException(paramName, "Tenant ID cannot be null or empty");
        }

        if (!Guid.TryParse(tenantId, out _))
        {
            throw new ArgumentException(
                $"Invalid tenant ID format. Expected valid GUID, got: {tenantId}",
                paramName);
        }
    }

    /// <summary>
    /// Validates tenant ID GUID to ensure it's not empty
    /// </summary>
    /// <param name="tenantId">Tenant ID GUID to validate</param>
    /// <param name="paramName">Parameter name for exception message</param>
    /// <exception cref="ArgumentException">Thrown when tenantId is Guid.Empty</exception>
    public static void ValidateTenantId(Guid tenantId, string paramName = "tenantId")
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty GUID", paramName);
        }
    }

    /// <summary>
    /// Validates refresh token format to prevent injection attacks
    /// Ensures token meets expected format requirements
    /// </summary>
    /// <param name="refreshToken">Refresh token to validate</param>
    /// <param name="paramName">Parameter name for exception message</param>
    /// <exception cref="ArgumentNullException">Thrown when token is null or whitespace</exception>
    /// <exception cref="ArgumentException">Thrown when token format is invalid</exception>
    public static void ValidateRefreshToken(string? refreshToken, string paramName = "refreshToken")
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentNullException(paramName, "Refresh token cannot be null or empty");
        }

        // Refresh tokens should be base64 or alphanumeric (minimum length 32 chars)
        if (refreshToken.Length < 32)
        {
            throw new ArgumentException(
                "Refresh token must be at least 32 characters long",
                paramName);
        }

        // Check for obvious SQL injection patterns (defense-in-depth)
        if (ContainsSqlInjectionPatterns(refreshToken))
        {
            throw new ArgumentException(
                "Refresh token contains invalid characters or patterns",
                paramName);
        }
    }

    /// <summary>
    /// Validates API key format to prevent injection attacks
    /// </summary>
    /// <param name="apiKey">API key to validate</param>
    /// <param name="paramName">Parameter name for exception message</param>
    /// <exception cref="ArgumentNullException">Thrown when API key is null or whitespace</exception>
    /// <exception cref="ArgumentException">Thrown when API key format is invalid</exception>
    public static void ValidateApiKey(string? apiKey, string paramName = "apiKey")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentNullException(paramName, "API key cannot be null or empty");
        }

        // API keys should be alphanumeric with hyphens (minimum 20 chars)
        if (apiKey.Length < 20)
        {
            throw new ArgumentException(
                "API key must be at least 20 characters long",
                paramName);
        }

        if (ContainsSqlInjectionPatterns(apiKey))
        {
            throw new ArgumentException(
                "API key contains invalid characters or patterns",
                paramName);
        }
    }

    /// <summary>
    /// Detects common SQL injection patterns in input strings
    /// Defense-in-depth validation layer
    /// </summary>
    /// <param name="input">Input string to check</param>
    /// <returns>True if SQL injection patterns detected, false otherwise</returns>
    private static bool ContainsSqlInjectionPatterns(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var lowerInput = input.ToLowerInvariant();

        // Check for common SQL injection patterns
        string[] sqlInjectionPatterns = new[]
        {
            "' or '",
            "\" or \"",
            "' or 1=1",
            "\" or 1=1",
            "'; drop ",
            "\"; drop ",
            "'; delete ",
            "\"; delete ",
            "'; update ",
            "\"; update ",
            "'; insert ",
            "\"; insert ",
            " union ",
            " select ",
            " exec(",
            " execute(",
            "xp_cmdshell",
            "--",
            "/*",
            "*/"
        };

        foreach (var pattern in sqlInjectionPatterns)
        {
            if (lowerInput.Contains(pattern))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sanitizes string input by removing potentially dangerous characters
    /// Use ONLY when parameterized queries are not possible
    /// WARNING: This is NOT a replacement for parameterized queries!
    /// </summary>
    /// <param name="input">Input to sanitize</param>
    /// <returns>Sanitized string</returns>
    public static string SanitizeStringInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove single quotes, double quotes, semicolons, and comment markers
        return input
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(";", "")
            .Replace("--", "")
            .Replace("/*", "")
            .Replace("*/", "");
    }
}
