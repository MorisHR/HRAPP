using System.Security.Claims;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using Npgsql;

namespace HRMS.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Production-grade EF Core interceptor for automatic audit logging of all database changes
/// Captures INSERT, UPDATE, DELETE operations with before/after values
/// Implements government/enterprise compliance requirements for complete audit trail
///
/// ARCHITECTURE NOTE: Breaks circular dependency by using IServiceProvider
/// - Does NOT inject IAuditLogService (which depends on DbContext)
/// - Creates fresh DbContext instance WITHOUT interceptors for saving audit logs
/// - Prevents infinite loops and circular dependency issues
///
/// PERFORMANCE OPTIMIZATIONS:
/// - Only serializes changed fields on UPDATE (not entire entity)
/// - Excludes volatile tables (tokens, sessions)
/// - Batches audit logs after main SaveChanges
/// - PERFORMANCE FIX: Uses queue-based background service instead of Task.Run
/// - Shallow serialization (depth = 1, no navigation properties)
/// </summary>
public class AuditLoggingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLoggingSaveChangesInterceptor> _logger;
    private readonly IConfiguration _configuration;

    // NOTE: AuditLogQueueService is optional to maintain backward compatibility
    // If not registered, falls back to direct database write
    private HRMS.Infrastructure.Services.AuditLogQueueService? _queueService;

    // Entities to exclude from audit logging (prevent infinite loops and reduce noise)
    private static readonly HashSet<string> ExcludedEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "AuditLog",           // Prevent infinite loop
        "RefreshToken",       // Too volatile, already logged via auth events
        "Session",            // Too volatile
        "MigrationHistory",   // System table
        "__EFMigrationsHistory" // System table
    };

    // Sensitive fields that trigger severity escalation to WARNING
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        // Compensation
        "Salary", "BaseSalary", "Compensation", "Bonus", "Commission",
        // Banking
        "BankAccountNumber", "BankName", "IBAN", "SwiftCode",
        // Personal IDs
        "NationalId", "SSN", "TaxId", "PassportNumber",
        // Medical & Emergency
        "MedicalInfo", "HealthCondition", "EmergencyContact", "EmergencyPhone",
        // Employment Status
        "TerminationDate", "TerminationReason", "EmploymentStatus", "IsTerminated", "IsOffboarded",
        // Security
        "PasswordHash", "MfaSecret", "BackupCodes",
        // Permissions
        "Role", "Permissions", "IsActive", "IsAdmin"
    };

    public AuditLoggingSaveChangesInterceptor(
        IServiceProvider serviceProvider,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditLoggingSaveChangesInterceptor> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _configuration = configuration;

        // PERFORMANCE FIX: Try to get queue service (optional for backward compatibility)
        try
        {
            _queueService = serviceProvider.GetService<HRMS.Infrastructure.Services.AuditLogQueueService>();
            if (_queueService != null)
            {
                _logger.LogInformation("Audit log queue service enabled for reliable delivery");
            }
        }
        catch
        {
            // Queue service not available - will use fallback
        }
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        // Capture all changed entities BEFORE save (because ChangeTracker is reset after)
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                       e.State == EntityState.Modified ||
                       e.State == EntityState.Deleted)
            .Where(e => !ExcludedEntities.Contains(e.Entity.GetType().Name))
            .ToList();

        // Process each change asynchronously after save completes
        if (entries.Any())
        {
            // Capture audit information now (before save)
            var auditLogs = entries.Select(entry => CaptureAuditInfo(entry)).ToList();

            // PERFORMANCE FIX: Use queue-based service if available, otherwise fallback to Task.Run
            if (_queueService != null)
            {
                // Queue audit logs for reliable background processing
                foreach (var auditLog in auditLogs)
                {
                    try
                    {
                        EnrichAuditLog(auditLog);
                        auditLog.Checksum = GenerateChecksum(auditLog);

                        await _queueService.QueueAuditLogAsync(auditLog);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to queue audit log for {EntityType} {EntityId}",
                            auditLog.EntityType, auditLog.EntityId);
                    }
                }
            }
            else
            {
                // Fallback: Using Task.Run (less reliable but maintains backward compatibility)
                _ = Task.Run(async () =>
                {
                    // Wait a moment for SaveChanges to complete
                    await Task.Delay(100, cancellationToken);

                    foreach (var auditLog in auditLogs)
                    {
                        try
                        {
                            await SaveAuditLogAsync(auditLog, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Failed to create audit log for {EntityType} {EntityId}, Action: {Action}",
                                auditLog.EntityType, auditLog.EntityId, auditLog.ActionType);
                        }
                    }
                }, cancellationToken);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Capture audit information from an entity entry
    /// Must be called BEFORE SaveChanges because ChangeTracker is reset after
    /// </summary>
    private AuditLog CaptureAuditInfo(EntityEntry entry)
    {
        try
        {
            var entityType = entry.Entity.GetType().Name;
            var actionType = DetermineActionType(entry.State, entityType);
            var entityId = GetEntityId(entry);
            var tenantId = GetTenantId(entry);

            // Build audit log
            return new AuditLog
            {
                Id = Guid.NewGuid(),
                ActionType = actionType,
                Category = AuditCategory.DATA_CHANGE,
                Severity = DetermineSeverity(entry),
                EntityType = entityType,
                EntityId = entityId,
                TenantId = tenantId,
                OldValues = GetOldValues(entry),
                NewValues = GetNewValues(entry),
                ChangedFields = GetChangedFields(entry),
                Success = true,
                PerformedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            // CRITICAL: Never throw exceptions from audit logging
            // Main business operations must not fail due to audit failures
            _logger.LogError(ex,
                "Failed to capture audit info for {EntityType}",
                entry.Entity.GetType().Name);

            // Return a minimal audit log so we don't lose all audit trail
            return new AuditLog
            {
                Id = Guid.NewGuid(),
                ActionType = AuditActionType.RECORD_UPDATED,
                Category = AuditCategory.DATA_CHANGE,
                Severity = AuditSeverity.WARNING,
                EntityType = entry.Entity.GetType().Name,
                ErrorMessage = "Failed to capture full audit details",
                Success = false,
                PerformedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Determine audit action type based on entity state and type
    /// </summary>
    private AuditActionType DetermineActionType(EntityState state, string entityType)
    {
        // Map specific entity types to specific actions
        var prefix = state switch
        {
            EntityState.Added => "CREATE",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",
            _ => "RECORD"
        };

        // Map entity type to specific action if exists in enum
        var specificAction = entityType.ToUpperInvariant() switch
        {
            "EMPLOYEE" => $"{prefix}_EMPLOYEE",
            "TENANT" => $"{prefix}_TENANT",
            "LEAVE" or "LEAVEREQUEST" => $"{prefix}_LEAVE",
            "LEAVEALLOCATION" => $"{prefix}_LEAVE_ALLOCATION",
            "DEPARTMENT" => $"{prefix}_DEPARTMENT",
            "PAYROLL" or "PAYROLLRECORD" => $"{prefix}_PAYROLL",
            "ATTENDANCE" or "ATTENDANCERECORD" => $"{prefix}_ATTENDANCE",
            "TIMESHEET" or "TIMESHEETENTRY" => $"{prefix}_TIMESHEET",
            "PERFORMANCEREVIEW" => $"{prefix}_PERFORMANCE_REVIEW",
            _ => null
        };

        // Try to parse specific action, fallback to generic
        if (specificAction != null && Enum.TryParse<AuditActionType>(specificAction, true, out var parsedAction))
        {
            return parsedAction;
        }

        // Fallback to generic actions
        return state switch
        {
            EntityState.Added => AuditActionType.RECORD_CREATED,
            EntityState.Modified => AuditActionType.RECORD_UPDATED,
            EntityState.Deleted => AuditActionType.RECORD_DELETED,
            _ => AuditActionType.RECORD_UPDATED
        };
    }

    /// <summary>
    /// Determine severity based on changed fields
    /// </summary>
    private AuditSeverity DetermineSeverity(EntityEntry entry)
    {
        // Deletions are always WARNING
        if (entry.State == EntityState.Deleted)
        {
            return AuditSeverity.WARNING;
        }

        // Check if any sensitive fields were modified
        if (entry.State == EntityState.Modified)
        {
            var modifiedProperties = entry.Properties
                .Where(p => p.IsModified)
                .Select(p => p.Metadata.Name)
                .ToList();

            if (modifiedProperties.Any(prop => SensitiveFields.Contains(prop)))
            {
                return AuditSeverity.WARNING;
            }
        }

        // Default to INFO
        return AuditSeverity.INFO;
    }

    /// <summary>
    /// Get primary key value of entity
    /// </summary>
    private Guid? GetEntityId(EntityEntry entry)
    {
        try
        {
            var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            if (keyProperty != null && keyProperty.CurrentValue != null)
            {
                if (keyProperty.CurrentValue is Guid guidValue)
                {
                    return guidValue;
                }

                // Try to parse if it's a string
                if (Guid.TryParse(keyProperty.CurrentValue.ToString(), out var parsedGuid))
                {
                    return parsedGuid;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get entity ID for {EntityType}", entry.Entity.GetType().Name);
        }

        return null;
    }

    /// <summary>
    /// Get tenant ID from entity if it has a TenantId property
    /// </summary>
    private Guid? GetTenantId(EntityEntry entry)
    {
        try
        {
            var tenantIdProperty = entry.Properties.FirstOrDefault(p =>
                p.Metadata.Name.Equals("TenantId", StringComparison.OrdinalIgnoreCase));

            if (tenantIdProperty != null && tenantIdProperty.CurrentValue != null)
            {
                if (tenantIdProperty.CurrentValue is Guid guidValue)
                {
                    return guidValue;
                }
            }

            // Fallback to HttpContext if entity doesn't have TenantId
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var tenantIdClaim = httpContext.User.FindFirst("tenant_id");
                if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
                {
                    return tenantId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get tenant ID for {EntityType}", entry.Entity.GetType().Name);
        }

        return null;
    }

    /// <summary>
    /// Get old values (before change) - only for Modified and Deleted states
    /// For Modified: only include changed fields (performance optimization)
    /// </summary>
    private string? GetOldValues(EntityEntry entry)
    {
        try
        {
            if (entry.State == EntityState.Added)
            {
                return null; // No old values for new records
            }

            var oldValues = new Dictionary<string, object?>();

            if (entry.State == EntityState.Modified)
            {
                // Only serialize changed fields (performance optimization)
                foreach (var property in entry.Properties.Where(p => p.IsModified))
                {
                    var propertyName = property.Metadata.Name;

                    // Skip navigation properties
                    if (property.Metadata.IsForeignKey())
                    {
                        continue;
                    }

                    oldValues[propertyName] = property.OriginalValue;
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                // For deletes, capture all current values
                foreach (var property in entry.Properties)
                {
                    var propertyName = property.Metadata.Name;

                    // Skip password hashes and other sensitive data
                    if (propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                        propertyName.Contains("Secret", StringComparison.OrdinalIgnoreCase))
                    {
                        oldValues[propertyName] = "***REDACTED***";
                        continue;
                    }

                    oldValues[propertyName] = property.CurrentValue;
                }
            }

            return oldValues.Any()
                ? JsonSerializer.Serialize(oldValues, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    MaxDepth = 1, // Prevent circular reference issues
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                })
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize old values for {EntityType}", entry.Entity.GetType().Name);
            return null;
        }
    }

    /// <summary>
    /// Get new values (after change) - only for Added and Modified states
    /// For Modified: only include changed fields (performance optimization)
    /// </summary>
    private string? GetNewValues(EntityEntry entry)
    {
        try
        {
            if (entry.State == EntityState.Deleted)
            {
                return null; // No new values for deleted records
            }

            var newValues = new Dictionary<string, object?>();

            if (entry.State == EntityState.Added)
            {
                // For new records, capture all values
                foreach (var property in entry.Properties)
                {
                    var propertyName = property.Metadata.Name;

                    // Skip password hashes and other sensitive data
                    if (propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                        propertyName.Contains("Secret", StringComparison.OrdinalIgnoreCase))
                    {
                        newValues[propertyName] = "***REDACTED***";
                        continue;
                    }

                    newValues[propertyName] = property.CurrentValue;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // Only serialize changed fields (performance optimization)
                foreach (var property in entry.Properties.Where(p => p.IsModified))
                {
                    var propertyName = property.Metadata.Name;
                    newValues[propertyName] = property.CurrentValue;
                }
            }

            return newValues.Any()
                ? JsonSerializer.Serialize(newValues, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    MaxDepth = 1, // Prevent circular reference issues
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                })
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize new values for {EntityType}", entry.Entity.GetType().Name);
            return null;
        }
    }

    /// <summary>
    /// Get list of changed field names (for Modified entities)
    /// </summary>
    private string? GetChangedFields(EntityEntry entry)
    {
        try
        {
            if (entry.State != EntityState.Modified)
            {
                return null;
            }

            var changedFields = entry.Properties
                .Where(p => p.IsModified)
                .Select(p => p.Metadata.Name)
                .ToList();

            return changedFields.Any() ? string.Join(", ", changedFields) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get changed fields for {EntityType}", entry.Entity.GetType().Name);
            return null;
        }
    }

    /// <summary>
    /// Save audit log to database using a fresh DbContext instance WITHOUT interceptors
    /// This prevents circular dependency and infinite loops
    /// PERFORMANCE FIX: Reuses pre-configured DbContextOptions from DI instead of building from scratch
    /// </summary>
    private async Task SaveAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        // PERFORMANCE FIX: Create a transient service scope and get pre-configured options
        // This avoids 5-15ms overhead of building DbContext from scratch
        using var scope = _serviceProvider.CreateScope();

        // PERFORMANCE FIX: Get pre-configured DbContextOptions from DI (already cached and optimized)
        // This reuses the EF Core model, service provider, and configuration
        var optionsAccessor = scope.ServiceProvider.GetService<DbContextOptions<MasterDbContext>>();

        // Fallback: Build options from scratch if not available in DI (backward compatibility)
        DbContextOptions<MasterDbContext> options;
        if (optionsAccessor != null)
        {
            // Use pre-configured options (fast path)
            options = optionsAccessor;
        }
        else
        {
            // Fallback: Build from configuration (slow path, but maintains compatibility)
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Cannot save audit log: Connection string 'DefaultConnection' not found");
                return;
            }

            var optionsBuilder = new DbContextOptionsBuilder<MasterDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            options = optionsBuilder.Options;
        }

        // Create fresh DbContext instance (without interceptors to prevent circular dependency)
        using var auditContext = new MasterDbContext(options);

        // Enrich audit log with HTTP context information
        EnrichAuditLog(auditLog);

        // Generate checksum for tamper detection
        auditLog.Checksum = GenerateChecksum(auditLog);

        // Ensure PerformedAt is set
        if (auditLog.PerformedAt == default)
        {
            auditLog.PerformedAt = DateTime.UtcNow;
        }

        // Add and save audit log
        auditContext.AuditLogs.Add(auditLog);
        await auditContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Audit log saved: {ActionType} on {EntityType} {EntityId}",
            auditLog.ActionType, auditLog.EntityType, auditLog.EntityId);
    }

    /// <summary>
    /// Enrich audit log with HTTP context information (IP, User Agent, User ID, etc.)
    /// </summary>
    private void EnrichAuditLog(AuditLog log)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        // Extract IP address
        if (string.IsNullOrEmpty(log.IpAddress))
        {
            log.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        // Extract User Agent
        if (string.IsNullOrEmpty(log.UserAgent))
        {
            log.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();
        }

        // Extract User ID and Email from claims if authenticated
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            if (log.UserId == null)
            {
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirst("sub")
                    ?? httpContext.User.FindFirst("user_id");

                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    log.UserId = userId;
                }
            }

            if (string.IsNullOrEmpty(log.UserEmail))
            {
                var emailClaim = httpContext.User.FindFirst(ClaimTypes.Email)
                    ?? httpContext.User.FindFirst("email");
                log.UserEmail = emailClaim?.Value;
            }
        }

        // Extract request path and method
        if (string.IsNullOrEmpty(log.RequestPath))
        {
            log.RequestPath = httpContext.Request.Path.ToString();
        }

        if (string.IsNullOrEmpty(log.HttpMethod))
        {
            log.HttpMethod = httpContext.Request.Method;
        }
    }

    /// <summary>
    /// Generate SHA256 checksum for tamper detection
    /// </summary>
    /// <summary>
    /// Truncate DateTime to microsecond precision to match PostgreSQL timestamp storage
    /// CRITICAL FIX: PostgreSQL stores timestamps with microsecond precision (6 decimal places),
    /// while .NET DateTime has 100-nanosecond tick precision (7 decimal places).
    /// This mismatch causes checksum validation failures after data is round-tripped through the database.
    /// </summary>
    /// <param name="dateTime">DateTime to truncate</param>
    /// <returns>DateTime truncated to microsecond precision</returns>
    private static DateTime TruncateToMicroseconds(DateTime dateTime)
    {
        // Convert to microseconds and back to remove sub-microsecond precision
        // 1 microsecond = 10 ticks (100 nanoseconds per tick)
        var microseconds = dateTime.Ticks / 10;
        return new DateTime(microseconds * 10, dateTime.Kind);
    }

    private string GenerateChecksum(AuditLog log)
    {
        // CRITICAL FIX: Truncate to microseconds to match PostgreSQL precision
        var performedAt = TruncateToMicroseconds(log.PerformedAt);
        var checksumInput = $"{log.ActionType}|{log.EntityType}|{log.EntityId}|{performedAt:O}|{log.UserId}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(checksumInput));
        return Convert.ToBase64String(hashBytes);
    }
}
