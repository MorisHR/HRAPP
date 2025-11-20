using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRMS.Infrastructure.Data;
using HRMS.Core.Enums;
using System.Text.Json;

namespace HRMS.Infrastructure.Compliance;

/// <summary>
/// FORTUNE 500 COMPLIANCE REPORTING SERVICE
///
/// Generates audit-ready compliance reports for major regulatory frameworks.
/// Follows patterns from Vanta, Drata, Secureframe, OneTrust GRC.
///
/// SUPPORTED FRAMEWORKS:
/// - SOX (Sarbanes-Oxley Act) - Financial reporting and access controls
/// - GDPR (General Data Protection Regulation) - Data privacy and consent
/// - ISO 27001 - Information security management
/// - SOC 2 Type II - Trust services criteria (Security, Availability, Confidentiality)
/// - PCI-DSS - Payment card data security
/// - HIPAA - Healthcare data privacy
/// - NIST 800-53 - Federal security controls
///
/// COMPLIANCE: SOC 2 CC5.2, ISO 27001 A.18.1.5, GDPR Article 30, SOX 404
/// </summary>
public interface IComplianceReportService
{
    Task<ComplianceReport> GenerateSoxReport(DateTime startDate, DateTime endDate, string? tenantId = null);
    Task<ComplianceReport> GenerateGdprReport(DateTime startDate, DateTime endDate, string? tenantId = null);
    Task<ComplianceReport> GenerateIso27001Report(DateTime startDate, DateTime endDate, string? tenantId = null);
    Task<ComplianceReport> GenerateSoc2Report(DateTime startDate, DateTime endDate, string? tenantId = null);
    Task<ComplianceReport> GeneratePciDssReport(DateTime startDate, DateTime endDate, string? tenantId = null);
    Task<ComplianceReport> GenerateHipaaReport(DateTime startDate, DateTime endDate, string? tenantId = null);
    Task<ComplianceReport> GenerateNist80053Report(DateTime startDate, DateTime endDate, string? tenantId = null);
    Task<byte[]> ExportReportToCsv(ComplianceReport report);
}

public class ComplianceReportService : IComplianceReportService
{
    private readonly MasterDbContext _context;
    private readonly ILogger<ComplianceReportService> _logger;

    public ComplianceReportService(
        MasterDbContext context,
        ILogger<ComplianceReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Generate SOX compliance report (Sarbanes-Oxley Act)
    /// Focuses on financial controls, access controls, and change management
    /// </summary>
    public async Task<ComplianceReport> GenerateSoxReport(DateTime startDate, DateTime endDate, string? tenantId = null)
    {
        _logger.LogInformation("Generating SOX compliance report from {StartDate} to {EndDate}", startDate, endDate);

        var report = new ComplianceReport
        {
            ReportId = Guid.NewGuid().ToString(),
            Framework = ComplianceFramework.SOX,
            Title = "SOX Compliance Report - Access Controls & Audit Trail",
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTimeOffset.UtcNow,
            TenantId = tenantId
        };

        // SOX 404: Access Controls - Administrative Users
        var accessControlSection = new ComplianceSection
        {
            SectionId = "SOX-404-AC",
            Title = "Access Controls (SOX 404)",
            Description = "User access controls and segregation of duties"
        };

        // Privileged access - AdminUsers
        var privilegedAdminCount = await _context.AdminUsers.CountAsync();

        accessControlSection.Findings.Add(new ComplianceFinding
        {
            FindingId = "SOX-AC-001",
            Title = "Privileged Admin Accounts",
            Severity = FindingSeverity.Info,
            Description = $"Total SuperAdmin accounts: {privilegedAdminCount}",
            Evidence = $"{{\"superAdminCount\": {privilegedAdminCount}}}",
            ControlReference = "SOX 404 - Access Control Matrix"
        });

        // Authentication events
        var authEventCounts = await _context.AuditLogs
            .Where(a => a.PerformedAt >= startDate && a.PerformedAt <= endDate)
            .Where(a => a.Category == AuditCategory.AUTHENTICATION)
            .Where(a => tenantId == null || a.TenantId.ToString() == tenantId)
            .GroupBy(a => a.ActionType)
            .Select(g => new { ActionType = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        accessControlSection.Findings.Add(new ComplianceFinding
        {
            FindingId = "SOX-AC-002",
            Title = "Authentication Activity",
            Severity = FindingSeverity.Info,
            Description = $"Authentication events during reporting period",
            Evidence = JsonSerializer.Serialize(authEventCounts),
            ControlReference = "SOX 404 - User Authentication"
        });

        report.Sections.Add(accessControlSection);

        // SOX 404: Change Management
        var changeManagementSection = new ComplianceSection
        {
            SectionId = "SOX-404-CM",
            Title = "Change Management (SOX 404)",
            Description = "System and configuration changes with approval trails"
        };

        var configChanges = await _context.AuditLogs
            .Where(a => a.PerformedAt >= startDate && a.PerformedAt <= endDate)
            .Where(a => a.Category == AuditCategory.DATA_CHANGE || a.Category == AuditCategory.SYSTEM_ADMIN)
            .Where(a => tenantId == null || a.TenantId.ToString() == tenantId)
            .OrderByDescending(a => a.PerformedAt)
            .Take(100)
            .Select(a => new
            {
                a.PerformedAt,
                a.UserId,
                ActionType = a.ActionType.ToString(),
                a.EntityType,
                a.EntityId,
                a.IpAddress
            })
            .ToListAsync();

        changeManagementSection.Findings.Add(new ComplianceFinding
        {
            FindingId = "SOX-CM-001",
            Title = "Configuration Changes",
            Severity = FindingSeverity.Info,
            Description = $"Total configuration changes: {configChanges.Count}",
            Evidence = JsonSerializer.Serialize(configChanges),
            ControlReference = "SOX 404 - Change Management"
        });

        report.Sections.Add(changeManagementSection);

        // Executive Summary
        report.ExecutiveSummary = $"SOX compliance report covering {(endDate - startDate).Days} days. " +
            $"Reviewed {privilegedAdminCount} SuperAdmin accounts, " +
            $"{authEventCounts.Sum(e => e.Count)} authentication events, and " +
            $"{configChanges.Count} configuration changes.";

        _logger.LogInformation("SOX report generated: {ReportId}", report.ReportId);
        return report;
    }

    /// <summary>
    /// Generate GDPR compliance report (General Data Protection Regulation)
    /// Article 30: Records of processing activities
    /// </summary>
    public async Task<ComplianceReport> GenerateGdprReport(DateTime startDate, DateTime endDate, string? tenantId = null)
    {
        _logger.LogInformation("Generating GDPR compliance report from {StartDate} to {EndDate}", startDate, endDate);

        var report = new ComplianceReport
        {
            ReportId = Guid.NewGuid().ToString(),
            Framework = ComplianceFramework.GDPR,
            Title = "GDPR Compliance Report - Data Processing Activities",
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTimeOffset.UtcNow,
            TenantId = tenantId
        };

        // Article 30: Data Processing Activities
        var processingSection = new ComplianceSection
        {
            SectionId = "GDPR-30",
            Title = "Records of Processing Activities (Article 30)",
            Description = "Documentation of data processing activities"
        };

        // Data access logs
        var dataAccessCounts = await _context.AuditLogs
            .Where(a => a.PerformedAt >= startDate && a.PerformedAt <= endDate)
            .Where(a => a.Category == AuditCategory.DATA_CHANGE || a.Category == AuditCategory.COMPLIANCE)
            .Where(a => tenantId == null || a.TenantId.ToString() == tenantId)
            .GroupBy(a => a.EntityType)
            .Select(g => new { EntityType = g.Key, Count = g.Count() })
            .ToListAsync();

        processingSection.Findings.Add(new ComplianceFinding
        {
            FindingId = "GDPR-30-001",
            Title = "Data Access Activities",
            Severity = FindingSeverity.Info,
            Description = $"Data access by entity type",
            Evidence = JsonSerializer.Serialize(dataAccessCounts),
            ControlReference = "GDPR Article 30 - Processing Records"
        });

        report.Sections.Add(processingSection);

        // Article 33: Security Incidents
        var breachSection = new ComplianceSection
        {
            SectionId = "GDPR-33",
            Title = "Security Incidents (Article 33)",
            Description = "Data breaches and security incidents requiring notification"
        };

        var securityIncidents = await _context.SecurityAlerts
            .Where(a => a.DetectedAt >= startDate && a.DetectedAt <= endDate)
            .Where(a => a.Severity == AuditSeverity.WARNING || a.Severity == AuditSeverity.CRITICAL)
            .Where(a => tenantId == null || a.TenantId.ToString() == tenantId)
            .Select(a => new
            {
                a.DetectedAt,
                a.AlertType,
                a.Severity,
                a.Description
            })
            .ToListAsync();

        breachSection.Findings.Add(new ComplianceFinding
        {
            FindingId = "GDPR-33-001",
            Title = "Security Incidents",
            Severity = securityIncidents.Any() ? FindingSeverity.High : FindingSeverity.Info,
            Description = $"Security incidents detected: {securityIncidents.Count}",
            Evidence = JsonSerializer.Serialize(securityIncidents),
            ControlReference = "GDPR Article 33 - Breach Notification",
            Recommendation = securityIncidents.Any() ? "Review incidents and determine if 72-hour notification to supervisory authority is required" : "No security incidents detected"
        });

        report.Sections.Add(breachSection);

        report.ExecutiveSummary = $"GDPR compliance report covering {(endDate - startDate).Days} days. " +
            $"Processed {dataAccessCounts.Sum(e => e.Count)} data access requests, " +
            $"{securityIncidents.Count} security incidents detected.";

        _logger.LogInformation("GDPR report generated: {ReportId}", report.ReportId);
        return report;
    }

    /// <summary>
    /// Generate ISO 27001 compliance report (Information Security Management)
    /// </summary>
    public async Task<ComplianceReport> GenerateIso27001Report(DateTime startDate, DateTime endDate, string? tenantId = null)
    {
        _logger.LogInformation("Generating ISO 27001 compliance report from {StartDate} to {EndDate}", startDate, endDate);

        var report = new ComplianceReport
        {
            ReportId = Guid.NewGuid().ToString(),
            Framework = ComplianceFramework.ISO27001,
            Title = "ISO 27001 Compliance Report - Information Security Controls",
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTimeOffset.UtcNow,
            TenantId = tenantId
        };

        // A.9: Access Control
        var accessControlSection = new ComplianceSection
        {
            SectionId = "ISO-A9",
            Title = "Access Control (A.9)",
            Description = "User access management and authentication controls"
        };

        var failedLoginCount = await _context.AuditLogs
            .Where(a => a.PerformedAt >= startDate && a.PerformedAt <= endDate)
            .Where(a => a.ActionType == AuditActionType.LOGIN_FAILED)
            .Where(a => tenantId == null || a.TenantId.ToString() == tenantId)
            .CountAsync();

        accessControlSection.Findings.Add(new ComplianceFinding
        {
            FindingId = "ISO-A9-001",
            Title = "Failed Authentication Attempts",
            Severity = failedLoginCount > 100 ? FindingSeverity.Medium : FindingSeverity.Info,
            Description = $"Failed login attempts: {failedLoginCount}",
            Evidence = $"{{\"failedLoginCount\": {failedLoginCount}}}",
            ControlReference = "ISO 27001 A.9.4.3 - Password Management System"
        });

        report.Sections.Add(accessControlSection);

        // A.12: Operations Security
        var operationsSection = new ComplianceSection
        {
            SectionId = "ISO-A12",
            Title = "Operations Security (A.12)",
            Description = "Logging, monitoring, and change management"
        };

        var totalAuditEvents = await _context.AuditLogs
            .Where(a => a.PerformedAt >= startDate && a.PerformedAt <= endDate)
            .Where(a => tenantId == null || a.TenantId.ToString() == tenantId)
            .CountAsync();

        operationsSection.Findings.Add(new ComplianceFinding
        {
            FindingId = "ISO-A12-001",
            Title = "Audit Logging Coverage",
            Severity = FindingSeverity.Info,
            Description = $"Total audit events logged: {totalAuditEvents}",
            Evidence = $"{{\"totalEvents\": {totalAuditEvents}}}",
            ControlReference = "ISO 27001 A.12.4.1 - Event Logging"
        });

        report.Sections.Add(operationsSection);

        report.ExecutiveSummary = $"ISO 27001 compliance report covering {(endDate - startDate).Days} days. " +
            $"Analyzed {failedLoginCount} failed login attempts, " +
            $"{totalAuditEvents} total audit events logged.";

        _logger.LogInformation("ISO 27001 report generated: {ReportId}", report.ReportId);
        return report;
    }

    /// <summary>
    /// Generate SOC 2 Type II compliance report (Trust Services Criteria)
    /// </summary>
    public async Task<ComplianceReport> GenerateSoc2Report(DateTime startDate, DateTime endDate, string? tenantId = null)
    {
        _logger.LogInformation("Generating SOC 2 Type II compliance report from {StartDate} to {EndDate}", startDate, endDate);

        var report = new ComplianceReport
        {
            ReportId = Guid.NewGuid().ToString(),
            Framework = ComplianceFramework.SOC2,
            Title = "SOC 2 Type II Compliance Report - Trust Services Criteria",
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTimeOffset.UtcNow,
            TenantId = tenantId
        };

        // CC6.1: Logical Access Controls
        var accessControlSection = new ComplianceSection
        {
            SectionId = "SOC2-CC6.1",
            Title = "Logical Access Controls (CC6.1)",
            Description = "User authentication, authorization, and session management"
        };

        var mfaSuccessCount = await _context.AuditLogs
            .Where(a => a.PerformedAt >= startDate && a.PerformedAt <= endDate)
            .Where(a => a.ActionType == AuditActionType.MFA_VERIFICATION_SUCCESS)
            .Where(a => tenantId == null || a.TenantId.ToString() == tenantId)
            .CountAsync();

        accessControlSection.Findings.Add(new ComplianceFinding
        {
            FindingId = "SOC2-CC6.1-001",
            Title = "Multi-Factor Authentication Usage",
            Severity = FindingSeverity.Info,
            Description = $"MFA verification events: {mfaSuccessCount}",
            Evidence = $"{{\"mfaVerifications\": {mfaSuccessCount}}}",
            ControlReference = "SOC 2 CC6.1 - Logical Access Controls"
        });

        report.Sections.Add(accessControlSection);

        // CC7.2: System Monitoring
        var monitoringSection = new ComplianceSection
        {
            SectionId = "SOC2-CC7.2",
            Title = "System Monitoring (CC7.2)",
            Description = "Security event monitoring and incident response"
        };

        var securityAlertCount = await _context.SecurityAlerts
            .Where(a => a.DetectedAt >= startDate && a.DetectedAt <= endDate)
            .Where(a => tenantId == null || a.TenantId.ToString() == tenantId)
            .CountAsync();

        monitoringSection.Findings.Add(new ComplianceFinding
        {
            FindingId = "SOC2-CC7.2-001",
            Title = "Security Alert Activity",
            Severity = FindingSeverity.Info,
            Description = $"Security alerts generated: {securityAlertCount}",
            Evidence = $"{{\"alertCount\": {securityAlertCount}}}",
            ControlReference = "SOC 2 CC7.2 - System Monitoring"
        });

        report.Sections.Add(monitoringSection);

        report.ExecutiveSummary = $"SOC 2 Type II compliance report covering {(endDate - startDate).Days} days. " +
            $"MFA verifications: {mfaSuccessCount}, " +
            $"{securityAlertCount} security alerts processed.";

        _logger.LogInformation("SOC 2 report generated: {ReportId}", report.ReportId);
        return report;
    }

    /// <summary>
    /// Generate PCI-DSS compliance report (Payment Card Industry Data Security Standard)
    /// </summary>
    public async Task<ComplianceReport> GeneratePciDssReport(DateTime startDate, DateTime endDate, string? tenantId = null)
    {
        _logger.LogInformation("Generating PCI-DSS compliance report from {StartDate} to {EndDate}", startDate, endDate);

        var report = new ComplianceReport
        {
            ReportId = Guid.NewGuid().ToString(),
            Framework = ComplianceFramework.PCIDSS,
            Title = "PCI-DSS Compliance Report - Payment Data Security",
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTimeOffset.UtcNow,
            TenantId = tenantId
        };

        // Requirement 10: Audit Logging
        var auditSection = new ComplianceSection
        {
            SectionId = "PCI-10",
            Title = "Audit Logging (Requirement 10)",
            Description = "Tracking and monitoring of all access to system resources"
        };

        var auditLogCount = await _context.AuditLogs
            .Where(a => a.PerformedAt >= startDate && a.PerformedAt <= endDate)
            .Where(a => tenantId == null || a.TenantId.ToString() == tenantId)
            .CountAsync();

        auditSection.Findings.Add(new ComplianceFinding
        {
            FindingId = "PCI-10-001",
            Title = "Audit Trail Completeness",
            Severity = FindingSeverity.Info,
            Description = $"Total audit events: {auditLogCount}",
            Evidence = $"{{\"auditEventCount\": {auditLogCount}}}",
            ControlReference = "PCI-DSS Requirement 10.2 - Audit Trails"
        });

        report.Sections.Add(auditSection);

        report.ExecutiveSummary = $"PCI-DSS compliance report covering {(endDate - startDate).Days} days. " +
            $"{auditLogCount} audit events logged.";

        _logger.LogInformation("PCI-DSS report generated: {ReportId}", report.ReportId);
        return report;
    }

    /// <summary>
    /// Generate HIPAA compliance report (Health Insurance Portability and Accountability Act)
    /// </summary>
    public async Task<ComplianceReport> GenerateHipaaReport(DateTime startDate, DateTime endDate, string? tenantId = null)
    {
        _logger.LogInformation("Generating HIPAA compliance report from {StartDate} to {EndDate}", startDate, endDate);

        var report = new ComplianceReport
        {
            ReportId = Guid.NewGuid().ToString(),
            Framework = ComplianceFramework.HIPAA,
            Title = "HIPAA Compliance Report - Protected Health Information",
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTimeOffset.UtcNow,
            TenantId = tenantId
        };

        // 164.308(a)(1): Security Management Process
        var securitySection = new ComplianceSection
        {
            SectionId = "HIPAA-164.308",
            Title = "Security Management (164.308)",
            Description = "Risk analysis and security incident procedures"
        };

        var dataAccessCount = await _context.AuditLogs
            .Where(a => a.PerformedAt >= startDate && a.PerformedAt <= endDate)
            .Where(a => a.Category == AuditCategory.DATA_CHANGE || a.Category == AuditCategory.COMPLIANCE)
            .Where(a => tenantId == null || a.TenantId.ToString() == tenantId)
            .CountAsync();

        securitySection.Findings.Add(new ComplianceFinding
        {
            FindingId = "HIPAA-308-001",
            Title = "Data Access Monitoring",
            Severity = FindingSeverity.Info,
            Description = $"Protected data access events: {dataAccessCount}",
            Evidence = $"{{\"dataAccessCount\": {dataAccessCount}}}",
            ControlReference = "HIPAA 164.308(a)(1)(ii)(D) - Information System Activity Review"
        });

        report.Sections.Add(securitySection);

        report.ExecutiveSummary = $"HIPAA compliance report covering {(endDate - startDate).Days} days. " +
            $"{dataAccessCount} data access events monitored.";

        _logger.LogInformation("HIPAA report generated: {ReportId}", report.ReportId);
        return report;
    }

    /// <summary>
    /// Generate NIST 800-53 compliance report (Federal Security Controls)
    /// </summary>
    public async Task<ComplianceReport> GenerateNist80053Report(DateTime startDate, DateTime endDate, string? tenantId = null)
    {
        _logger.LogInformation("Generating NIST 800-53 compliance report from {StartDate} to {EndDate}", startDate, endDate);

        var report = new ComplianceReport
        {
            ReportId = Guid.NewGuid().ToString(),
            Framework = ComplianceFramework.NIST80053,
            Title = "NIST 800-53 Compliance Report - Federal Security Controls",
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = DateTimeOffset.UtcNow,
            TenantId = tenantId
        };

        // AU-2: Audit Events
        var auditSection = new ComplianceSection
        {
            SectionId = "NIST-AU-2",
            Title = "Audit Events (AU-2)",
            Description = "Auditable events and audit record generation"
        };

        var auditEventStats = await _context.AuditLogs
            .Where(a => a.PerformedAt >= startDate && a.PerformedAt <= endDate)
            .Where(a => tenantId == null || a.TenantId.ToString() == tenantId)
            .GroupBy(a => 1)
            .Select(g => new
            {
                TotalEvents = g.Count(),
                UniqueUsers = g.Select(a => a.UserId).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        auditSection.Findings.Add(new ComplianceFinding
        {
            FindingId = "NIST-AU-2-001",
            Title = "Audit Event Statistics",
            Severity = FindingSeverity.Info,
            Description = $"Audit event coverage during reporting period",
            Evidence = JsonSerializer.Serialize(auditEventStats),
            ControlReference = "NIST 800-53 AU-2 - Audit Events"
        });

        report.Sections.Add(auditSection);

        report.ExecutiveSummary = $"NIST 800-53 compliance report covering {(endDate - startDate).Days} days. " +
            $"{auditEventStats?.TotalEvents ?? 0} audit events from {auditEventStats?.UniqueUsers ?? 0} unique users.";

        _logger.LogInformation("NIST 800-53 report generated: {ReportId}", report.ReportId);
        return report;
    }

    /// <summary>
    /// Export compliance report to CSV format
    /// </summary>
    public async Task<byte[]> ExportReportToCsv(ComplianceReport report)
    {
        _logger.LogInformation("Exporting report {ReportId} to CSV", report.ReportId);

        var csv = new System.Text.StringBuilder();

        // Header
        csv.AppendLine("Report ID,Framework,Title,Start Date,End Date,Generated At");
        csv.AppendLine($"\"{report.ReportId}\",\"{report.Framework}\",\"{report.Title}\",\"{report.StartDate:yyyy-MM-dd}\",\"{report.EndDate:yyyy-MM-dd}\",\"{report.GeneratedAt:yyyy-MM-dd HH:mm:ss}\"");
        csv.AppendLine();

        // Executive Summary
        csv.AppendLine("Executive Summary");
        csv.AppendLine($"\"{report.ExecutiveSummary}\"");
        csv.AppendLine();

        // Findings
        csv.AppendLine("Section ID,Section Title,Finding ID,Finding Title,Severity,Description,Control Reference,Recommendation");

        foreach (var section in report.Sections)
        {
            foreach (var finding in section.Findings)
            {
                csv.AppendLine($"\"{section.SectionId}\",\"{section.Title}\",\"{finding.FindingId}\",\"{finding.Title}\",\"{finding.Severity}\",\"{finding.Description}\",\"{finding.ControlReference}\",\"{finding.Recommendation}\"");
            }
        }

        return await Task.FromResult(System.Text.Encoding.UTF8.GetBytes(csv.ToString()));
    }
}

/// <summary>
/// Compliance report model
/// </summary>
public class ComplianceReport
{
    public string ReportId { get; set; } = string.Empty;
    public ComplianceFramework Framework { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public string? TenantId { get; set; }
    public string ExecutiveSummary { get; set; } = string.Empty;
    public List<ComplianceSection> Sections { get; set; } = new();
}

/// <summary>
/// Compliance framework enum
/// </summary>
public enum ComplianceFramework
{
    SOX,        // Sarbanes-Oxley Act
    GDPR,       // General Data Protection Regulation
    ISO27001,   // Information Security Management
    SOC2,       // Service Organization Control 2
    PCIDSS,     // Payment Card Industry Data Security Standard
    HIPAA,      // Health Insurance Portability and Accountability Act
    NIST80053   // NIST Security Controls
}

/// <summary>
/// Compliance section within a report
/// </summary>
public class ComplianceSection
{
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ComplianceFinding> Findings { get; set; } = new();
}

/// <summary>
/// Individual compliance finding
/// </summary>
public class ComplianceFinding
{
    public string FindingId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public FindingSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public string ControlReference { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
}

/// <summary>
/// Finding severity levels
/// </summary>
public enum FindingSeverity
{
    Info,       // Informational finding
    Low,        // Low risk finding
    Medium,     // Medium risk finding
    High,       // High risk finding
    Critical    // Critical risk finding requiring immediate action
}
