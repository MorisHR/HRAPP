using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HRMS.Infrastructure.Compliance;

namespace HRMS.API.Controllers;

/// <summary>
/// FORTUNE 500 COMPLIANCE REPORTING API
///
/// Provides REST endpoints for generating audit-ready compliance reports.
/// Follows patterns from Vanta, Drata, Secureframe, OneTrust GRC.
///
/// ENDPOINTS:
/// - GET /api/compliance/sox - SOX (Sarbanes-Oxley) compliance report
/// - GET /api/compliance/gdpr - GDPR (data privacy) compliance report
/// - GET /api/compliance/iso27001 - ISO 27001 (information security) report
/// - GET /api/compliance/soc2 - SOC 2 Type II (trust services) report
/// - GET /api/compliance/pci-dss - PCI-DSS (payment card security) report
/// - GET /api/compliance/hipaa - HIPAA (healthcare privacy) report
/// - GET /api/compliance/nist - NIST 800-53 (federal security) report
/// - GET /api/compliance/{reportId}/export/pdf - Export report as PDF
/// - GET /api/compliance/{reportId}/export/csv - Export report as CSV
/// - GET /api/compliance/{reportId}/export/excel - Export report as Excel
///
/// COMPLIANCE: SOC 2 CC5.2, ISO 27001 A.18.1.5, GDPR Article 30
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,TenantAdmin")]
public class ComplianceController : ControllerBase
{
    private readonly IComplianceReportService _complianceService;
    private readonly ILogger<ComplianceController> _logger;

    // In-memory cache for reports (production: use Redis or database)
    private static readonly Dictionary<string, ComplianceReport> _reportCache = new();
    private static readonly object _cacheLock = new();

    public ComplianceController(
        IComplianceReportService complianceService,
        ILogger<ComplianceController> logger)
    {
        _complianceService = complianceService;
        _logger = logger;
    }

    /// <summary>
    /// Generate SOX compliance report (Sarbanes-Oxley Act)
    /// </summary>
    /// <param name="startDate">Report start date (YYYY-MM-DD)</param>
    /// <param name="endDate">Report end date (YYYY-MM-DD)</param>
    /// <param name="tenantId">Optional tenant filter (SuperAdmin only)</param>
    [HttpGet("sox")]
    [ProducesResponseType(typeof(ComplianceReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSoxReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            if (start > end)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Generating SOX report from {StartDate} to {EndDate}", start, end);

            var report = await _complianceService.GenerateSoxReport(start, end, tenantId);

            // Cache report for export
            lock (_cacheLock)
            {
                _reportCache[report.ReportId] = report;
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SOX report");
            return StatusCode(500, new { error = "Failed to generate SOX report", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate GDPR compliance report (General Data Protection Regulation)
    /// </summary>
    /// <param name="startDate">Report start date (YYYY-MM-DD)</param>
    /// <param name="endDate">Report end date (YYYY-MM-DD)</param>
    /// <param name="tenantId">Optional tenant filter (SuperAdmin only)</param>
    [HttpGet("gdpr")]
    [ProducesResponseType(typeof(ComplianceReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGdprReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            if (start > end)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Generating GDPR report from {StartDate} to {EndDate}", start, end);

            var report = await _complianceService.GenerateGdprReport(start, end, tenantId);

            lock (_cacheLock)
            {
                _reportCache[report.ReportId] = report;
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating GDPR report");
            return StatusCode(500, new { error = "Failed to generate GDPR report", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate ISO 27001 compliance report (Information Security Management)
    /// </summary>
    /// <param name="startDate">Report start date (YYYY-MM-DD)</param>
    /// <param name="endDate">Report end date (YYYY-MM-DD)</param>
    /// <param name="tenantId">Optional tenant filter (SuperAdmin only)</param>
    [HttpGet("iso27001")]
    [ProducesResponseType(typeof(ComplianceReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetIso27001Report(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            if (start > end)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Generating ISO 27001 report from {StartDate} to {EndDate}", start, end);

            var report = await _complianceService.GenerateIso27001Report(start, end, tenantId);

            lock (_cacheLock)
            {
                _reportCache[report.ReportId] = report;
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating ISO 27001 report");
            return StatusCode(500, new { error = "Failed to generate ISO 27001 report", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate SOC 2 Type II compliance report (Trust Services Criteria)
    /// </summary>
    /// <param name="startDate">Report start date (YYYY-MM-DD)</param>
    /// <param name="endDate">Report end date (YYYY-MM-DD)</param>
    /// <param name="tenantId">Optional tenant filter (SuperAdmin only)</param>
    [HttpGet("soc2")]
    [ProducesResponseType(typeof(ComplianceReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSoc2Report(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            if (start > end)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Generating SOC 2 report from {StartDate} to {EndDate}", start, end);

            var report = await _complianceService.GenerateSoc2Report(start, end, tenantId);

            lock (_cacheLock)
            {
                _reportCache[report.ReportId] = report;
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SOC 2 report");
            return StatusCode(500, new { error = "Failed to generate SOC 2 report", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate PCI-DSS compliance report (Payment Card Industry Data Security Standard)
    /// </summary>
    /// <param name="startDate">Report start date (YYYY-MM-DD)</param>
    /// <param name="endDate">Report end date (YYYY-MM-DD)</param>
    /// <param name="tenantId">Optional tenant filter (SuperAdmin only)</param>
    [HttpGet("pci-dss")]
    [ProducesResponseType(typeof(ComplianceReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPciDssReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            if (start > end)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Generating PCI-DSS report from {StartDate} to {EndDate}", start, end);

            var report = await _complianceService.GeneratePciDssReport(start, end, tenantId);

            lock (_cacheLock)
            {
                _reportCache[report.ReportId] = report;
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PCI-DSS report");
            return StatusCode(500, new { error = "Failed to generate PCI-DSS report", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate HIPAA compliance report (Health Insurance Portability and Accountability Act)
    /// </summary>
    /// <param name="startDate">Report start date (YYYY-MM-DD)</param>
    /// <param name="endDate">Report end date (YYYY-MM-DD)</param>
    /// <param name="tenantId">Optional tenant filter (SuperAdmin only)</param>
    [HttpGet("hipaa")]
    [ProducesResponseType(typeof(ComplianceReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHipaaReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            if (start > end)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Generating HIPAA report from {StartDate} to {EndDate}", start, end);

            var report = await _complianceService.GenerateHipaaReport(start, end, tenantId);

            lock (_cacheLock)
            {
                _reportCache[report.ReportId] = report;
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating HIPAA report");
            return StatusCode(500, new { error = "Failed to generate HIPAA report", details = ex.Message });
        }
    }

    /// <summary>
    /// Generate NIST 800-53 compliance report (Federal Security Controls)
    /// </summary>
    /// <param name="startDate">Report start date (YYYY-MM-DD)</param>
    /// <param name="endDate">Report end date (YYYY-MM-DD)</param>
    /// <param name="tenantId">Optional tenant filter (SuperAdmin only)</param>
    [HttpGet("nist")]
    [ProducesResponseType(typeof(ComplianceReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNistReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? tenantId = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            if (start > end)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            _logger.LogInformation("Generating NIST 800-53 report from {StartDate} to {EndDate}", start, end);

            var report = await _complianceService.GenerateNist80053Report(start, end, tenantId);

            lock (_cacheLock)
            {
                _reportCache[report.ReportId] = report;
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating NIST 800-53 report");
            return StatusCode(500, new { error = "Failed to generate NIST 800-53 report", details = ex.Message });
        }
    }

    /// <summary>
    /// Export compliance report to PDF
    /// </summary>
    /// <param name="reportId">Report ID from generated report</param>
    [HttpGet("{reportId}/export/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public Task<IActionResult> ExportReportToPdf(string reportId)
    {
        try
        {
            ComplianceReport? report;
            lock (_cacheLock)
            {
                if (!_reportCache.TryGetValue(reportId, out report))
                {
                    return Task.FromResult<IActionResult>(NotFound(new { error = "Report not found. Please generate the report first." }));
                }
            }

            _logger.LogInformation("Exporting report {ReportId} to PDF", reportId);

            // PDF export not yet implemented - will return 501
            throw new NotImplementedException("PDF export feature is not yet implemented");
        }
        catch (NotImplementedException)
        {
            return Task.FromResult<IActionResult>(StatusCode(501, new { error = "PDF export feature is not yet implemented" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report to PDF");
            return Task.FromResult<IActionResult>(StatusCode(500, new { error = "Failed to export report to PDF", details = ex.Message }));
        }
    }

    /// <summary>
    /// Export compliance report to CSV
    /// </summary>
    /// <param name="reportId">Report ID from generated report</param>
    [HttpGet("{reportId}/export/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportReportToCsv(string reportId)
    {
        try
        {
            ComplianceReport? report;
            lock (_cacheLock)
            {
                if (!_reportCache.TryGetValue(reportId, out report))
                {
                    return NotFound(new { error = "Report not found. Please generate the report first." });
                }
            }

            _logger.LogInformation("Exporting report {ReportId} to CSV", reportId);

            var csvBytes = await _complianceService.ExportReportToCsv(report);

            return File(csvBytes, "text/csv", $"{report.Framework}_{report.StartDate:yyyyMMdd}_{report.EndDate:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report to CSV");
            return StatusCode(500, new { error = "Failed to export report to CSV", details = ex.Message });
        }
    }

    /// <summary>
    /// Export compliance report to Excel
    /// </summary>
    /// <param name="reportId">Report ID from generated report</param>
    [HttpGet("{reportId}/export/excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public Task<IActionResult> ExportReportToExcel(string reportId)
    {
        try
        {
            ComplianceReport? report;
            lock (_cacheLock)
            {
                if (!_reportCache.TryGetValue(reportId, out report))
                {
                    return Task.FromResult<IActionResult>(NotFound(new { error = "Report not found. Please generate the report first." }));
                }
            }

            _logger.LogInformation("Exporting report {ReportId} to Excel", reportId);

            // Excel export not yet implemented - will return 501
            throw new NotImplementedException("Excel export feature is not yet implemented");
        }
        catch (NotImplementedException)
        {
            return Task.FromResult<IActionResult>(StatusCode(501, new { error = "Excel export feature is not yet implemented" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report to Excel");
            return Task.FromResult<IActionResult>(StatusCode(500, new { error = "Failed to export report to Excel", details = ex.Message }));
        }
    }

    /// <summary>
    /// Get all available compliance frameworks
    /// </summary>
    [HttpGet("frameworks")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetFrameworks()
    {
        var frameworks = new[]
        {
            new
            {
                id = "sox",
                name = "SOX",
                fullName = "Sarbanes-Oxley Act",
                description = "Financial reporting and access controls",
                endpoint = "/api/compliance/sox"
            },
            new
            {
                id = "gdpr",
                name = "GDPR",
                fullName = "General Data Protection Regulation",
                description = "Data privacy and consent management",
                endpoint = "/api/compliance/gdpr"
            },
            new
            {
                id = "iso27001",
                name = "ISO 27001",
                fullName = "ISO/IEC 27001",
                description = "Information security management system",
                endpoint = "/api/compliance/iso27001"
            },
            new
            {
                id = "soc2",
                name = "SOC 2 Type II",
                fullName = "Service Organization Control 2",
                description = "Trust services criteria (Security, Availability, Confidentiality)",
                endpoint = "/api/compliance/soc2"
            },
            new
            {
                id = "pci-dss",
                name = "PCI-DSS",
                fullName = "Payment Card Industry Data Security Standard",
                description = "Payment card data security",
                endpoint = "/api/compliance/pci-dss"
            },
            new
            {
                id = "hipaa",
                name = "HIPAA",
                fullName = "Health Insurance Portability and Accountability Act",
                description = "Healthcare data privacy",
                endpoint = "/api/compliance/hipaa"
            },
            new
            {
                id = "nist",
                name = "NIST 800-53",
                fullName = "NIST Special Publication 800-53",
                description = "Federal information security controls",
                endpoint = "/api/compliance/nist"
            }
        };

        return Ok(new
        {
            frameworks,
            exportFormats = new[] { "pdf", "csv", "excel" },
            defaultDateRange = new
            {
                start = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd"),
                end = DateTime.UtcNow.ToString("yyyy-MM-dd")
            }
        });
    }
}
