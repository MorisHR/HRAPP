using System;
using System.Text.Json;

namespace HRMS.Application.DTOs.Monitoring;

/// <summary>
/// FORTUNE 50: Security event and threat detection metrics
/// Monitors authentication failures, IDOR attempts, and suspicious activity
/// COMPLIANCE: SOC 2, ISO 27001, PCI-DSS security logging requirements
/// </summary>
public class SecurityEventDto
{
    /// <summary>
    /// Unique event identifier
    /// </summary>
    public long EventId { get; set; }

    /// <summary>
    /// Event type: FailedLogin, IdorAttempt, RateLimitExceeded, UnauthorizedAccess, etc.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Severity level: Critical, High, Medium, Low, Info
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// User ID involved in the event (if applicable)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// User email (for display purposes)
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// Source IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Tenant subdomain (for multi-tenant events)
    /// </summary>
    public string? TenantSubdomain { get; set; }

    /// <summary>
    /// Resource ID attempted to access (for IDOR detection)
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// API endpoint involved in the event
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Whether the event was blocked by security controls
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Event description for human readability
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional event details as JSON
    /// </summary>
    public JsonDocument? Details { get; set; }

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Whether this event has been reviewed by security team
    /// </summary>
    public bool IsReviewed { get; set; }

    /// <summary>
    /// Notes from security team review
    /// </summary>
    public string? ReviewNotes { get; set; }

    /// <summary>
    /// Who reviewed this event
    /// </summary>
    public string? ReviewedBy { get; set; }

    /// <summary>
    /// When the event was reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }
}
