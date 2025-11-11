using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRMS.Application.Interfaces;
using HRMS.Core.DTOs.ComplianceReports;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// GDPR compliance service for EU data protection regulations
/// </summary>
public class GDPRComplianceService : IGDPRComplianceService
{
    private readonly MasterDbContext _context;
    private readonly ILogger<GDPRComplianceService> _logger;

    public GDPRComplianceService(MasterDbContext context, ILogger<GDPRComplianceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RightToAccessReport> GenerateRightToAccessReportAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var auditLogs = await _context.AuditLogs
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

        var report = new RightToAccessReport
        {
            UserId = userId,
            UserEmail = auditLogs.FirstOrDefault()?.UserEmail,
            ReportGeneratedAt = DateTime.UtcNow,
            TotalAuditLogEntries = auditLogs.Count,
            PersonalData = new List<PersonalDataItem>
            {
                new PersonalDataItem { DataType = "Email", DataValue = auditLogs.FirstOrDefault()?.UserEmail ?? "", Source = "AuditLog", CollectedAt = DateTime.UtcNow },
                new PersonalDataItem { DataType = "IP Address", DataValue = auditLogs.FirstOrDefault()?.IpAddress ?? "", Source = "AuditLog", CollectedAt = DateTime.UtcNow }
            },
            ProcessingActivities = auditLogs.Select(a => new DataProcessingActivity
            {
                ActivityType = a.ActionType.ToString(),
                Purpose = "System audit and compliance",
                ProcessedAt = a.PerformedAt,
                LegalBasis = "Legitimate interest"
            }).Take(100).ToList()
        };

        return report;
    }

    public async Task<RightToBeForgottenReport> GenerateRightToBeForgettenReportAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var hasLegalHold = await _context.AuditLogs
            .AnyAsync(a => a.UserId == userId && a.IsUnderLegalHold, cancellationToken);

        return new RightToBeForgottenReport
        {
            UserId = userId,
            ReportGeneratedAt = DateTime.UtcNow,
            EntitiesAffected = new List<string> { "AuditLog", "Employee", "LeaveRequest" },
            TotalRecordsToDelete = 0,
            HasLegalHoldConflict = hasLegalHold,
            LegalHoldReason = hasLegalHold ? "User data is under active legal hold" : null
        };
    }

    public Task<DataBreachNotificationReport> GenerateDataBreachNotificationReportAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new DataBreachNotificationReport
        {
            IncidentId = incidentId,
            BreachDetectedAt = DateTime.UtcNow,
            ReportGeneratedAt = DateTime.UtcNow,
            BreachType = "Unauthorized Access",
            BreachDescription = "Potential data breach detected",
            AffectedUsersCount = 0,
            RequiresRegulatoryNotification = false,
            RequiresUserNotification = false
        });
    }

    public Task<ConsentAuditReport> GenerateConsentAuditReportAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ConsentAuditReport
        {
            ReportGeneratedAt = DateTime.UtcNow,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            TenantId = tenantId,
            TotalConsents = 0,
            ActiveConsents = 0,
            WithdrawnConsents = 0
        });
    }
}
