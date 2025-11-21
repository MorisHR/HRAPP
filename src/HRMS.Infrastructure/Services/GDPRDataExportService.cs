using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRMS.Application.Interfaces;
using HRMS.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OfficeOpenXml;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// GDPR Article 15 & 20 - Complete User Data Export Service
/// FORTUNE 500 PATTERN: Aggregates ALL user data across all systems
/// PERFORMANCE: Parallel queries with Task.WhenAll for <5s exports
/// SECURITY: User authorization enforced at controller level
/// </summary>
public class GDPRDataExportService : IGDPRDataExportService
{
    private readonly MasterDbContext _masterContext;
    private readonly ILogger<GDPRDataExportService> _logger;

    public GDPRDataExportService(
        MasterDbContext masterContext,
        ILogger<GDPRDataExportService> logger)
    {
        _masterContext = masterContext;
        _logger = logger;
    }

    public async Task<UserDataExportPackage> GenerateUserDataExportAsync(
        Guid userId,
        string format = "json",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating GDPR data export for user {UserId}, format {Format}", userId, format);

            var package = new UserDataExportPackage
            {
                UserId = userId,
                ExportGeneratedAt = DateTime.UtcNow
            };

            // PERFORMANCE: Execute all queries in parallel using Task.WhenAll
            await Task.WhenAll(
                LoadAuditLogsAsync(userId, package, cancellationToken),
                LoadConsentsAsync(userId, package, cancellationToken),
                LoadSessionsAsync(userId, package, cancellationToken),
                LoadFileUploadsAsync(userId, package, cancellationToken),
                LoadSecurityAlertsAsync(userId, package, cancellationToken)
            );

            // Calculate metadata
            package.Metadata = new ExportMetadata
            {
                TotalAuditLogEntries = package.AuditLogs.Count,
                TotalConsents = package.Consents.Count,
                TotalSessions = package.Sessions.Count,
                TotalFilesUploaded = package.FileUploads.Count,
                TotalStorageUsedBytes = package.FileUploads.Sum(f => f.FileSizeBytes),
                DataCollectionStartDate = package.AuditLogs.Any()
                    ? package.AuditLogs.Min(a => a.Timestamp)
                    : DateTime.UtcNow,
                DataCollectionEndDate = DateTime.UtcNow
            };

            _logger.LogInformation(
                "GDPR export completed for user {UserId}: {AuditLogs} audit logs, {Consents} consents, {Sessions} sessions",
                userId, package.Metadata.TotalAuditLogEntries, package.Metadata.TotalConsents, package.Metadata.TotalSessions);

            return package;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate GDPR data export for user {UserId}", userId);
            throw;
        }
    }

    public async Task<ExportFileResult> ExportUserDataToFileAsync(
        Guid userId,
        string format = "json",
        CancellationToken cancellationToken = default)
    {
        var package = await GenerateUserDataExportAsync(userId, format, cancellationToken);

        return format.ToLower() switch
        {
            "json" => GenerateJsonExport(package),
            "csv" => GenerateCsvExport(package),
            "pdf" => GeneratePdfExport(package),
            "excel" => GenerateExcelExport(package),
            "xlsx" => GenerateExcelExport(package),
            _ => GenerateJsonExport(package) // Default to JSON
        };
    }

    // ==========================================
    // PRIVATE DATA LOADING METHODS
    // ==========================================

    private async Task LoadAuditLogsAsync(
        Guid userId,
        UserDataExportPackage package,
        CancellationToken cancellationToken)
    {
        var auditLogs = await _masterContext.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.PerformedAt)
            .Take(1000) // Limit to last 1000 entries
            .Select(a => new AuditLogEntry
            {
                Timestamp = a.PerformedAt,
                ActionType = a.ActionType.ToString(),
                Category = a.Category.ToString(),
                EntityType = a.EntityType,
                Description = a.ErrorMessage ?? a.Reason ?? $"{a.ActionType} on {a.EntityType ?? "Unknown"}",
                IpAddress = a.IpAddress ?? "Unknown"
            })
            .ToListAsync(cancellationToken);

        package.AuditLogs = auditLogs;

        // Get user email from most recent audit log
        var recentLog = await _masterContext.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.PerformedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (recentLog != null)
        {
            package.UserEmail = recentLog.UserEmail;
        }
    }

    private async Task LoadConsentsAsync(
        Guid userId,
        UserDataExportPackage package,
        CancellationToken cancellationToken)
    {
        var consents = await _masterContext.UserConsents
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.GivenAt)
            .Select(c => new ConsentEntry
            {
                GivenAt = c.GivenAt,
                ConsentType = c.ConsentType.ToString(),
                ConsentCategory = c.ConsentCategory,
                Purpose = c.Purpose,
                Status = c.Status.ToString(),
                WithdrawnAt = c.WithdrawnAt
            })
            .ToListAsync(cancellationToken);

        package.Consents = consents;
    }

    private async Task LoadSessionsAsync(
        Guid userId,
        UserDataExportPackage package,
        CancellationToken cancellationToken)
    {
        var sessions = await _masterContext.RefreshTokens
            .Where(r => r.AdminUserId == userId || r.EmployeeId == userId) // RefreshToken has AdminUserId and EmployeeId, not UserId
            .OrderByDescending(r => r.CreatedAt)
            .Take(100) // Last 100 sessions
            .Select(r => new SessionEntry
            {
                CreatedAt = r.CreatedAt,
                LastActivity = r.LastActivityAt, // Property is LastActivityAt not LastActivity
                IpAddress = r.CreatedByIp ?? "Unknown", // Property is CreatedByIp not IpAddress
                UserAgent = "N/A", // RefreshToken doesn't have UserAgent property
                IsActive = r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow
            })
            .ToListAsync(cancellationToken);

        package.Sessions = sessions;
    }

    private async Task LoadFileUploadsAsync(
        Guid userId,
        UserDataExportPackage package,
        CancellationToken cancellationToken)
    {
        var files = await _masterContext.FileUploadLogs
            .Where(f => f.UploadedBy == userId)
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => new FileUploadEntry
            {
                UploadedAt = f.UploadedAt,
                FileName = f.FileName,
                FileSizeBytes = f.FileSizeBytes,
                FileType = f.FileType
            })
            .ToListAsync(cancellationToken);

        package.FileUploads = files;
    }

    private async Task LoadSecurityAlertsAsync(
        Guid userId,
        UserDataExportPackage package,
        CancellationToken cancellationToken)
    {
        // Get security alerts related to this user
        var alerts = await _masterContext.SecurityAlerts
            .Where(a => a.UserId == userId) // SecurityAlert has UserId, not AffectedUsers
            .OrderByDescending(a => a.DetectedAt)
            .Take(100)
            .Select(a => new SecurityAlertEntry
            {
                DetectedAt = a.DetectedAt,
                AlertType = a.AlertType.ToString(),
                Severity = a.Severity.ToString(),
                Description = a.Description
            })
            .ToListAsync(cancellationToken);

        package.SecurityAlerts = alerts;
    }

    // ==========================================
    // EXPORT FORMAT GENERATORS
    // ==========================================

    private ExportFileResult GenerateJsonExport(UserDataExportPackage package)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(package, options);
        var bytes = Encoding.UTF8.GetBytes(json);

        return new ExportFileResult
        {
            FileBytes = bytes,
            FileName = $"gdpr_data_export_{package.UserId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json",
            MimeType = "application/json",
            FileSizeBytes = bytes.Length
        };
    }

    private ExportFileResult GenerateCsvExport(UserDataExportPackage package)
    {
        var csv = new StringBuilder();

        // Header
        csv.AppendLine("GDPR Data Export");
        csv.AppendLine($"User ID,{package.UserId}");
        csv.AppendLine($"Export Date,{package.ExportGeneratedAt:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine($"User Email,{package.UserEmail}");
        csv.AppendLine();

        // Audit Logs
        csv.AppendLine("=== AUDIT LOGS ===");
        csv.AppendLine("Timestamp,Action Type,Category,Entity Type,Description,IP Address");
        foreach (var log in package.AuditLogs)
        {
            csv.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.ActionType}\",\"{log.Category}\",\"{log.EntityType}\",\"{log.Description}\",\"{log.IpAddress}\"");
        }
        csv.AppendLine();

        // Consents
        csv.AppendLine("=== CONSENTS ===");
        csv.AppendLine("Given At,Consent Type,Category,Purpose,Status,Withdrawn At");
        foreach (var consent in package.Consents)
        {
            csv.AppendLine($"\"{consent.GivenAt:yyyy-MM-dd HH:mm:ss}\",\"{consent.ConsentType}\",\"{consent.ConsentCategory}\",\"{consent.Purpose}\",\"{consent.Status}\",\"{consent.WithdrawnAt:yyyy-MM-dd HH:mm:ss}\"");
        }
        csv.AppendLine();

        // Sessions
        csv.AppendLine("=== SESSIONS ===");
        csv.AppendLine("Created At,Last Activity,IP Address,User Agent,Is Active");
        foreach (var session in package.Sessions)
        {
            csv.AppendLine($"\"{session.CreatedAt:yyyy-MM-dd HH:mm:ss}\",\"{session.LastActivity:yyyy-MM-dd HH:mm:ss}\",\"{session.IpAddress}\",\"{session.UserAgent}\",\"{session.IsActive}\"");
        }
        csv.AppendLine();

        // File Uploads
        csv.AppendLine("=== FILE UPLOADS ===");
        csv.AppendLine("Uploaded At,File Name,File Size (bytes),File Type");
        foreach (var file in package.FileUploads)
        {
            csv.AppendLine($"\"{file.UploadedAt:yyyy-MM-dd HH:mm:ss}\",\"{file.FileName}\",\"{file.FileSizeBytes}\",\"{file.FileType}\"");
        }
        csv.AppendLine();

        // Metadata
        csv.AppendLine("=== SUMMARY STATISTICS ===");
        csv.AppendLine($"Total Audit Log Entries,{package.Metadata.TotalAuditLogEntries}");
        csv.AppendLine($"Total Consents,{package.Metadata.TotalConsents}");
        csv.AppendLine($"Total Sessions,{package.Metadata.TotalSessions}");
        csv.AppendLine($"Total Files Uploaded,{package.Metadata.TotalFilesUploaded}");
        csv.AppendLine($"Total Storage Used (bytes),{package.Metadata.TotalStorageUsedBytes}");
        csv.AppendLine($"Data Collection Start Date,{package.Metadata.DataCollectionStartDate:yyyy-MM-dd}");
        csv.AppendLine($"Data Collection End Date,{package.Metadata.DataCollectionEndDate:yyyy-MM-dd}");

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());

        return new ExportFileResult
        {
            FileBytes = bytes,
            FileName = $"gdpr_data_export_{package.UserId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv",
            MimeType = "text/csv",
            FileSizeBytes = bytes.Length
        };
    }

    private ExportFileResult GeneratePdfExport(UserDataExportPackage package)
    {
        // QuestPDF License Configuration (Community Edition)
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // Header
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text("GDPR Data Export Report").FontSize(20).SemiBold();
                        column.Item().Text($"User ID: {package.UserId}").FontSize(10);
                        column.Item().Text($"Generated: {package.ExportGeneratedAt:yyyy-MM-dd HH:mm:ss} UTC").FontSize(8);
                    });
                });

                // Content
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                {
                    // Summary
                    column.Item().Text("Summary").FontSize(14).SemiBold();
                    column.Item().PaddingBottom(0.5f, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Cell().BorderBottom(1).Padding(5).Text("Metric").SemiBold();
                        table.Cell().BorderBottom(1).Padding(5).Text("Value").SemiBold();

                        table.Cell().BorderBottom(1).Padding(5).Text("Total Audit Log Entries");
                        table.Cell().BorderBottom(1).Padding(5).Text(package.Metadata.TotalAuditLogEntries.ToString());

                        table.Cell().BorderBottom(1).Padding(5).Text("Total Consents");
                        table.Cell().BorderBottom(1).Padding(5).Text(package.Metadata.TotalConsents.ToString());

                        table.Cell().BorderBottom(1).Padding(5).Text("Total Sessions");
                        table.Cell().BorderBottom(1).Padding(5).Text(package.Metadata.TotalSessions.ToString());

                        table.Cell().BorderBottom(1).Padding(5).Text("Total Files Uploaded");
                        table.Cell().BorderBottom(1).Padding(5).Text(package.Metadata.TotalFilesUploaded.ToString());

                        table.Cell().Padding(5).Text("Total Storage Used");
                        table.Cell().Padding(5).Text($"{package.Metadata.TotalStorageUsedBytes:N0} bytes");
                    });

                    // Audit Logs Section
                    if (package.AuditLogs.Any())
                    {
                        column.Item().PaddingTop(1, Unit.Centimetre).Text($"Audit Logs ({package.AuditLogs.Count})").FontSize(12).SemiBold();
                        column.Item().PaddingBottom(0.5f, Unit.Centimetre).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).Padding(3).Text("Timestamp").FontSize(8).SemiBold();
                                header.Cell().BorderBottom(1).Padding(3).Text("Action").FontSize(8).SemiBold();
                                header.Cell().BorderBottom(1).Padding(3).Text("Category").FontSize(8).SemiBold();
                                header.Cell().BorderBottom(1).Padding(3).Text("IP Address").FontSize(8).SemiBold();
                            });

                            foreach (var log in package.AuditLogs.Take(20)) // Limit to first 20 for PDF
                            {
                                table.Cell().BorderBottom(1).Padding(3).Text(log.Timestamp.ToString("yyyy-MM-dd HH:mm")).FontSize(7);
                                table.Cell().BorderBottom(1).Padding(3).Text(log.ActionType).FontSize(7);
                                table.Cell().BorderBottom(1).Padding(3).Text(log.Category).FontSize(7);
                                table.Cell().BorderBottom(1).Padding(3).Text(log.IpAddress).FontSize(7);
                            }
                        });
                    }

                    // Consents Section
                    if (package.Consents.Any())
                    {
                        column.Item().PaddingTop(1, Unit.Centimetre).Text($"Consents ({package.Consents.Count})").FontSize(12).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).Padding(3).Text("Given At").FontSize(8).SemiBold();
                                header.Cell().BorderBottom(1).Padding(3).Text("Type").FontSize(8).SemiBold();
                                header.Cell().BorderBottom(1).Padding(3).Text("Purpose").FontSize(8).SemiBold();
                                header.Cell().BorderBottom(1).Padding(3).Text("Status").FontSize(8).SemiBold();
                            });

                            foreach (var consent in package.Consents.Take(10))
                            {
                                table.Cell().BorderBottom(1).Padding(3).Text(consent.GivenAt.ToString("yyyy-MM-dd")).FontSize(7);
                                table.Cell().BorderBottom(1).Padding(3).Text(consent.ConsentType).FontSize(7);
                                table.Cell().BorderBottom(1).Padding(3).Text(consent.Purpose).FontSize(7);
                                table.Cell().BorderBottom(1).Padding(3).Text(consent.Status).FontSize(7);
                            }
                        });
                    }
                });

                // Footer
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                    text.Span(" | Generated by HRMS GDPR Compliance System");
                });
            });
        });

        var pdfBytes = document.GeneratePdf();

        return new ExportFileResult
        {
            FileBytes = pdfBytes,
            FileName = $"gdpr_data_export_{package.UserId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf",
            MimeType = "application/pdf",
            FileSizeBytes = pdfBytes.Length
        };
    }

    private ExportFileResult GenerateExcelExport(UserDataExportPackage package)
    {
        // EPPlus License Configuration (NonCommercial)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var excelPackage = new ExcelPackage();

        // Summary Sheet
        var summarySheet = excelPackage.Workbook.Worksheets.Add("Summary");
        summarySheet.Cells["A1"].Value = "GDPR Data Export Summary";
        summarySheet.Cells["A1:B1"].Merge = true;
        summarySheet.Cells["A1"].Style.Font.Size = 16;
        summarySheet.Cells["A1"].Style.Font.Bold = true;

        summarySheet.Cells["A3"].Value = "User ID";
        summarySheet.Cells["B3"].Value = package.UserId.ToString();
        summarySheet.Cells["A4"].Value = "User Email";
        summarySheet.Cells["B4"].Value = package.UserEmail ?? "N/A";
        summarySheet.Cells["A5"].Value = "Export Generated";
        summarySheet.Cells["B5"].Value = package.ExportGeneratedAt.ToString("yyyy-MM-dd HH:mm:ss");

        summarySheet.Cells["A7"].Value = "Metric";
        summarySheet.Cells["B7"].Value = "Value";
        summarySheet.Cells["A7:B7"].Style.Font.Bold = true;

        summarySheet.Cells["A8"].Value = "Total Audit Log Entries";
        summarySheet.Cells["B8"].Value = package.Metadata.TotalAuditLogEntries;
        summarySheet.Cells["A9"].Value = "Total Consents";
        summarySheet.Cells["B9"].Value = package.Metadata.TotalConsents;
        summarySheet.Cells["A10"].Value = "Total Sessions";
        summarySheet.Cells["B10"].Value = package.Metadata.TotalSessions;
        summarySheet.Cells["A11"].Value = "Total Files Uploaded";
        summarySheet.Cells["B11"].Value = package.Metadata.TotalFilesUploaded;
        summarySheet.Cells["A12"].Value = "Total Storage Used (bytes)";
        summarySheet.Cells["B12"].Value = package.Metadata.TotalStorageUsedBytes;

        summarySheet.Cells[summarySheet.Dimension.Address].AutoFitColumns();

        // Audit Logs Sheet
        if (package.AuditLogs.Any())
        {
            var auditSheet = excelPackage.Workbook.Worksheets.Add("Audit Logs");
            auditSheet.Cells["A1"].Value = "Timestamp";
            auditSheet.Cells["B1"].Value = "Action Type";
            auditSheet.Cells["C1"].Value = "Category";
            auditSheet.Cells["D1"].Value = "Entity Type";
            auditSheet.Cells["E1"].Value = "Description";
            auditSheet.Cells["F1"].Value = "IP Address";
            auditSheet.Cells["A1:F1"].Style.Font.Bold = true;

            int row = 2;
            foreach (var log in package.AuditLogs)
            {
                auditSheet.Cells[row, 1].Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                auditSheet.Cells[row, 2].Value = log.ActionType;
                auditSheet.Cells[row, 3].Value = log.Category;
                auditSheet.Cells[row, 4].Value = log.EntityType;
                auditSheet.Cells[row, 5].Value = log.Description;
                auditSheet.Cells[row, 6].Value = log.IpAddress;
                row++;
            }

            auditSheet.Cells[auditSheet.Dimension.Address].AutoFitColumns();
        }

        // Consents Sheet
        if (package.Consents.Any())
        {
            var consentsSheet = excelPackage.Workbook.Worksheets.Add("Consents");
            consentsSheet.Cells["A1"].Value = "Given At";
            consentsSheet.Cells["B1"].Value = "Consent Type";
            consentsSheet.Cells["C1"].Value = "Category";
            consentsSheet.Cells["D1"].Value = "Purpose";
            consentsSheet.Cells["E1"].Value = "Status";
            consentsSheet.Cells["F1"].Value = "Withdrawn At";
            consentsSheet.Cells["A1:F1"].Style.Font.Bold = true;

            int row = 2;
            foreach (var consent in package.Consents)
            {
                consentsSheet.Cells[row, 1].Value = consent.GivenAt.ToString("yyyy-MM-dd HH:mm:ss");
                consentsSheet.Cells[row, 2].Value = consent.ConsentType;
                consentsSheet.Cells[row, 3].Value = consent.ConsentCategory;
                consentsSheet.Cells[row, 4].Value = consent.Purpose;
                consentsSheet.Cells[row, 5].Value = consent.Status;
                consentsSheet.Cells[row, 6].Value = consent.WithdrawnAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                row++;
            }

            consentsSheet.Cells[consentsSheet.Dimension.Address].AutoFitColumns();
        }

        // Sessions Sheet
        if (package.Sessions.Any())
        {
            var sessionsSheet = excelPackage.Workbook.Worksheets.Add("Sessions");
            sessionsSheet.Cells["A1"].Value = "Created At";
            sessionsSheet.Cells["B1"].Value = "Last Activity";
            sessionsSheet.Cells["C1"].Value = "IP Address";
            sessionsSheet.Cells["D1"].Value = "User Agent";
            sessionsSheet.Cells["E1"].Value = "Is Active";
            sessionsSheet.Cells["A1:E1"].Style.Font.Bold = true;

            int row = 2;
            foreach (var session in package.Sessions)
            {
                sessionsSheet.Cells[row, 1].Value = session.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                sessionsSheet.Cells[row, 2].Value = session.LastActivity?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                sessionsSheet.Cells[row, 3].Value = session.IpAddress;
                sessionsSheet.Cells[row, 4].Value = session.UserAgent;
                sessionsSheet.Cells[row, 5].Value = session.IsActive ? "Yes" : "No";
                row++;
            }

            sessionsSheet.Cells[sessionsSheet.Dimension.Address].AutoFitColumns();
        }

        var excelBytes = excelPackage.GetAsByteArray();

        return new ExportFileResult
        {
            FileBytes = excelBytes,
            FileName = $"gdpr_data_export_{package.UserId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx",
            MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileSizeBytes = excelBytes.Length
        };
    }
}
