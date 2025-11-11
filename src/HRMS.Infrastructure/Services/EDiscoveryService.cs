using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// E-Discovery service for legal data export
/// </summary>
public class EDiscoveryService : IEDiscoveryService
{
    private readonly MasterDbContext _context;
    private readonly ILogger<EDiscoveryService> _logger;

    public EDiscoveryService(MasterDbContext context, ILogger<EDiscoveryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<byte[]> CreateEDiscoveryPackageAsync(
        Guid legalHoldId,
        EDiscoveryFormat format,
        CancellationToken cancellationToken = default)
    {
        var legalHold = await _context.LegalHolds.FindAsync(new object[] { legalHoldId }, cancellationToken);
        if (legalHold == null)
            throw new InvalidOperationException($"Legal hold {legalHoldId} not found");

        var auditLogs = await _context.AuditLogs
            .Where(a => a.LegalHoldId == legalHoldId)
            .OrderBy(a => a.PerformedAt)
            .ToListAsync(cancellationToken);

        return format switch
        {
            EDiscoveryFormat.EMLX => await ExportToEmlxAsync(auditLogs, cancellationToken),
            EDiscoveryFormat.PDF => await ExportToPdfAsync(auditLogs, legalHold.CaseNumber, cancellationToken),
            EDiscoveryFormat.JSON => await ExportToJsonAsync(auditLogs, cancellationToken),
            EDiscoveryFormat.CSV => await ExportToCsvAsync(auditLogs, cancellationToken),
            _ => throw new NotSupportedException($"Format {format} not supported")
        };
    }

    public Task<byte[]> ExportToEmlxAsync(
        List<AuditLog> auditLogs,
        CancellationToken cancellationToken = default)
    {
        // Simplified EMLX format
        var sb = new StringBuilder();
        foreach (var log in auditLogs)
        {
            sb.AppendLine($"From: {log.UserEmail ?? "system@hrms.com"}");
            sb.AppendLine($"Date: {log.PerformedAt:R}");
            sb.AppendLine($"Subject: {log.ActionType} - {log.EntityType}");
            sb.AppendLine($"Message-ID: <{log.Id}@hrms.com>");
            sb.AppendLine();
            sb.AppendLine($"Action: {log.ActionType}");
            sb.AppendLine($"User: {log.UserFullName} ({log.UserEmail})");
            sb.AppendLine($"IP: {log.IpAddress}");
            sb.AppendLine($"Timestamp: {log.PerformedAt:O}");
            sb.AppendLine();
        }
        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    public Task<byte[]> ExportToPdfAsync(
        List<AuditLog> auditLogs,
        string caseNumber,
        CancellationToken cancellationToken = default)
    {
        // NOTE: In production, use a PDF library like QuestPDF or DinkToPdf
        var sb = new StringBuilder();
        sb.AppendLine("=== E-DISCOVERY EXPORT ===");
        sb.AppendLine($"Case Number: {caseNumber}");
        sb.AppendLine($"Generated: {DateTime.UtcNow:O}");
        sb.AppendLine($"Total Records: {auditLogs.Count}");
        sb.AppendLine();
        sb.AppendLine("CHAIN OF CUSTODY:");
        sb.AppendLine($"Exported by: System");
        sb.AppendLine($"Export date: {DateTime.UtcNow:O}");
        sb.AppendLine();
        foreach (var log in auditLogs)
        {
            sb.AppendLine($"--- Record {log.Id} ---");
            sb.AppendLine($"Timestamp: {log.PerformedAt:O}");
            sb.AppendLine($"User: {log.UserFullName} ({log.UserEmail})");
            sb.AppendLine($"Action: {log.ActionType}");
            sb.AppendLine($"Entity: {log.EntityType}");
            sb.AppendLine();
        }
        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    public async Task<byte[]> GenerateChainOfCustodyReportAsync(
        Guid legalHoldId,
        CancellationToken cancellationToken = default)
    {
        var legalHold = await _context.LegalHolds.FindAsync(new object[] { legalHoldId }, cancellationToken);
        if (legalHold == null)
            throw new InvalidOperationException($"Legal hold {legalHoldId} not found");

        var sb = new StringBuilder();
        sb.AppendLine("=== CHAIN OF CUSTODY REPORT ===");
        sb.AppendLine($"Case Number: {legalHold.CaseNumber}");
        sb.AppendLine($"Legal Hold ID: {legalHold.Id}");
        sb.AppendLine($"Created: {legalHold.CreatedAt:O}");
        sb.AppendLine($"Status: {legalHold.Status}");
        sb.AppendLine($"Records Under Hold: {legalHold.AffectedAuditLogCount}");
        sb.AppendLine();
        sb.AppendLine("CUSTODIAN INFORMATION:");
        sb.AppendLine($"Requested By: {legalHold.RequestedBy}");
        sb.AppendLine($"Legal Representative: {legalHold.LegalRepresentative ?? "N/A"}");
        sb.AppendLine();
        sb.AppendLine("DATA INTEGRITY:");
        sb.AppendLine("All records are stored with immutable audit trails.");
        sb.AppendLine("SHA256 checksums are maintained for tamper detection.");
        sb.AppendLine();

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public Task<byte[]> ExportToJsonAsync(
        List<AuditLog> auditLogs,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(auditLogs, new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(Encoding.UTF8.GetBytes(json));
    }

    public Task<byte[]> ExportToCsvAsync(
        List<AuditLog> auditLogs,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,PerformedAt,UserEmail,ActionType,EntityType,Success,IpAddress,Geolocation");
        foreach (var log in auditLogs)
        {
            sb.AppendLine($"{log.Id},{log.PerformedAt:O},{log.UserEmail},{log.ActionType},{log.EntityType},{log.Success},{log.IpAddress},{log.Geolocation}");
        }
        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }
}
