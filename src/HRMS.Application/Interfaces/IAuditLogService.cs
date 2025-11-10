using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Production-grade audit logging service for comprehensive compliance tracking
/// Supports Mauritius Workers' Rights Act, Data Protection Act, and MRA Tax requirements
///
/// CRITICAL COMPLIANCE REQUIREMENTS:
/// - All operations are async for performance
/// - Logs are immutable (no update/delete)
/// - Supports 10+ year retention
/// - Provides complete audit trail
/// </summary>
public interface IAuditLogService
{
    // ============================================
    // GENERIC LOGGING METHODS
    // ============================================

    /// <summary>
    /// Generic audit logging method - foundation for all other logging methods
    /// Automatically captures HTTP context (IP, user agent, etc.)
    /// </summary>
    /// <param name="log">Audit log entry to create</param>
    /// <returns>Created audit log entry with generated ID</returns>
    Task<AuditLog> LogAsync(AuditLog log);

    /// <summary>
    /// Simplified logging method with auto-population of common fields
    /// </summary>
    /// <param name="actionType">Type of action performed</param>
    /// <param name="category">High-level category</param>
    /// <param name="severity">Severity level</param>
    /// <param name="entityType">Type of entity affected (optional)</param>
    /// <param name="entityId">ID of entity affected (optional)</param>
    /// <param name="oldValues">Old values before change (JSON, optional)</param>
    /// <param name="newValues">New values after change (JSON, optional)</param>
    /// <param name="success">Whether operation succeeded</param>
    /// <param name="errorMessage">Error message if failed (optional)</param>
    /// <param name="reason">User-provided reason (optional)</param>
    /// <returns>Created audit log entry</returns>
    Task<AuditLog> LogActionAsync(
        AuditActionType actionType,
        AuditCategory category,
        AuditSeverity severity,
        string? entityType = null,
        Guid? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        bool success = true,
        string? errorMessage = null,
        string? reason = null
    );

    // ============================================
    // SPECIALIZED LOGGING METHODS
    // ============================================

    /// <summary>
    /// Log authentication events (login, logout, password changes, MFA)
    /// AUTO-ENRICHES: IP address, user agent, session ID, correlation ID, user role from HttpContext
    /// </summary>
    /// <param name="actionType">Authentication action (LOGIN_SUCCESS, LOGIN_FAILED, etc.)</param>
    /// <param name="userId">User ID (if known)</param>
    /// <param name="userEmail">User email</param>
    /// <param name="success">Whether authentication succeeded</param>
    /// <param name="tenantId">Tenant ID (null for SuperAdmin)</param>
    /// <param name="errorMessage">Error message if failed</param>
    /// <param name="eventData">Event-specific data (MFA method, login duration, etc.)</param>
    Task<AuditLog> LogAuthenticationAsync(
        AuditActionType actionType,
        Guid? userId,
        string? userEmail,
        bool success,
        Guid? tenantId = null,
        string? errorMessage = null,
        Dictionary<string, object>? eventData = null
    );

    /// <summary>
    /// Log authorization events (access granted/denied, role changes, permission changes)
    /// </summary>
    /// <param name="actionType">Authorization action (ACCESS_GRANTED, ACCESS_DENIED, etc.)</param>
    /// <param name="userId">User ID attempting access</param>
    /// <param name="resourceType">Type of resource being accessed</param>
    /// <param name="resourceId">ID of resource being accessed</param>
    /// <param name="success">Whether access was granted</param>
    /// <param name="reason">Reason for denial (if denied)</param>
    Task<AuditLog> LogAuthorizationAsync(
        AuditActionType actionType,
        Guid userId,
        string resourceType,
        Guid? resourceId,
        bool success,
        string? reason = null
    );

    /// <summary>
    /// Log data changes with before/after values (CRUD operations)
    /// Automatically detects changed fields and formats JSON
    /// </summary>
    /// <param name="actionType">Data action (RECORD_CREATED, RECORD_UPDATED, etc.)</param>
    /// <param name="entityType">Type of entity changed</param>
    /// <param name="entityId">ID of entity changed</param>
    /// <param name="oldValues">Old entity state (will be serialized to JSON)</param>
    /// <param name="newValues">New entity state (will be serialized to JSON)</param>
    /// <param name="changedFields">List of changed field names (optional, auto-detected if not provided)</param>
    /// <param name="reason">Reason for change (optional but recommended for sensitive data)</param>
    Task<AuditLog> LogDataChangeAsync<T>(
        AuditActionType actionType,
        string entityType,
        Guid entityId,
        T? oldValues,
        T? newValues,
        string[]? changedFields = null,
        string? reason = null
    );

    /// <summary>
    /// Log tenant lifecycle events (create, activate, suspend, delete)
    /// </summary>
    /// <param name="actionType">Tenant lifecycle action</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="tenantName">Tenant name</param>
    /// <param name="performedBy">SuperAdmin who performed the action</param>
    /// <param name="reason">Reason for action</param>
    /// <param name="additionalInfo">Additional context (subscription details, etc.)</param>
    Task<AuditLog> LogTenantLifecycleAsync(
        AuditActionType actionType,
        Guid tenantId,
        string tenantName,
        string performedBy,
        string? reason = null,
        string? additionalInfo = null
    );

    /// <summary>
    /// Log security events (suspicious activity, failed logins, unauthorized access)
    /// Triggers real-time alerts for CRITICAL and EMERGENCY severity
    /// </summary>
    /// <param name="actionType">Security event type</param>
    /// <param name="severity">Severity level (WARNING, CRITICAL, EMERGENCY)</param>
    /// <param name="userId">User ID (if applicable)</param>
    /// <param name="description">Event description</param>
    /// <param name="additionalInfo">Additional context for security team</param>
    Task<AuditLog> LogSecurityEventAsync(
        AuditActionType actionType,
        AuditSeverity severity,
        Guid? userId,
        string description,
        string? additionalInfo = null
    );

    // ============================================
    // QUERY AND SEARCH METHODS
    // ============================================

    /// <summary>
    /// Query audit logs with filtering and pagination
    /// Supports role-based access control
    /// </summary>
    /// <param name="tenantId">Filter by tenant (null for SuperAdmin viewing all tenants)</param>
    /// <param name="userId">Filter by user (optional)</param>
    /// <param name="category">Filter by category (optional)</param>
    /// <param name="severity">Filter by severity (optional)</param>
    /// <param name="actionType">Filter by action type (optional)</param>
    /// <param name="entityType">Filter by entity type (optional)</param>
    /// <param name="entityId">Filter by entity ID (optional)</param>
    /// <param name="startDate">Filter by start date (optional)</param>
    /// <param name="endDate">Filter by end date (optional)</param>
    /// <param name="searchTerm">Search in user email, entity type, etc. (optional)</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size (max 1000)</param>
    /// <returns>Paginated list of audit logs</returns>
    Task<(List<AuditLog> Logs, int TotalCount)> QueryAsync(
        Guid? tenantId = null,
        Guid? userId = null,
        AuditCategory? category = null,
        AuditSeverity? severity = null,
        AuditActionType? actionType = null,
        string? entityType = null,
        Guid? entityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 50
    );

    /// <summary>
    /// Get complete change history for a specific entity
    /// Returns all audit logs ordered by timestamp (most recent first)
    /// </summary>
    /// <param name="entityType">Type of entity</param>
    /// <param name="entityId">ID of entity</param>
    /// <returns>List of audit logs for this entity</returns>
    Task<List<AuditLog>> GetEntityHistoryAsync(string entityType, Guid entityId);

    /// <summary>
    /// Get audit logs for a specific user (employee self-service view)
    /// Limited to last 90 days by default
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="days">Number of days to retrieve (max 90)</param>
    /// <returns>List of audit logs for this user</returns>
    Task<List<AuditLog>> GetUserActivityAsync(Guid userId, int days = 90);

    /// <summary>
    /// Export audit logs to CSV/JSON for compliance reporting
    /// </summary>
    /// <param name="tenantId">Tenant ID (null for all tenants)</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="format">Export format (csv or json)</param>
    /// <returns>Byte array of exported file</returns>
    Task<byte[]> ExportAsync(
        Guid? tenantId,
        DateTime startDate,
        DateTime endDate,
        string format = "csv"
    );

    // ============================================
    // STATISTICS AND MONITORING
    // ============================================

    /// <summary>
    /// Get audit log statistics for monitoring dashboard
    /// </summary>
    /// <param name="tenantId">Tenant ID (null for platform-wide stats)</param>
    /// <param name="days">Number of days to analyze</param>
    /// <returns>Statistics object</returns>
    Task<AuditLogStatistics> GetStatisticsAsync(Guid? tenantId, int days = 30);

    // ============================================
    // AUDIT LOG VIEWER API METHODS
    // ============================================

    /// <summary>
    /// Get paginated and filtered audit logs for the viewer UI
    /// </summary>
    Task<HRMS.Application.DTOs.AuditLog.PagedResult<HRMS.Application.DTOs.AuditLog.AuditLogDto>> GetAuditLogsAsync(
        HRMS.Application.DTOs.AuditLog.AuditLogFilterDto filter);

    /// <summary>
    /// Get detailed audit log by ID
    /// </summary>
    Task<HRMS.Application.DTOs.AuditLog.AuditLogDetailDto?> GetAuditLogByIdAsync(Guid id);

    /// <summary>
    /// Get statistics with date range filter
    /// </summary>
    Task<HRMS.Application.DTOs.AuditLog.AuditLogStatisticsDto> GetStatisticsAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? tenantId);

    /// <summary>
    /// Export audit logs to CSV format
    /// </summary>
    Task<string> ExportToCsvAsync(HRMS.Application.DTOs.AuditLog.AuditLogFilterDto filter);

    /// <summary>
    /// Get user activity summary for a tenant
    /// </summary>
    Task<List<HRMS.Application.DTOs.AuditLog.UserActivityDto>> GetUserActivityAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate);

    // ============================================
    // FORTUNE 500 ENHANCEMENT: SUPERADMIN ACTION LOGGING
    // ============================================

    /// <summary>
    /// Log SuperAdmin platform administration actions with enhanced accountability
    /// CRITICAL for SOC 2, GDPR, and audit compliance
    /// Automatically enriches with IP address, user agent, correlation ID from HttpContext
    /// </summary>
    /// <param name="actionType">SuperAdmin action type (TENANT_CREATED, TENANT_SUSPENDED, etc.)</param>
    /// <param name="superAdminId">ID of SuperAdmin performing the action</param>
    /// <param name="superAdminEmail">Email of SuperAdmin (for reporting)</param>
    /// <param name="targetTenantId">ID of tenant being affected (if applicable)</param>
    /// <param name="targetTenantName">Name of tenant being affected (if applicable)</param>
    /// <param name="description">Human-readable description of action</param>
    /// <param name="oldValues">Previous state (for updates/deletions)</param>
    /// <param name="newValues">New state (for creates/updates)</param>
    /// <param name="reason">Business reason for action (required for sensitive operations)</param>
    /// <param name="success">Whether operation succeeded</param>
    /// <param name="errorMessage">Error message if operation failed</param>
    /// <param name="additionalContext">Additional context (subscription details, pricing, etc.)</param>
    /// <returns>Created audit log entry</returns>
    Task<AuditLog> LogSuperAdminActionAsync(
        AuditActionType actionType,
        Guid superAdminId,
        string superAdminEmail,
        Guid? targetTenantId = null,
        string? targetTenantName = null,
        string? description = null,
        string? oldValues = null,
        string? newValues = null,
        string? reason = null,
        bool success = true,
        string? errorMessage = null,
        Dictionary<string, object>? additionalContext = null
    );
}

/// <summary>
/// Audit log statistics for monitoring dashboard
/// </summary>
public class AuditLogStatistics
{
    public int TotalLogs { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public int SecurityEvents { get; set; }
    public int CriticalEvents { get; set; }
    public Dictionary<AuditCategory, int> LogsByCategory { get; set; } = new();
    public Dictionary<AuditSeverity, int> LogsBySeverity { get; set; } = new();
    public Dictionary<string, int> TopUsers { get; set; } = new();
    public Dictionary<string, int> TopActions { get; set; } = new();
}
