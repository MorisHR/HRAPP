using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Production-grade audit logging service implementation
/// Automatically captures HTTP context and populates audit log fields
/// Supports compliance requirements for Mauritius Workers' Rights Act, Data Protection Act
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly MasterDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        MasterDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditLogService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    // ============================================
    // GENERIC LOGGING METHODS
    // ============================================

    public async Task<AuditLog> LogAsync(AuditLog log)
    {
        try
        {
            // Auto-populate fields from HTTP context if not already set
            EnrichAuditLog(log);

            // Generate checksum for tamper detection
            log.Checksum = GenerateChecksum(log);

            // Ensure PerformedAt is set
            if (log.PerformedAt == default)
            {
                log.PerformedAt = DateTime.UtcNow;
            }

            // Add to database
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Audit log created: {ActionType} by {UserEmail} on {EntityType} {EntityId}",
                log.ActionType, log.UserEmail, log.EntityType, log.EntityId);

            return log;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log: {ActionType}", log.ActionType);
            throw;
        }
    }

    public async Task<AuditLog> LogActionAsync(
        AuditActionType actionType,
        AuditCategory category,
        AuditSeverity severity,
        string? entityType = null,
        Guid? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        bool success = true,
        string? errorMessage = null,
        string? reason = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = actionType,
            Category = category,
            Severity = severity,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            Success = success,
            ErrorMessage = errorMessage,
            Reason = reason,
            PerformedAt = DateTime.UtcNow
        };

        return await LogAsync(log);
    }

    // ============================================
    // SPECIALIZED LOGGING METHODS
    // ============================================

    public async Task<AuditLog> LogAuthenticationAsync(
        AuditActionType actionType,
        Guid? userId,
        string? userEmail,
        bool success,
        Guid? tenantId = null,
        string? errorMessage = null,
        Dictionary<string, object>? eventData = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = actionType,
            Category = AuditCategory.AUTHENTICATION,
            Severity = success ? AuditSeverity.INFO : AuditSeverity.WARNING,
            UserId = userId,
            UserEmail = userEmail,
            TenantId = tenantId,
            Success = success,
            ErrorMessage = errorMessage,
            AdditionalMetadata = eventData != null ? JsonSerializer.Serialize(eventData) : null,
            PerformedAt = DateTime.UtcNow
        };

        // Escalate to CRITICAL if multiple failed attempts detected
        if (!success && actionType == AuditActionType.LOGIN_FAILED && !string.IsNullOrEmpty(userEmail))
        {
            var recentFailures = await CountRecentFailedLogins(userEmail);
            if (recentFailures >= 3)
            {
                log.Severity = AuditSeverity.CRITICAL;
                _logger.LogWarning("Multiple failed login attempts detected for {Email}: {Count}",
                    userEmail, recentFailures + 1);
            }
        }

        return await LogAsync(log);
    }

    public async Task<AuditLog> LogAuthorizationAsync(
        AuditActionType actionType,
        Guid userId,
        string resourceType,
        Guid? resourceId,
        bool success,
        string? reason = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = actionType,
            Category = AuditCategory.AUTHORIZATION,
            Severity = success ? AuditSeverity.INFO : AuditSeverity.WARNING,
            UserId = userId,
            EntityType = resourceType,
            EntityId = resourceId,
            Success = success,
            Reason = reason,
            PerformedAt = DateTime.UtcNow
        };

        return await LogAsync(log);
    }

    public async Task<AuditLog> LogDataChangeAsync<T>(
        AuditActionType actionType,
        string entityType,
        Guid entityId,
        T? oldValues,
        T? newValues,
        string[]? changedFields = null,
        string? reason = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = actionType,
            Category = AuditCategory.DATA_CHANGE,
            Severity = AuditSeverity.INFO,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            ChangedFields = changedFields != null ? string.Join(", ", changedFields) : null,
            Reason = reason,
            Success = true,
            PerformedAt = DateTime.UtcNow
        };

        // Auto-detect changed fields if not provided
        if (changedFields == null && oldValues != null && newValues != null)
        {
            log.ChangedFields = DetectChangedFields(oldValues, newValues);
        }

        return await LogAsync(log);
    }

    public async Task<AuditLog> LogTenantLifecycleAsync(
        AuditActionType actionType,
        Guid tenantId,
        string tenantName,
        string performedBy,
        string? reason = null,
        string? additionalInfo = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = actionType,
            Category = AuditCategory.TENANT_LIFECYCLE,
            Severity = AuditSeverity.INFO,
            TenantId = tenantId,
            TenantName = tenantName,
            UserFullName = performedBy,
            Reason = reason,
            AdditionalMetadata = additionalInfo,
            Success = true,
            PerformedAt = DateTime.UtcNow
        };

        return await LogAsync(log);
    }

    public async Task<AuditLog> LogSecurityEventAsync(
        AuditActionType actionType,
        AuditSeverity severity,
        Guid? userId,
        string description,
        string? additionalInfo = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = actionType,
            Category = AuditCategory.SECURITY_EVENT,
            Severity = severity,
            UserId = userId,
            ErrorMessage = description,
            AdditionalMetadata = additionalInfo,
            Success = false,
            PerformedAt = DateTime.UtcNow
        };

        // Log security events immediately
        await LogAsync(log);

        // Trigger real-time alert for CRITICAL and EMERGENCY
        if (severity >= AuditSeverity.CRITICAL)
        {
            _logger.LogCritical(
                "SECURITY ALERT: {ActionType} - {Description}. User: {UserId}. Details: {Info}",
                actionType, description, userId, additionalInfo);
        }

        return log;
    }

    // ============================================
    // QUERY AND SEARCH METHODS
    // ============================================

    public async Task<(List<AuditLog> Logs, int TotalCount)> QueryAsync(
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
        int pageSize = 50)
    {
        try
        {
            // Validate pagination
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 1000);

            var query = _context.AuditLogs.AsQueryable();

            // Apply filters
            if (tenantId.HasValue)
                query = query.Where(l => l.TenantId == tenantId.Value);

            if (userId.HasValue)
                query = query.Where(l => l.UserId == userId.Value);

            if (category.HasValue)
                query = query.Where(l => l.Category == category.Value);

            if (severity.HasValue)
                query = query.Where(l => l.Severity == severity.Value);

            if (actionType.HasValue)
                query = query.Where(l => l.ActionType == actionType.Value);

            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(l => l.EntityType == entityType);

            if (entityId.HasValue)
                query = query.Where(l => l.EntityId == entityId.Value);

            if (startDate.HasValue)
                query = query.Where(l => l.PerformedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.PerformedAt <= endDate.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(l =>
                    l.UserEmail!.Contains(searchTerm) ||
                    l.UserFullName!.Contains(searchTerm) ||
                    l.EntityType!.Contains(searchTerm) ||
                    l.ErrorMessage!.Contains(searchTerm));
            }

            // Exclude archived logs by default
            query = query.Where(l => !l.IsArchived);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination and ordering
            var logs = await query
                .OrderByDescending(l => l.PerformedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query audit logs");
            throw;
        }
    }

    public async Task<List<AuditLog>> GetEntityHistoryAsync(string entityType, Guid entityId)
    {
        try
        {
            return await _context.AuditLogs
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .OrderByDescending(l => l.PerformedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity history: {EntityType} {EntityId}",
                entityType, entityId);
            throw;
        }
    }

    public async Task<List<AuditLog>> GetUserActivityAsync(Guid userId, int days = 90)
    {
        try
        {
            // Limit to max 90 days for performance
            days = Math.Clamp(days, 1, 90);

            var startDate = DateTime.UtcNow.AddDays(-days);

            return await _context.AuditLogs
                .Where(l => l.UserId == userId && l.PerformedAt >= startDate)
                .OrderByDescending(l => l.PerformedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user activity: {UserId}", userId);
            throw;
        }
    }

    public async Task<byte[]> ExportAsync(
        Guid? tenantId,
        DateTime startDate,
        DateTime endDate,
        string format = "csv")
    {
        try
        {
            var query = _context.AuditLogs
                .Where(l => l.PerformedAt >= startDate && l.PerformedAt <= endDate);

            if (tenantId.HasValue)
                query = query.Where(l => l.TenantId == tenantId.Value);

            var logs = await query
                .OrderBy(l => l.PerformedAt)
                .ToListAsync();

            if (format.ToLower() == "json")
            {
                var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                return Encoding.UTF8.GetBytes(json);
            }
            else
            {
                // CSV export
                var csv = new StringBuilder();
                csv.AppendLine("Timestamp,TenantId,TenantName,UserId,UserEmail,UserFullName,UserRole,ActionType,Category,Severity,EntityType,EntityId,Success,ErrorMessage,IpAddress,Reason");

                foreach (var log in logs)
                {
                    csv.AppendLine($"\"{log.PerformedAt:yyyy-MM-dd HH:mm:ss}\",\"{log.TenantId}\",\"{log.TenantName}\",\"{log.UserId}\",\"{log.UserEmail}\",\"{log.UserFullName}\",\"{log.UserRole}\",\"{log.ActionType}\",\"{log.Category}\",\"{log.Severity}\",\"{log.EntityType}\",\"{log.EntityId}\",\"{log.Success}\",\"{log.ErrorMessage}\",\"{log.IpAddress}\",\"{log.Reason}\"");
                }

                return Encoding.UTF8.GetBytes(csv.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export audit logs");
            throw;
        }
    }

    public async Task<AuditLogStatistics> GetStatisticsAsync(Guid? tenantId, int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var query = _context.AuditLogs.Where(l => l.PerformedAt >= startDate);

            if (tenantId.HasValue)
                query = query.Where(l => l.TenantId == tenantId.Value);

            var logs = await query.ToListAsync();

            var stats = new AuditLogStatistics
            {
                TotalLogs = logs.Count,
                SuccessfulOperations = logs.Count(l => l.Success),
                FailedOperations = logs.Count(l => !l.Success),
                SecurityEvents = logs.Count(l => l.Category == AuditCategory.SECURITY_EVENT),
                CriticalEvents = logs.Count(l => l.Severity >= AuditSeverity.CRITICAL),
                LogsByCategory = logs.GroupBy(l => l.Category)
                    .ToDictionary(g => g.Key, g => g.Count()),
                LogsBySeverity = logs.GroupBy(l => l.Severity)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TopUsers = logs.Where(l => l.UserEmail != null)
                    .GroupBy(l => l.UserEmail!)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TopActions = logs.GroupBy(l => l.ActionType.ToString())
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit log statistics");
            throw;
        }
    }

    // ============================================
    // PRIVATE HELPER METHODS
    // ============================================

    /// <summary>
    /// Automatically populate audit log fields from HTTP context
    /// </summary>
    private void EnrichAuditLog(AuditLog log)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // Extract user information from claims
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            if (log.UserId == null)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    log.UserId = userId;
                }
            }

            if (string.IsNullOrWhiteSpace(log.UserEmail))
            {
                log.UserEmail = user.FindFirst(ClaimTypes.Email)?.Value;
            }

            if (string.IsNullOrWhiteSpace(log.UserFullName))
            {
                log.UserFullName = user.FindFirst(ClaimTypes.Name)?.Value;
            }

            if (string.IsNullOrWhiteSpace(log.UserRole))
            {
                log.UserRole = user.FindFirst(ClaimTypes.Role)?.Value;
            }

            if (string.IsNullOrWhiteSpace(log.TenantName))
            {
                log.TenantName = user.FindFirst("TenantName")?.Value;
            }

            // Extract tenant ID from claims if not set
            if (log.TenantId == null)
            {
                var tenantIdClaim = user.FindFirst("TenantId");
                if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
                {
                    log.TenantId = tenantId;
                }
            }
        }

        // HTTP context information
        if (string.IsNullOrWhiteSpace(log.IpAddress))
        {
            log.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        }

        if (string.IsNullOrWhiteSpace(log.UserAgent))
        {
            log.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();
        }

        if (string.IsNullOrWhiteSpace(log.HttpMethod))
        {
            log.HttpMethod = httpContext.Request.Method;
        }

        if (string.IsNullOrWhiteSpace(log.RequestPath))
        {
            log.RequestPath = httpContext.Request.Path.ToString();
        }

        if (string.IsNullOrWhiteSpace(log.QueryString))
        {
            log.QueryString = httpContext.Request.QueryString.ToString();
        }

        // Session and correlation IDs
        if (string.IsNullOrWhiteSpace(log.SessionId))
        {
            log.SessionId = httpContext.Session?.Id;
        }

        if (string.IsNullOrWhiteSpace(log.CorrelationId))
        {
            log.CorrelationId = httpContext.TraceIdentifier;
        }
    }

    /// <summary>
    /// Generate SHA256 checksum for tamper detection
    /// </summary>
    private string GenerateChecksum(AuditLog log)
    {
        try
        {
            var data = $"{log.Id}|{log.ActionType}|{log.UserId}|{log.EntityType}|{log.EntityId}|{log.PerformedAt:O}";
            var bytes = Encoding.UTF8.GetBytes(data);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLower();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Detect changed fields between old and new values
    /// </summary>
    private string? DetectChangedFields<T>(T oldValues, T newValues)
    {
        try
        {
            var changes = new List<string>();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                var oldValue = prop.GetValue(oldValues);
                var newValue = prop.GetValue(newValues);

                if (!Equals(oldValue, newValue))
                {
                    changes.Add(prop.Name);
                }
            }

            return changes.Any() ? string.Join(", ", changes) : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Count recent failed login attempts for a user (last 15 minutes)
    /// </summary>
    private async Task<int> CountRecentFailedLogins(string userEmail)
    {
        try
        {
            var fifteenMinutesAgo = DateTime.UtcNow.AddMinutes(-15);

            return await _context.AuditLogs
                .Where(l =>
                    l.UserEmail == userEmail &&
                    l.ActionType == AuditActionType.LOGIN_FAILED &&
                    l.PerformedAt >= fifteenMinutesAgo)
                .CountAsync();
        }
        catch
        {
            return 0;
        }
    }
}
