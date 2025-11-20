using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HRMS.Infrastructure.Logging;

/// <summary>
/// FORTUNE 500 SECURITY EVENT LOGGER
///
/// Dedicated logger for security events with structured logging for SIEM integration.
/// Follows patterns from AWS CloudTrail, Azure Activity Log, Splunk CIM.
///
/// EVENT TYPES:
/// - Authentication (Login, Logout, MFA, Failed Login)
/// - Authorization (Access Denied, Privilege Escalation)
/// - Data Access (PII Access, Mass Export, Sensitive Query)
/// - Data Modification (Create, Update, Delete)
/// - Configuration Changes (Settings, Permissions, Roles)
/// - Security Violations (Brute Force, Rate Limit, IP Blacklist)
///
/// OUTPUT FORMAT: JSON structured logs compatible with Splunk/ELK/Azure Sentinel
/// COMPLIANCE: SOC 2, ISO 27001, PCI-DSS 10.2-10.3, NIST 800-53 AU-3, GDPR Article 30
/// </summary>
public interface ISecurityEventLogger
{
    void LogAuthenticationEvent(SecurityEventType eventType, string userId, string? email, string ipAddress,
        bool success, string? reason = null, Dictionary<string, object>? additionalData = null);

    void LogAuthorizationEvent(string userId, string resource, string action, bool success,
        string? reason = null, Dictionary<string, object>? additionalData = null);

    void LogDataAccessEvent(string userId, string resourceType, string resourceId,
        string action, int? recordCount = null, Dictionary<string, object>? additionalData = null);

    void LogDataModificationEvent(string userId, string entityType, string entityId,
        string action, object? oldValue = null, object? newValue = null,
        Dictionary<string, object>? additionalData = null);

    void LogConfigurationChangeEvent(string userId, string configurationType, string action,
        object? oldValue = null, object? newValue = null, Dictionary<string, object>? additionalData = null);

    void LogSecurityViolationEvent(SecurityViolationType violationType, string? userId, string ipAddress,
        string description, string severity, Dictionary<string, object>? additionalData = null);
}

public class SecurityEventLogger : ISecurityEventLogger
{
    private readonly ILogger<SecurityEventLogger> _logger;

    public SecurityEventLogger(ILogger<SecurityEventLogger> logger)
    {
        _logger = logger;
    }

    public void LogAuthenticationEvent(
        SecurityEventType eventType,
        string userId,
        string? email,
        string ipAddress,
        bool success,
        string? reason = null,
        Dictionary<string, object>? additionalData = null)
    {
        var securityEvent = new SecurityEvent
        {
            EventType = "Authentication",
            EventSubType = eventType.ToString(),
            UserId = userId,
            UserEmail = email,
            SourceIp = ipAddress,
            Success = success,
            Reason = reason,
            Severity = success ? "Info" : (eventType == SecurityEventType.FailedLogin ? "Warning" : "Info"),
            AdditionalData = additionalData
        };

        LogStructuredSecurityEvent(securityEvent, success ? LogLevel.Information : LogLevel.Warning);
    }

    public void LogAuthorizationEvent(
        string userId,
        string resource,
        string action,
        bool success,
        string? reason = null,
        Dictionary<string, object>? additionalData = null)
    {
        var securityEvent = new SecurityEvent
        {
            EventType = "Authorization",
            EventSubType = success ? "AccessGranted" : "AccessDenied",
            UserId = userId,
            Resource = resource,
            Action = action,
            Success = success,
            Reason = reason,
            Severity = success ? "Info" : "Warning",
            AdditionalData = additionalData
        };

        LogStructuredSecurityEvent(securityEvent, success ? LogLevel.Information : LogLevel.Warning);
    }

    public void LogDataAccessEvent(
        string userId,
        string resourceType,
        string resourceId,
        string action,
        int? recordCount = null,
        Dictionary<string, object>? additionalData = null)
    {
        var securityEvent = new SecurityEvent
        {
            EventType = "DataAccess",
            EventSubType = action,
            UserId = userId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            RecordCount = recordCount,
            Success = true,
            Severity = (recordCount.HasValue && recordCount.Value > 100) ? "Warning" : "Info",
            AdditionalData = additionalData
        };

        var logLevel = (recordCount.HasValue && recordCount.Value > 100) ? LogLevel.Warning : LogLevel.Information;
        LogStructuredSecurityEvent(securityEvent, logLevel);
    }

    public void LogDataModificationEvent(
        string userId,
        string entityType,
        string entityId,
        string action,
        object? oldValue = null,
        object? newValue = null,
        Dictionary<string, object>? additionalData = null)
    {
        var securityEvent = new SecurityEvent
        {
            EventType = "DataModification",
            EventSubType = action,
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
            NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
            Success = true,
            Severity = "Info",
            AdditionalData = additionalData
        };

        LogStructuredSecurityEvent(securityEvent, LogLevel.Information);
    }

    public void LogConfigurationChangeEvent(
        string userId,
        string configurationType,
        string action,
        object? oldValue = null,
        object? newValue = null,
        Dictionary<string, object>? additionalData = null)
    {
        var securityEvent = new SecurityEvent
        {
            EventType = "ConfigurationChange",
            EventSubType = configurationType,
            UserId = userId,
            Action = action,
            OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
            NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
            Success = true,
            Severity = "Info",
            AdditionalData = additionalData
        };

        LogStructuredSecurityEvent(securityEvent, LogLevel.Information);
    }

    public void LogSecurityViolationEvent(
        SecurityViolationType violationType,
        string? userId,
        string ipAddress,
        string description,
        string severity,
        Dictionary<string, object>? additionalData = null)
    {
        var securityEvent = new SecurityEvent
        {
            EventType = "SecurityViolation",
            EventSubType = violationType.ToString(),
            UserId = userId ?? "anonymous",
            SourceIp = ipAddress,
            Description = description,
            Success = false,
            Severity = severity,
            AdditionalData = additionalData
        };

        var logLevel = severity switch
        {
            "Critical" => LogLevel.Critical,
            "High" => LogLevel.Error,
            "Medium" => LogLevel.Warning,
            _ => LogLevel.Information
        };

        LogStructuredSecurityEvent(securityEvent, logLevel);
    }

    /// <summary>
    /// Log structured security event in JSON format for SIEM consumption
    /// </summary>
    private void LogStructuredSecurityEvent(SecurityEvent securityEvent, LogLevel logLevel)
    {
        // Add timestamp
        securityEvent.Timestamp = DateTimeOffset.UtcNow;

        // Serialize to JSON for structured logging
        var jsonEvent = JsonSerializer.Serialize(securityEvent, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Log with structured data
        _logger.Log(logLevel,
            "SECURITY_EVENT: {EventType}.{EventSubType} | User: {UserId} | Success: {Success} | Severity: {Severity} | Data: {SecurityEventJson}",
            securityEvent.EventType,
            securityEvent.EventSubType,
            securityEvent.UserId,
            securityEvent.Success,
            securityEvent.Severity,
            jsonEvent);
    }
}

/// <summary>
/// Security event model for structured logging
/// Compatible with Common Event Format (CEF), Splunk CIM, ELK ECS
/// </summary>
public class SecurityEvent
{
    public DateTimeOffset Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventSubType { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? SourceIp { get; set; }
    public string? Resource { get; set; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? Action { get; set; }
    public bool Success { get; set; }
    public string? Reason { get; set; }
    public string Severity { get; set; } = "Info";
    public string? Description { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public int? RecordCount { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Security event types for authentication
/// </summary>
public enum SecurityEventType
{
    Login,
    Logout,
    MfaVerification,
    FailedLogin,
    PasswordReset,
    PasswordChange,
    AccountLocked,
    AccountUnlocked,
    SessionExpired,
    SessionTerminated
}

/// <summary>
/// Security violation types
/// </summary>
public enum SecurityViolationType
{
    BruteForceAttempt,
    RateLimitExceeded,
    IpBlacklisted,
    UnauthorizedAccess,
    SuspiciousActivity,
    DataExfiltrationAttempt,
    PrivilegeEscalation,
    MaliciousRequest,
    SqlInjectionAttempt,
    XssAttempt,
    CsrfAttempt,
    InvalidToken,
    ExpiredToken,
    TamperedRequest
}
