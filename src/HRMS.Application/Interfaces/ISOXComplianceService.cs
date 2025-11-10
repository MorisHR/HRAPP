using HRMS.Core.DTOs.ComplianceReports;

namespace HRMS.Application.Interfaces;

/// <summary>
/// SOX compliance service for Sarbanes-Oxley financial controls reporting
/// FORTUNE 500 PATTERN: SOX 404 compliance, IT General Controls (ITGC)
/// </summary>
public interface ISOXComplianceService
{
    /// <summary>
    /// Generates financial data access report (SOX Section 404)
    /// </summary>
    Task<SoxComplianceReport> GenerateFinancialDataAccessReportAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates user access changes report (segregation of duties)
    /// </summary>
    Task<UserAccessChangesSummary> GenerateUserAccessChangesReportAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates IT General Controls (ITGC) report
    /// </summary>
    Task<ITGCSummary> GenerateITGCReportAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates full SOX compliance report
    /// </summary>
    Task<SoxComplianceReport> GenerateFullSOXReportAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);
}
