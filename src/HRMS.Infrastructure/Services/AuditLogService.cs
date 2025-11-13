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
    private readonly ISecurityAlertingService? _securityAlertingService;

    public AuditLogService(
        MasterDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditLogService> logger,
        ISecurityAlertingService? securityAlertingService = null)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _securityAlertingService = securityAlertingService;
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

            // PHASE 2: Check if security alert should be triggered
            if (_securityAlertingService != null)
            {
                try
                {
                    var (shouldAlert, alertType, riskScore) = await _securityAlertingService.ShouldTriggerAlertAsync(log);

                    if (shouldAlert && alertType.HasValue)
                    {
                        _logger.LogWarning(
                            "Security alert triggered: {AlertType} for audit log {AuditLogId} (Risk Score: {RiskScore})",
                            alertType.Value, log.Id, riskScore);

                        // Create security alert based on type
                        await _securityAlertingService.CreateAlertFromAuditLogAsync(
                            log,
                            alertType.Value,
                            $"Security Alert: {alertType.Value}",
                            $"Suspicious activity detected: {log.ActionType} by {log.UserEmail ?? "Unknown User"}",
                            riskScore,
                            $"Review audit log {log.Id} for details. Investigate user activity and take appropriate action.",
                            sendNotifications: true
                        );
                    }
                }
                catch (Exception ex)
                {
                    // Don't fail audit logging if alerting fails
                    _logger.LogError(ex, "Failed to trigger security alert for audit log {AuditLogId}", log.Id);
                }
            }

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
    // AUDIT LOG VIEWER METHODS (for Controllers)
    // ============================================

    /// <summary>
    /// Get paginated audit logs with filtering
    /// Used by audit log viewer UI
    /// </summary>
    public async Task<HRMS.Application.DTOs.AuditLog.PagedResult<HRMS.Application.DTOs.AuditLog.AuditLogDto>> GetAuditLogsAsync(
        HRMS.Application.DTOs.AuditLog.AuditLogFilterDto filter)
    {
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            // Apply filters
            if (filter.TenantId.HasValue)
                query = query.Where(l => l.TenantId == filter.TenantId.Value);

            if (filter.StartDate.HasValue)
                query = query.Where(l => l.PerformedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(l => l.PerformedAt <= filter.EndDate.Value);

            if (!string.IsNullOrEmpty(filter.UserEmail))
                query = query.Where(l => l.UserEmail != null && l.UserEmail.Contains(filter.UserEmail));

            if (filter.ActionTypes != null && filter.ActionTypes.Any())
                query = query.Where(l => filter.ActionTypes.Contains(l.ActionType));

            if (filter.Categories != null && filter.Categories.Any())
                query = query.Where(l => filter.Categories.Contains(l.Category));

            if (filter.Severities != null && filter.Severities.Any())
                query = query.Where(l => filter.Severities.Contains(l.Severity));

            if (!string.IsNullOrEmpty(filter.EntityType))
                query = query.Where(l => l.EntityType == filter.EntityType);

            if (filter.EntityId.HasValue)
                query = query.Where(l => l.EntityId == filter.EntityId.Value);

            if (filter.Success.HasValue)
                query = query.Where(l => l.Success == filter.Success.Value);

            if (!string.IsNullOrEmpty(filter.IpAddress))
                query = query.Where(l => l.IpAddress != null && l.IpAddress.Contains(filter.IpAddress));

            if (!string.IsNullOrEmpty(filter.CorrelationId))
                query = query.Where(l => l.CorrelationId == filter.CorrelationId);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortDescending
                ? query.OrderByDescending(l => l.PerformedAt)
                : query.OrderBy(l => l.PerformedAt);

            // Apply pagination
            var logs = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(l => new HRMS.Application.DTOs.AuditLog.AuditLogDto
                {
                    Id = l.Id,
                    TenantId = l.TenantId,
                    TenantName = l.TenantName,
                    UserId = l.UserId,
                    UserEmail = l.UserEmail,
                    UserFullName = l.UserFullName,
                    UserRole = l.UserRole,
                    ActionType = l.ActionType,
                    ActionTypeName = l.ActionType.ToString(),
                    Category = l.Category,
                    CategoryName = l.Category.ToString(),
                    Severity = l.Severity,
                    SeverityName = l.Severity.ToString(),
                    EntityType = l.EntityType,
                    EntityId = l.EntityId,
                    Success = l.Success,
                    ChangedFields = l.ChangedFields,
                    IpAddress = l.IpAddress,
                    UserAgent = l.UserAgent,
                    PerformedAt = l.PerformedAt,
                    CorrelationId = l.CorrelationId
                })
                .ToListAsync();

            return new HRMS.Application.DTOs.AuditLog.PagedResult<HRMS.Application.DTOs.AuditLog.AuditLogDto>
            {
                Items = logs,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit logs with filter");
            throw;
        }
    }

    /// <summary>
    /// Get detailed audit log by ID
    /// </summary>
    public async Task<HRMS.Application.DTOs.AuditLog.AuditLogDetailDto?> GetAuditLogByIdAsync(Guid id)
    {
        try
        {
            var log = await _context.AuditLogs.FindAsync(id);
            if (log == null)
                return null;

            return new HRMS.Application.DTOs.AuditLog.AuditLogDetailDto
            {
                Id = log.Id,
                TenantId = log.TenantId,
                TenantName = log.TenantName,
                UserId = log.UserId,
                UserEmail = log.UserEmail,
                UserFullName = log.UserFullName,
                UserRole = log.UserRole,
                ActionType = log.ActionType,
                ActionTypeName = log.ActionType.ToString(),
                Category = log.Category,
                CategoryName = log.Category.ToString(),
                Severity = log.Severity,
                SeverityName = log.Severity.ToString(),
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Success = log.Success,
                ChangedFields = log.ChangedFields,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                PerformedAt = log.PerformedAt,
                CorrelationId = log.CorrelationId,
                OldValues = string.IsNullOrEmpty(log.OldValues)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, object?>>(log.OldValues),
                NewValues = string.IsNullOrEmpty(log.NewValues)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, object?>>(log.NewValues),
                AdditionalMetadata = string.IsNullOrEmpty(log.AdditionalMetadata)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, object?>>(log.AdditionalMetadata),
                Reason = log.Reason,
                RequestPath = log.RequestPath,
                HttpMethod = log.HttpMethod,
                ResponseCode = log.ResponseCode,
                DurationMs = log.DurationMs,
                ErrorMessage = log.ErrorMessage,
                SessionId = log.SessionId,
                DeviceInfo = log.DeviceInfo,
                Geolocation = log.Geolocation,
                ParentActionId = log.ParentActionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit log by ID: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Get statistics for audit log dashboard
    /// PERFORMANCE FIX: Uses database-side aggregations instead of loading all data into memory
    /// Prevents OutOfMemory exceptions on large datasets (1M+ records)
    /// </summary>
    public async Task<HRMS.Application.DTOs.AuditLog.AuditLogStatisticsDto> GetStatisticsAsync(
        DateTime? startDate,
        DateTime? endDate,
        Guid? tenantId)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekStart = now.AddDays(-7);
            var monthStart = now.AddDays(-30);

            var query = _context.AuditLogs
                .Where(l => l.PerformedAt >= start && l.PerformedAt <= end);

            if (tenantId.HasValue)
                query = query.Where(l => l.TenantId == tenantId.Value);

            // PERFORMANCE FIX: Execute all aggregations on database side
            var stats = new HRMS.Application.DTOs.AuditLog.AuditLogStatisticsDto
            {
                // Use database COUNT instead of loading into memory
                TotalActions = await query.CountAsync(),
                ActionsToday = await query.CountAsync(l => l.PerformedAt >= todayStart),
                ActionsThisWeek = await query.CountAsync(l => l.PerformedAt >= weekStart),
                ActionsThisMonth = await query.CountAsync(l => l.PerformedAt >= monthStart),
                FailedLogins = await query.CountAsync(l => l.ActionType == AuditActionType.LOGIN_FAILED),
                CriticalEvents = await query.CountAsync(l => l.Severity == AuditSeverity.CRITICAL || l.Severity == AuditSeverity.EMERGENCY),
                WarningEvents = await query.CountAsync(l => l.Severity == AuditSeverity.WARNING),

                // Database-side grouping for ActionsByCategory
                ActionsByCategory = await query
                    .GroupBy(l => l.Category)
                    .Select(g => new { Category = g.Key.ToString(), Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count),

                // Database-side grouping for ActionsBySeverity
                ActionsBySeverity = await query
                    .GroupBy(l => l.Severity)
                    .Select(g => new { Severity = g.Key.ToString(), Count = g.Count() })
                    .ToDictionaryAsync(x => x.Severity, x => x.Count),

                // Database-side grouping for MostActiveUsers (TOP 10)
                MostActiveUsers = await query
                    .Where(l => l.UserEmail != null)
                    .GroupBy(l => new { l.UserEmail, l.UserFullName })
                    .Select(g => new HRMS.Application.DTOs.AuditLog.TopUserActivityDto
                    {
                        UserEmail = g.Key.UserEmail!,
                        UserFullName = g.Key.UserFullName,
                        ActionCount = g.Count(),
                        LastActivity = g.Max(l => l.PerformedAt)
                    })
                    .OrderByDescending(u => u.ActionCount)
                    .Take(10)
                    .ToListAsync(),

                // Database-side grouping for MostModifiedEntities (TOP 10)
                MostModifiedEntities = await query
                    .Where(l => l.EntityType != null && l.EntityId != null)
                    .GroupBy(l => new { l.EntityType, l.EntityId })
                    .Select(g => new HRMS.Application.DTOs.AuditLog.TopEntityActivityDto
                    {
                        EntityType = g.Key.EntityType!,
                        EntityId = g.Key.EntityId,
                        ChangeCount = g.Count(),
                        LastModified = g.Max(l => l.PerformedAt)
                    })
                    .OrderByDescending(e => e.ChangeCount)
                    .Take(10)
                    .ToListAsync()
            };

            _logger.LogInformation(
                "Statistics generated for {RecordCount} audit logs in date range {Start} to {End}",
                stats.TotalActions, start, end);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit log statistics");
            throw;
        }
    }

    /// <summary>
    /// Export audit logs to CSV format
    /// SECURITY FIX: Added export limits to prevent DoS attacks
    /// Maximum 50,000 records per export
    /// </summary>
    public async Task<string> ExportToCsvAsync(HRMS.Application.DTOs.AuditLog.AuditLogFilterDto filter)
    {
        try
        {
            // SECURITY FIX: Enforce maximum export limit to prevent DoS
            const int MAX_EXPORT_LIMIT = 50000;

            // Override pagination with safe limits
            var originalPageSize = filter.PageSize;
            var originalPageNumber = filter.PageNumber;

            // Limit to maximum 50,000 records
            filter.PageSize = Math.Min(filter.PageSize, MAX_EXPORT_LIMIT);
            filter.PageNumber = 1; // Always start from page 1 for exports

            var result = await GetAuditLogsAsync(filter);

            // Warn if export was truncated
            if (result.TotalCount > MAX_EXPORT_LIMIT)
            {
                _logger.LogWarning(
                    "Export truncated: {TotalCount} total records, exported {ExportCount}. " +
                    "Use date filters to export in smaller batches.",
                    result.TotalCount, result.Items.Count);
            }

            // Restore original pagination values
            filter.PageSize = originalPageSize;
            filter.PageNumber = originalPageNumber;

            var csv = new StringBuilder();

            // CSV Header
            csv.AppendLine("Timestamp,Tenant,User,Action,Category,Severity,Entity Type,Entity ID,Success,IP Address,Changed Fields");

            // CSV Rows
            foreach (var log in result.Items)
            {
                csv.AppendLine($"\"{log.PerformedAt:yyyy-MM-dd HH:mm:ss}\"," +
                    $"\"{log.TenantName ?? "N/A"}\"," +
                    $"\"{log.UserEmail ?? "N/A"}\"," +
                    $"\"{log.ActionTypeName}\"," +
                    $"\"{log.CategoryName}\"," +
                    $"\"{log.SeverityName}\"," +
                    $"\"{log.EntityType ?? "N/A"}\"," +
                    $"\"{log.EntityId?.ToString() ?? "N/A"}\"," +
                    $"\"{log.Success}\"," +
                    $"\"{log.IpAddress ?? "N/A"}\"," +
                    $"\"{log.ChangedFields ?? "N/A"}\"");
            }

            return csv.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export audit logs to CSV");
            throw;
        }
    }

    /// <summary>
    /// Get user activity summary for a tenant
    /// </summary>
    public async Task<List<HRMS.Application.DTOs.AuditLog.UserActivityDto>> GetUserActivityAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var logs = await _context.AuditLogs
                .Where(l => l.TenantId == tenantId &&
                           l.PerformedAt >= startDate &&
                           l.PerformedAt <= endDate &&
                           l.UserEmail != null)
                .ToListAsync();

            return logs
                .GroupBy(l => new { l.UserEmail, l.UserFullName })
                .Select(g => new HRMS.Application.DTOs.AuditLog.UserActivityDto
                {
                    UserEmail = g.Key.UserEmail!,
                    UserFullName = g.Key.UserFullName,
                    TotalActions = g.Count(),
                    UniqueActionTypes = g.Select(l => l.ActionType).Distinct().Count(),
                    FirstActivity = g.Min(l => l.PerformedAt),
                    LastActivity = g.Max(l => l.PerformedAt),
                    ActionBreakdown = g.GroupBy(l => l.ActionType.ToString())
                        .ToDictionary(ag => ag.Key, ag => ag.Count())
                })
                .OrderByDescending(u => u.TotalActions)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user activity for tenant {TenantId}", tenantId);
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
            try
            {
                log.SessionId = httpContext.Session?.Id;
            }
            catch (InvalidOperationException)
            {
                // Session middleware not configured - use TraceIdentifier instead
                log.SessionId = httpContext.TraceIdentifier;
            }
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

    // ============================================
    // FORTUNE 500 ENHANCEMENT: SUPERADMIN ACTION LOGGING
    // ============================================

    /// <summary>
    /// Log SuperAdmin platform administration actions with enhanced accountability
    /// CRITICAL for SOC 2, GDPR, and audit compliance
    /// Provides complete audit trail for all SuperAdmin operations
    /// </summary>
    public async Task<AuditLog> LogSuperAdminActionAsync(
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
        Dictionary<string, object>? additionalContext = null)
    {
        try
        {
            // Determine severity based on action type
            var severity = DetermineSuperAdminActionSeverity(actionType, success);

            // Build additional metadata
            var metadata = new Dictionary<string, object>
            {
                { "superAdminId", superAdminId },
                { "superAdminEmail", superAdminEmail },
                { "actionTimestamp", DateTime.UtcNow },
                { "environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown" }
            };

            // Merge additional context if provided
            if (additionalContext != null)
            {
                foreach (var kvp in additionalContext)
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }

            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActionType = actionType,
                Category = AuditCategory.SYSTEM_ADMIN,
                Severity = severity,
                UserId = superAdminId,
                UserEmail = superAdminEmail,
                UserFullName = $"SuperAdmin: {superAdminEmail}",
                UserRole = "SuperAdmin",
                TenantId = targetTenantId,
                TenantName = targetTenantName,
                EntityType = "Tenant",
                EntityId = targetTenantId,
                OldValues = oldValues,
                NewValues = newValues,
                Reason = reason,
                ErrorMessage = errorMessage,
                Success = success,
                AdditionalMetadata = JsonSerializer.Serialize(metadata),
                PerformedAt = DateTime.UtcNow
            };

            // CRITICAL: Log with enhanced tracking
            var createdLog = await LogAsync(log);

            // Log to console for immediate visibility
            if (success)
            {
                _logger.LogInformation(
                    "🔐 SUPERADMIN ACTION: {ActionType} by {SuperAdminEmail} | Target: {TargetTenant} | Reason: {Reason}",
                    actionType, superAdminEmail, targetTenantName ?? "N/A", reason ?? "Not provided");
            }
            else
            {
                _logger.LogError(
                    "❌ SUPERADMIN ACTION FAILED: {ActionType} by {SuperAdminEmail} | Error: {ErrorMessage}",
                    actionType, superAdminEmail, errorMessage);
            }

            // FORTUNE 500 PATTERN: Monitor for suspicious SuperAdmin activity
            await MonitorSuperAdminActivity(superAdminId, actionType, createdLog.IpAddress);

            return createdLog;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "CRITICAL: Failed to log SuperAdmin action {ActionType} by {SuperAdminEmail}",
                actionType, superAdminEmail);
            throw;
        }
    }

    /// <summary>
    /// Determine severity for SuperAdmin actions based on risk level
    /// </summary>
    private AuditSeverity DetermineSuperAdminActionSeverity(AuditActionType actionType, bool success)
    {
        // Failed operations are always at least WARNING
        if (!success)
        {
            return AuditSeverity.WARNING;
        }

        // High-risk operations are CRITICAL
        var criticalActions = new[]
        {
            AuditActionType.TENANT_HARD_DELETED,
            AuditActionType.SUPERADMIN_CREATED,
            AuditActionType.SUPERADMIN_DELETED,
            AuditActionType.SUPERADMIN_PERMISSION_CHANGED,
            AuditActionType.TENANT_IMPERSONATION_STARTED,
            AuditActionType.SUPERADMIN_AUDIT_LOG_ACCESS
        };

        if (criticalActions.Contains(actionType))
        {
            return AuditSeverity.CRITICAL;
        }

        // Medium-risk operations are WARNING
        var warningActions = new[]
        {
            AuditActionType.TENANT_SUSPENDED,
            AuditActionType.TENANT_DELETED,
            AuditActionType.SUPERADMIN_UNLOCKED_ACCOUNT,
            AuditActionType.SECURITY_SETTING_CHANGED
        };

        if (warningActions.Contains(actionType))
        {
            return AuditSeverity.WARNING;
        }

        // Normal operations are INFO
        return AuditSeverity.INFO;
    }

    /// <summary>
    /// Monitor SuperAdmin activity for suspicious patterns
    /// Integrates with SecurityAlertingService for real-time threat detection
    /// </summary>
    private async Task MonitorSuperAdminActivity(Guid superAdminId, AuditActionType actionType, string? ipAddress)
    {
        if (_securityAlertingService == null)
            return;

        try
        {
            var now = DateTime.UtcNow;

            // Check for suspicious patterns
            var recentActions = await _context.AuditLogs
                .Where(l =>
                    l.UserId == superAdminId &&
                    l.Category == AuditCategory.SYSTEM_ADMIN &&
                    l.PerformedAt >= now.AddMinutes(-5))
                .CountAsync();

            // Pattern 1: Rapid actions (>10 actions in 5 minutes)
            if (recentActions > 10)
            {
                await _securityAlertingService.CreateAlertAsync(new SecurityAlert
                {
                    Id = Guid.NewGuid(),
                    AlertType = SecurityAlertType.RAPID_HIGH_RISK_ACTIONS,
                    Severity = AuditSeverity.CRITICAL,
                    Category = AuditCategory.SECURITY_EVENT,
                    Status = SecurityAlertStatus.NEW,
                    Title = "Suspicious SuperAdmin Activity: Rapid Actions",
                    Description = $"SuperAdmin {superAdminId} performed {recentActions} actions in 5 minutes. This may indicate automated/compromised account activity.",
                    RecommendedActions = "Review SuperAdmin activity logs immediately. Verify account is not compromised. Consider temporary suspension if suspicious.",
                    RiskScore = 85,
                    UserId = superAdminId,
                    IpAddress = ipAddress,
                    DetectedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                }, sendNotifications: true);
            }

            // Pattern 2: Off-hours access (2am-6am)
            var hour = now.Hour;
            if (hour >= 2 && hour < 6)
            {
                var isHighRisk = new[]
                {
                    AuditActionType.TENANT_HARD_DELETED,
                    AuditActionType.TENANT_SUSPENDED,
                    AuditActionType.SUPERADMIN_DELETED
                }.Contains(actionType);

                if (isHighRisk)
                {
                    await _securityAlertingService.CreateAlertAsync(new SecurityAlert
                    {
                        Id = Guid.NewGuid(),
                        AlertType = SecurityAlertType.AFTER_HOURS_ACCESS,
                        Severity = AuditSeverity.WARNING,
                        Category = AuditCategory.SECURITY_EVENT,
                        Status = SecurityAlertStatus.NEW,
                        Title = $"Off-Hours SuperAdmin Action: {actionType}",
                        Description = $"SuperAdmin {superAdminId} performed high-risk action {actionType} at {now:yyyy-MM-dd HH:mm:ss} UTC",
                        RecommendedActions = "Verify this action was authorized and performed by legitimate administrator. Off-hours high-risk operations should be investigated.",
                        RiskScore = 65,
                        UserId = superAdminId,
                        IpAddress = ipAddress,
                        DetectedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    }, sendNotifications: true);
                }
            }

            // Pattern 3: Mass tenant deletions
            if (actionType == AuditActionType.TENANT_HARD_DELETED)
            {
                var recentDeletions = await _context.AuditLogs
                    .Where(l =>
                        l.UserId == superAdminId &&
                        l.ActionType == AuditActionType.TENANT_HARD_DELETED &&
                        l.PerformedAt >= now.AddHours(-1))
                    .CountAsync();

                if (recentDeletions > 3)
                {
                    await _securityAlertingService.CreateAlertAsync(new SecurityAlert
                    {
                        Id = Guid.NewGuid(),
                        AlertType = SecurityAlertType.MASS_DATA_EXPORT,
                        Severity = AuditSeverity.EMERGENCY,
                        Category = AuditCategory.SECURITY_EVENT,
                        Status = SecurityAlertStatus.NEW,
                        Title = "EMERGENCY: Mass Tenant Deletion Detected",
                        Description = $"SuperAdmin {superAdminId} deleted {recentDeletions} tenants in 1 hour. This is highly unusual and may indicate a security incident.",
                        RecommendedActions = "IMMEDIATE ACTION REQUIRED: Suspend SuperAdmin account, review all deletions, contact security team, initiate incident response protocol.",
                        RiskScore = 95,
                        UserId = superAdminId,
                        IpAddress = ipAddress,
                        DetectedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    }, sendNotifications: true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor SuperAdmin activity for {SuperAdminId}", superAdminId);
            // Don't throw - monitoring failure shouldn't break audit logging
        }
    }

    // ============================================
    // DEVICE API KEY AUTHENTICATION LOGGING
    // ============================================

    public async Task<AuditLog> LogDeviceApiKeyCreatedAsync(
        Guid apiKeyId,
        Guid deviceId,
        string description,
        DateTime? expiresAt)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.RECORD_CREATED,
            Category = AuditCategory.SYSTEM_EVENT,
            Severity = AuditSeverity.INFO,
            EntityType = "DeviceApiKey",
            EntityId = apiKeyId,
            Success = true,
            AdditionalMetadata = JsonSerializer.Serialize(new
            {
                DeviceId = deviceId,
                Description = description,
                ExpiresAt = expiresAt
            }),
            PerformedAt = DateTime.UtcNow
        };

        return await LogAsync(log);
    }

    public async Task<AuditLog> LogDeviceApiKeyAuthenticationSuccessAsync(
        Guid apiKeyId,
        Guid deviceId,
        string ipAddress)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.LOGIN_SUCCESS,
            Category = AuditCategory.AUTHENTICATION,
            Severity = AuditSeverity.INFO,
            EntityType = "DeviceApiKey",
            EntityId = apiKeyId,
            Success = true,
            IpAddress = ipAddress,
            AdditionalMetadata = JsonSerializer.Serialize(new
            {
                DeviceId = deviceId,
                AuthenticationMethod = "API_KEY"
            }),
            PerformedAt = DateTime.UtcNow
        };

        return await LogAsync(log);
    }

    public async Task<AuditLog> LogDeviceApiKeyAuthenticationFailedAsync(
        Guid? apiKeyId,
        string ipAddress,
        string reason)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.LOGIN_FAILED,
            Category = AuditCategory.AUTHENTICATION,
            Severity = AuditSeverity.WARNING,
            EntityType = "DeviceApiKey",
            EntityId = apiKeyId,
            Success = false,
            IpAddress = ipAddress,
            ErrorMessage = reason,
            AdditionalMetadata = JsonSerializer.Serialize(new
            {
                AuthenticationMethod = "API_KEY",
                FailureReason = reason
            }),
            PerformedAt = DateTime.UtcNow
        };

        return await LogAsync(log);
    }

    public async Task<AuditLog> LogDeviceApiKeyRateLimitExceededAsync(
        Guid apiKeyId,
        string ipAddress,
        int limitPerMinute)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.ACCESS_DENIED,
            Category = AuditCategory.SECURITY_EVENT,
            Severity = AuditSeverity.WARNING,
            EntityType = "DeviceApiKey",
            EntityId = apiKeyId,
            Success = false,
            IpAddress = ipAddress,
            ErrorMessage = "Rate limit exceeded",
            AdditionalMetadata = JsonSerializer.Serialize(new
            {
                LimitPerMinute = limitPerMinute,
                Reason = "RATE_LIMIT_EXCEEDED"
            }),
            PerformedAt = DateTime.UtcNow
        };

        return await LogAsync(log);
    }

    public async Task<AuditLog> LogDeviceApiKeyRevokedAsync(
        Guid apiKeyId,
        Guid deviceId)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.RECORD_UPDATED,
            Category = AuditCategory.SYSTEM_EVENT,
            Severity = AuditSeverity.INFO,
            EntityType = "DeviceApiKey",
            EntityId = apiKeyId,
            Success = true,
            AdditionalMetadata = JsonSerializer.Serialize(new
            {
                DeviceId = deviceId,
                Action = "REVOKED"
            }),
            PerformedAt = DateTime.UtcNow
        };

        return await LogAsync(log);
    }

    public async Task<AuditLog> LogDeviceApiKeyRotatedAsync(
        Guid oldApiKeyId,
        Guid newApiKeyId,
        Guid deviceId)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActionType = AuditActionType.RECORD_UPDATED,
            Category = AuditCategory.SYSTEM_EVENT,
            Severity = AuditSeverity.INFO,
            EntityType = "DeviceApiKey",
            EntityId = oldApiKeyId,
            Success = true,
            AdditionalMetadata = JsonSerializer.Serialize(new
            {
                OldApiKeyId = oldApiKeyId,
                NewApiKeyId = newApiKeyId,
                DeviceId = deviceId,
                Action = "ROTATED"
            }),
            PerformedAt = DateTime.UtcNow
        };

        return await LogAsync(log);
    }
}
