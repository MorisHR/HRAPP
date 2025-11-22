using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Core.Exceptions;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Legal hold service for litigation and regulatory compliance
/// </summary>
public class LegalHoldService : ILegalHoldService
{
    private readonly MasterDbContext _context;
    private readonly ILogger<LegalHoldService> _logger;

    public LegalHoldService(MasterDbContext context, ILogger<LegalHoldService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LegalHold> CreateLegalHoldAsync(
        Guid? tenantId,
        string caseNumber,
        string description,
        DateTime startDate,
        DateTime? endDate,
        List<Guid>? userIds,
        List<string>? entityTypes,
        Guid requestedBy,
        string? legalRepresentative = null,
        string? courtOrder = null,
        CancellationToken cancellationToken = default)
    {
        var legalHold = new LegalHold
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseNumber = caseNumber,
            Description = description,
            StartDate = startDate,
            EndDate = endDate,
            Status = LegalHoldStatus.ACTIVE,
            UserIds = userIds != null ? JsonSerializer.Serialize(userIds) : null,
            EntityTypes = entityTypes != null ? JsonSerializer.Serialize(entityTypes) : null,
            RequestedBy = requestedBy,
            LegalRepresentative = legalRepresentative,
            CourtOrder = courtOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = requestedBy
        };

        _context.LegalHolds.Add(legalHold);
        await _context.SaveChangesAsync(cancellationToken);

        // Apply to existing audit logs
        await ApplyLegalHoldToAuditLogsAsync(legalHold.Id, cancellationToken);

        _logger.LogInformation("Legal hold {LegalHoldId} created for case {CaseNumber}", legalHold.Id, caseNumber);

        return legalHold;
    }

    public async Task<LegalHold> ReleaseLegalHoldAsync(
        Guid legalHoldId,
        Guid releasedBy,
        string? releaseNotes = null,
        CancellationToken cancellationToken = default)
    {
        var legalHold = await _context.LegalHolds.FindAsync(new object[] { legalHoldId }, cancellationToken);
        if (legalHold == null)
            throw new NotFoundException(
                ErrorCodes.SEC_ALERT_NOT_FOUND,
                "The legal hold you requested could not be found.",
                $"Legal hold {legalHoldId} not found in database",
                "Verify the legal hold ID or contact your security administrator.");

        legalHold.Status = LegalHoldStatus.RELEASED;
        legalHold.ReleasedBy = releasedBy;
        legalHold.ReleasedAt = DateTime.UtcNow;
        legalHold.ReleaseNotes = releaseNotes;
        legalHold.UpdatedAt = DateTime.UtcNow;
        legalHold.UpdatedBy = releasedBy;

        // Remove legal hold flag from audit logs
        await _context.AuditLogs
            .Where(a => a.LegalHoldId == legalHoldId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsUnderLegalHold, false)
                .SetProperty(a => a.LegalHoldId, (Guid?)null), cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Legal hold {LegalHoldId} released by {ReleasedBy}", legalHoldId, releasedBy);

        return legalHold;
    }

    public async Task<List<LegalHold>> GetActiveLegalHoldsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LegalHolds.Where(lh => lh.Status == LegalHoldStatus.ACTIVE);

        if (tenantId.HasValue)
            query = query.Where(lh => lh.TenantId == tenantId.Value);

        return await query.OrderByDescending(lh => lh.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<int> ApplyLegalHoldToAuditLogsAsync(
        Guid legalHoldId,
        CancellationToken cancellationToken = default)
    {
        var legalHold = await _context.LegalHolds.FindAsync(new object[] { legalHoldId }, cancellationToken);
        if (legalHold == null)
            throw new NotFoundException(
                ErrorCodes.SEC_ALERT_NOT_FOUND,
                "The legal hold you requested could not be found.",
                $"Legal hold {legalHoldId} not found in database",
                "Verify the legal hold ID or contact your security administrator.");

        var query = _context.AuditLogs
            .Where(a => a.PerformedAt >= legalHold.StartDate);

        if (legalHold.EndDate.HasValue)
            query = query.Where(a => a.PerformedAt <= legalHold.EndDate.Value);

        if (legalHold.TenantId.HasValue)
            query = query.Where(a => a.TenantId == legalHold.TenantId.Value);

        if (!string.IsNullOrEmpty(legalHold.UserIds))
        {
            var userIds = JsonSerializer.Deserialize<List<Guid>>(legalHold.UserIds);
            if (userIds != null && userIds.Any())
                query = query.Where(a => a.UserId != null && userIds.Contains(a.UserId.Value));
        }

        var affectedCount = await query.ExecuteUpdateAsync(s => s
            .SetProperty(a => a.IsUnderLegalHold, true)
            .SetProperty(a => a.LegalHoldId, legalHoldId), cancellationToken);

        legalHold.AffectedAuditLogCount = affectedCount;
        await _context.SaveChangesAsync(cancellationToken);

        return affectedCount;
    }

    public async Task<LegalHold?> GetLegalHoldByIdAsync(
        Guid legalHoldId,
        CancellationToken cancellationToken = default)
    {
        return await _context.LegalHolds.FindAsync(new object[] { legalHoldId }, cancellationToken);
    }

    public async Task<bool> IsAuditLogUnderLegalHoldAsync(
        Guid auditLogId,
        CancellationToken cancellationToken = default)
    {
        var auditLog = await _context.AuditLogs.FindAsync(new object[] { auditLogId }, cancellationToken);
        return auditLog?.IsUnderLegalHold ?? false;
    }

    public async Task<LegalHold> UpdateLegalHoldAsync(
        Guid legalHoldId,
        string? description,
        DateTime? endDate,
        string? legalRepresentative,
        string? legalRepresentativeEmail,
        string? lawFirm,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        var legalHold = await _context.LegalHolds.FindAsync(new object[] { legalHoldId }, cancellationToken);
        if (legalHold == null)
            throw new NotFoundException(
                ErrorCodes.SEC_ALERT_NOT_FOUND,
                "The legal hold you requested could not be found.",
                $"Legal hold {legalHoldId} not found in database",
                "Verify the legal hold ID or contact your security administrator.");

        // Only allow updates if legal hold is ACTIVE
        if (legalHold.Status != LegalHoldStatus.ACTIVE)
            throw new InvalidOperationException($"Cannot update legal hold with status {legalHold.Status}. Only ACTIVE legal holds can be updated.");

        // Update allowed fields
        if (!string.IsNullOrWhiteSpace(description))
            legalHold.Description = description;

        if (endDate.HasValue)
            legalHold.EndDate = endDate.Value;

        if (!string.IsNullOrWhiteSpace(legalRepresentative))
            legalHold.LegalRepresentative = legalRepresentative;

        if (!string.IsNullOrWhiteSpace(legalRepresentativeEmail))
            legalHold.LegalRepresentativeEmail = legalRepresentativeEmail;

        if (!string.IsNullOrWhiteSpace(lawFirm))
            legalHold.LawFirm = lawFirm;

        legalHold.UpdatedAt = DateTime.UtcNow;
        legalHold.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Legal hold {LegalHoldId} updated by {UpdatedBy}", legalHoldId, updatedBy);

        return legalHold;
    }

    public async Task<List<object>> GetAffectedAuditLogsAsync(
        Guid legalHoldId,
        CancellationToken cancellationToken = default)
    {
        var legalHold = await _context.LegalHolds.FindAsync(new object[] { legalHoldId }, cancellationToken);
        if (legalHold == null)
            throw new NotFoundException(
                ErrorCodes.SEC_ALERT_NOT_FOUND,
                "The legal hold you requested could not be found.",
                $"Legal hold {legalHoldId} not found in database",
                "Verify the legal hold ID or contact your security administrator.");

        var auditLogs = await _context.AuditLogs
            .Where(a => a.LegalHoldId == legalHoldId && a.IsUnderLegalHold)
            .OrderByDescending(a => a.PerformedAt)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.UserEmail,
                a.UserFullName,
                a.UserRole,
                a.ActionType,
                a.Category,
                a.Severity,
                a.EntityType,
                a.EntityId,
                Description = a.Reason ?? string.Empty,
                a.PerformedAt,
                a.IpAddress,
                a.UserAgent,
                a.Success,
                a.ErrorMessage,
                a.TenantId,
                a.IsUnderLegalHold,
                a.LegalHoldId
            })
            .Take(1000) // Limit to 1000 records for performance
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} audit logs for legal hold {LegalHoldId}", auditLogs.Count, legalHoldId);

        return auditLogs.Cast<object>().ToList();
    }
}
