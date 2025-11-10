using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;

namespace HRMS.Application.Interfaces;

/// <summary>
/// E-Discovery service for legal data export and chain of custody
/// FORTUNE 500 PATTERN: Relativity, Nuix, Everlaw
/// </summary>
public interface IEDiscoveryService
{
    /// <summary>
    /// Creates an e-discovery export package for a legal hold
    /// </summary>
    Task<byte[]> CreateEDiscoveryPackageAsync(
        Guid legalHoldId,
        EDiscoveryFormat format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit logs to EMLX format
    /// </summary>
    Task<byte[]> ExportToEmlxAsync(
        List<AuditLog> auditLogs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit logs to PDF with chain of custody
    /// </summary>
    Task<byte[]> ExportToPdfAsync(
        List<AuditLog> auditLogs,
        string caseNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates chain of custody report
    /// </summary>
    Task<byte[]> GenerateChainOfCustodyReportAsync(
        Guid legalHoldId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit logs to JSON format
    /// </summary>
    Task<byte[]> ExportToJsonAsync(
        List<AuditLog> auditLogs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit logs to CSV format
    /// </summary>
    Task<byte[]> ExportToCsvAsync(
        List<AuditLog> auditLogs,
        CancellationToken cancellationToken = default);
}
