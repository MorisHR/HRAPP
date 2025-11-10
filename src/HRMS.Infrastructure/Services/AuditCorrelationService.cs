using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Master;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Audit correlation service for advanced activity analysis
/// </summary>
public class AuditCorrelationService : IAuditCorrelationService
{
    private readonly MasterDbContext _context;
    private readonly ILogger<AuditCorrelationService> _logger;

    public AuditCorrelationService(MasterDbContext context, ILogger<AuditCorrelationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Dictionary<string, object>> CorrelateUserActivityAsync(
        Guid userId,
        TimeSpan timeRange,
        CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.UtcNow.Subtract(timeRange);
        var logs = await _context.AuditLogs
            .Where(a => a.UserId == userId && a.PerformedAt >= startDate)
            .OrderBy(a => a.PerformedAt)
            .ToListAsync(cancellationToken);

        return new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["totalActions"] = logs.Count,
            ["uniqueIpAddresses"] = logs.Select(l => l.IpAddress).Distinct().Count(),
            ["activityByHour"] = logs.GroupBy(l => l.PerformedAt.Hour).ToDictionary(g => g.Key, g => g.Count()),
            ["actionTypes"] = logs.GroupBy(l => l.ActionType).ToDictionary(g => g.Key.ToString(), g => g.Count())
        };
    }

    public async Task<List<ActivityTimelineEntry>> BuildActivityTimelineAsync(
        Guid userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.UserId == userId && a.PerformedAt >= startDate && a.PerformedAt <= endDate)
            .OrderBy(a => a.PerformedAt)
            .ToListAsync(cancellationToken);

        return logs.Select(l => new ActivityTimelineEntry
        {
            Timestamp = l.PerformedAt,
            ActionType = l.ActionType.ToString(),
            Description = $"{l.ActionType} on {l.EntityType}",
            IpAddress = l.IpAddress,
            Location = l.Geolocation,
            Success = l.Success,
            AuditLogId = l.Id
        }).ToList();
    }

    public async Task<List<CorrelatedPattern>> DetectPatternsAcrossUsersAsync(
        Guid? tenantId = null,
        int daysBack = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);
        var query = _context.AuditLogs.Where(a => a.PerformedAt >= cutoffDate);

        if (tenantId.HasValue)
            query = query.Where(a => a.TenantId == tenantId.Value);

        var logs = await query.ToListAsync(cancellationToken);

        // Simple pattern detection: users with similar IP addresses
        var ipGroups = logs.GroupBy(l => l.IpAddress).Where(g => g.Select(l => l.UserId).Distinct().Count() > 1);

        return ipGroups.Select(g => new CorrelatedPattern
        {
            PatternType = "Shared IP Address",
            Description = $"Multiple users accessing from same IP: {g.Key}",
            InvolvedUserIds = g.Select(l => l.UserId).Where(u => u.HasValue).Select(u => u!.Value).Distinct().ToList(),
            RelatedAuditLogIds = g.Select(l => l.Id).ToList(),
            Severity = 3,
            DetectedAt = DateTime.UtcNow
        }).ToList();
    }

    public async Task<List<AuditLog>> FindRelatedAuditLogsAsync(
        Guid auditLogId,
        int correlationDepth = 2,
        CancellationToken cancellationToken = default)
    {
        var mainLog = await _context.AuditLogs.FindAsync(new object[] { auditLogId }, cancellationToken);
        if (mainLog == null)
            return new List<AuditLog>();

        // Find logs from same session or correlation ID
        var relatedLogs = await _context.AuditLogs
            .Where(a => (a.SessionId == mainLog.SessionId || a.CorrelationId == mainLog.CorrelationId) && a.Id != auditLogId)
            .OrderBy(a => a.PerformedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return relatedLogs;
    }
}
