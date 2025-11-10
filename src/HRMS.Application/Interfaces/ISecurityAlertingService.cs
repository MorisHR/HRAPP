using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Production-grade security alerting service for real-time threat detection and notification
/// Supports Fortune 500 compliance requirements (SOX, GDPR, ISO 27001, PCI-DSS)
///
/// CRITICAL FEATURES:
/// - Real-time alerting for CRITICAL/EMERGENCY severity events
/// - Multi-channel notifications (Email, SMS, Slack, SIEM)
/// - Anomaly detection integration
/// - Alert lifecycle management (acknowledge, assign, resolve)
/// - Configurable alert rules and thresholds
/// </summary>
public interface ISecurityAlertingService
{
    // ============================================
    // ALERT CREATION METHODS
    // ============================================

    /// <summary>
    /// Create a new security alert with automatic notification
    /// Automatically sends notifications based on severity and configuration
    /// </summary>
    /// <param name="alert">Security alert to create</param>
    /// <param name="sendNotifications">Whether to send notifications immediately (default: true)</param>
    /// <returns>Created security alert with generated ID</returns>
    Task<SecurityAlert> CreateAlertAsync(SecurityAlert alert, bool sendNotifications = true);

    /// <summary>
    /// Create a security alert from an audit log entry
    /// Automatically maps audit log fields to security alert
    /// </summary>
    /// <param name="auditLog">Source audit log entry</param>
    /// <param name="alertType">Type of security alert</param>
    /// <param name="title">Alert title/summary</param>
    /// <param name="description">Detailed description</param>
    /// <param name="riskScore">Risk score 0-100</param>
    /// <param name="recommendedActions">Recommended actions to take</param>
    /// <param name="sendNotifications">Whether to send notifications</param>
    /// <returns>Created security alert</returns>
    Task<SecurityAlert> CreateAlertFromAuditLogAsync(
        AuditLog auditLog,
        SecurityAlertType alertType,
        string title,
        string description,
        int riskScore,
        string? recommendedActions = null,
        bool sendNotifications = true
    );

    /// <summary>
    /// Create a security alert for failed login threshold
    /// Called when user exceeds failed login attempts
    /// </summary>
    /// <param name="userId">User ID who triggered alert</param>
    /// <param name="userEmail">User email</param>
    /// <param name="tenantId">Tenant ID (null for platform-level)</param>
    /// <param name="failedAttempts">Number of failed attempts</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="auditLogId">Related audit log ID</param>
    /// <returns>Created security alert</returns>
    Task<SecurityAlert> CreateFailedLoginAlertAsync(
        Guid? userId,
        string? userEmail,
        Guid? tenantId,
        int failedAttempts,
        string? ipAddress,
        Guid? auditLogId
    );

    /// <summary>
    /// Create a security alert for unauthorized access attempt
    /// </summary>
    Task<SecurityAlert> CreateUnauthorizedAccessAlertAsync(
        Guid? userId,
        string? userEmail,
        Guid? tenantId,
        string resourceAttempted,
        string? ipAddress,
        Guid? auditLogId
    );

    /// <summary>
    /// Create a security alert for mass data export
    /// </summary>
    Task<SecurityAlert> CreateMassDataExportAlertAsync(
        Guid? userId,
        string? userEmail,
        Guid? tenantId,
        string entityType,
        int recordCount,
        Guid? auditLogId
    );

    /// <summary>
    /// Create a security alert for after-hours access
    /// </summary>
    Task<SecurityAlert> CreateAfterHoursAccessAlertAsync(
        Guid? userId,
        string? userEmail,
        Guid? tenantId,
        string resourceAccessed,
        DateTime accessTime,
        Guid? auditLogId
    );

    /// <summary>
    /// Create a security alert for salary/financial data changes
    /// </summary>
    Task<SecurityAlert> CreateSalaryChangeAlertAsync(
        Guid? userId,
        string? userEmail,
        Guid? tenantId,
        Guid employeeId,
        string employeeName,
        decimal oldSalary,
        decimal newSalary,
        Guid? auditLogId
    );

    // ============================================
    // ALERT RETRIEVAL METHODS
    // ============================================

    /// <summary>
    /// Get security alert by ID
    /// </summary>
    Task<SecurityAlert?> GetAlertByIdAsync(Guid alertId);

    /// <summary>
    /// Get all security alerts with filtering and pagination
    /// </summary>
    /// <param name="tenantId">Filter by tenant (null for platform-level)</param>
    /// <param name="status">Filter by status</param>
    /// <param name="severity">Filter by severity</param>
    /// <param name="alertType">Filter by alert type</param>
    /// <param name="startDate">Filter by start date</param>
    /// <param name="endDate">Filter by end date</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>List of security alerts matching criteria</returns>
    Task<(List<SecurityAlert> alerts, int totalCount)> GetAlertsAsync(
        Guid? tenantId = null,
        SecurityAlertStatus? status = null,
        AuditSeverity? severity = null,
        SecurityAlertType? alertType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50
    );

    /// <summary>
    /// Get active (unresolved) alerts count by severity
    /// </summary>
    Task<Dictionary<AuditSeverity, int>> GetActiveAlertCountsBySeverityAsync(Guid? tenantId = null);

    /// <summary>
    /// Get recent critical alerts (last 24 hours)
    /// </summary>
    Task<List<SecurityAlert>> GetRecentCriticalAlertsAsync(Guid? tenantId = null, int hours = 24);

    // ============================================
    // ALERT LIFECYCLE MANAGEMENT
    // ============================================

    /// <summary>
    /// Acknowledge a security alert
    /// Sets status to ACKNOWLEDGED and records who acknowledged it
    /// </summary>
    Task<SecurityAlert> AcknowledgeAlertAsync(
        Guid alertId,
        Guid acknowledgedBy,
        string acknowledgedByEmail
    );

    /// <summary>
    /// Assign a security alert to a user for investigation
    /// </summary>
    Task<SecurityAlert> AssignAlertAsync(
        Guid alertId,
        Guid assignedTo,
        string assignedToEmail,
        Guid? assignedBy = null
    );

    /// <summary>
    /// Mark a security alert as in progress
    /// </summary>
    Task<SecurityAlert> MarkAlertInProgressAsync(Guid alertId);

    /// <summary>
    /// Resolve a security alert
    /// Sets status to RESOLVED and records resolution details
    /// </summary>
    Task<SecurityAlert> ResolveAlertAsync(
        Guid alertId,
        Guid resolvedBy,
        string resolvedByEmail,
        string resolutionNotes
    );

    /// <summary>
    /// Mark a security alert as false positive
    /// </summary>
    Task<SecurityAlert> MarkAlertAsFalsePositiveAsync(
        Guid alertId,
        Guid resolvedBy,
        string resolvedByEmail,
        string reason
    );

    /// <summary>
    /// Escalate a security alert
    /// Sends additional notifications to escalation contacts
    /// </summary>
    Task<SecurityAlert> EscalateAlertAsync(
        Guid alertId,
        string escalatedTo,
        Guid? escalatedBy = null
    );

    // ============================================
    // NOTIFICATION METHODS
    // ============================================

    /// <summary>
    /// Send notifications for a security alert
    /// Sends to all configured channels based on severity and alert type
    /// </summary>
    Task SendNotificationsAsync(SecurityAlert alert);

    /// <summary>
    /// Send email notification
    /// </summary>
    Task<bool> SendEmailNotificationAsync(SecurityAlert alert, List<string> recipients);

    /// <summary>
    /// Send SMS notification
    /// </summary>
    Task<bool> SendSmsNotificationAsync(SecurityAlert alert, List<string> phoneNumbers);

    /// <summary>
    /// Send Slack notification
    /// </summary>
    Task<bool> SendSlackNotificationAsync(SecurityAlert alert, List<string> channels);

    /// <summary>
    /// Send to SIEM system (Splunk, QRadar, Sentinel, Elastic)
    /// </summary>
    Task<bool> SendToSiemAsync(SecurityAlert alert);

    // ============================================
    // ALERT RULES & CONFIGURATION
    // ============================================

    /// <summary>
    /// Check if audit log should trigger security alert
    /// Based on configured rules and thresholds
    /// </summary>
    Task<(bool shouldAlert, SecurityAlertType? alertType, int riskScore)> ShouldTriggerAlertAsync(AuditLog auditLog);

    /// <summary>
    /// Calculate risk score for an alert
    /// Based on severity, user role, time of day, location, etc.
    /// </summary>
    int CalculateRiskScore(
        AuditSeverity severity,
        string? userRole = null,
        DateTime? eventTime = null,
        string? location = null,
        bool isRepeatOffender = false
    );

    // ============================================
    // ANALYTICS & REPORTING
    // ============================================

    /// <summary>
    /// Get alert statistics for a time period
    /// </summary>
    Task<SecurityAlertStatistics> GetAlertStatisticsAsync(
        Guid? tenantId,
        DateTime startDate,
        DateTime endDate
    );
}

/// <summary>
/// Security alert statistics DTO
/// </summary>
public class SecurityAlertStatistics
{
    public int TotalAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int ActiveAlerts { get; set; }
    public int FalsePositives { get; set; }
    public Dictionary<SecurityAlertType, int> AlertsByType { get; set; } = new();
    public Dictionary<AuditSeverity, int> AlertsBySeverity { get; set; } = new();
    public double AverageResolutionTimeHours { get; set; }
    public double AverageRiskScore { get; set; }
    public int HighRiskAlerts { get; set; } // Risk score >= 70
    public int EscalatedAlerts { get; set; }
}
