using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Legal hold service for litigation and regulatory compliance
/// FORTUNE 500 PATTERN: E-Discovery platforms, litigation hold management
/// </summary>
public interface ILegalHoldService
{
    /// <summary>
    /// Creates a new legal hold
    /// </summary>
    Task<LegalHold> CreateLegalHoldAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a legal hold
    /// </summary>
    Task<LegalHold> ReleaseLegalHoldAsync(
        Guid legalHoldId,
        Guid releasedBy,
        string? releaseNotes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active legal holds
    /// </summary>
    Task<List<LegalHold>> GetActiveLegalHoldsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies legal hold to audit logs
    /// </summary>
    Task<int> ApplyLegalHoldToAuditLogsAsync(
        Guid legalHoldId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets legal hold by ID
    /// </summary>
    Task<LegalHold?> GetLegalHoldByIdAsync(
        Guid legalHoldId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if audit log is under legal hold
    /// </summary>
    Task<bool> IsAuditLogUnderLegalHoldAsync(
        Guid auditLogId,
        CancellationToken cancellationToken = default);
}
