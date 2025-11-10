using HRMS.Core.DTOs.ComplianceReports;

namespace HRMS.Application.Interfaces;

/// <summary>
/// GDPR compliance service for EU data protection regulations
/// FORTUNE 500 PATTERN: GDPR Article 30 records, Data Subject Rights
/// </summary>
public interface IGDPRComplianceService
{
    /// <summary>
    /// Generates Right to Access report (GDPR Article 15)
    /// </summary>
    Task<RightToAccessReport> GenerateRightToAccessReportAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates Right to Be Forgotten report (GDPR Article 17)
    /// </summary>
    Task<RightToBeForgottenReport> GenerateRightToBeForgettenReportAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates data breach notification report (GDPR Article 33/34)
    /// </summary>
    Task<DataBreachNotificationReport> GenerateDataBreachNotificationReportAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates consent audit report (GDPR Article 7)
    /// </summary>
    Task<ConsentAuditReport> GenerateConsentAuditReportAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);
}
