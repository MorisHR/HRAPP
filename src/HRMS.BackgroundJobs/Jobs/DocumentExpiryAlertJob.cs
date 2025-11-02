using HRMS.Application.Interfaces;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.BackgroundJobs.Jobs;

/// <summary>
/// Background job to check and alert for expiring documents (Passport, Visa, Work Permit)
/// Runs daily at 9:00 AM
/// </summary>
public class DocumentExpiryAlertJob
{
    private readonly ILogger<DocumentExpiryAlertJob> _logger;
    private readonly IEmailService _emailService;
    private readonly MasterDbContext _masterContext;

    public DocumentExpiryAlertJob(
        ILogger<DocumentExpiryAlertJob> logger,
        IEmailService emailService,
        MasterDbContext masterContext)
    {
        _logger = logger;
        _emailService = emailService;
        _masterContext = masterContext;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Document Expiry Alert Job started at {Time}", DateTime.UtcNow);

        try
        {
            // Get all active tenants
            var tenants = await _masterContext.Tenants
                .Where(t => t.Status == Core.Enums.TenantStatus.Active && !t.IsDeleted)
                .ToListAsync();

            int totalAlertseSent = 0;

            foreach (var tenant in tenants)
            {
                _logger.LogInformation("Checking document expiry for tenant: {TenantName}", tenant.CompanyName);

                var alertsSent = await CheckTenantDocumentExpiryAsync(tenant.SchemaName);
                totalAlertseSent += alertsSent;
            }

            _logger.LogInformation("Document Expiry Alert Job completed. Total alerts sent: {Count}", totalAlertseSent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Document Expiry Alert Job");
            throw;
        }
    }

    private async Task<int> CheckTenantDocumentExpiryAsync(string schemaName)
    {
        // Create tenant-specific context
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        var connectionString = _masterContext.Database.GetConnectionString();
        optionsBuilder.UseNpgsql(connectionString);

        using var tenantContext = new TenantDbContext(optionsBuilder.Options, schemaName);

        int alertsSent = 0;
        var today = DateTime.UtcNow.Date;

        // Get all expatriate employees with documents
        var expatriateEmployees = await tenantContext.Employees
            .Where(e => !e.IsDeleted)
            .Where(e => e.Nationality != "Mauritian")
            .Where(e => e.PassportExpiryDate != null || e.VisaExpiryDate != null || e.WorkPermitExpiryDate != null)
            .ToListAsync();

        foreach (var employee in expatriateEmployees)
        {
            // Check Passport Expiry
            if (employee.PassportExpiryDate.HasValue)
            {
                var daysUntilExpiry = (employee.PassportExpiryDate.Value - today).Days;

                if (ShouldSendAlert(daysUntilExpiry))
                {
                    var urgency = GetUrgencyLevel(daysUntilExpiry);

                    try
                    {
                        await _emailService.SendDocumentExpiryAlertAsync(
                            $"{employee.FirstName} {employee.LastName}",
                            employee.Email,
                            "Passport",
                            employee.PassportExpiryDate.Value,
                            daysUntilExpiry,
                            urgency
                        );

                        _logger.LogInformation("Passport expiry alert sent to {Employee} - {Days} days remaining",
                            employee.Email, daysUntilExpiry);

                        alertsSent++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send passport expiry alert to {Employee}", employee.Email);
                    }
                }
            }

            // Check Visa Expiry
            if (employee.VisaExpiryDate.HasValue)
            {
                var daysUntilExpiry = (employee.VisaExpiryDate.Value - today).Days;

                if (ShouldSendAlert(daysUntilExpiry))
                {
                    var urgency = GetUrgencyLevel(daysUntilExpiry);

                    try
                    {
                        await _emailService.SendDocumentExpiryAlertAsync(
                            $"{employee.FirstName} {employee.LastName}",
                            employee.Email,
                            "Visa",
                            employee.VisaExpiryDate.Value,
                            daysUntilExpiry,
                            urgency
                        );

                        _logger.LogInformation("Visa expiry alert sent to {Employee} - {Days} days remaining",
                            employee.Email, daysUntilExpiry);

                        alertsSent++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send visa expiry alert to {Employee}", employee.Email);
                    }
                }
            }

            // Check Work Permit Expiry
            if (employee.WorkPermitExpiryDate.HasValue)
            {
                var daysUntilExpiry = (employee.WorkPermitExpiryDate.Value - today).Days;

                if (ShouldSendAlert(daysUntilExpiry))
                {
                    var urgency = GetUrgencyLevel(daysUntilExpiry);

                    try
                    {
                        await _emailService.SendDocumentExpiryAlertAsync(
                            $"{employee.FirstName} {employee.LastName}",
                            employee.Email,
                            "Work Permit",
                            employee.WorkPermitExpiryDate.Value,
                            daysUntilExpiry,
                            urgency
                        );

                        _logger.LogInformation("Work permit expiry alert sent to {Employee} - {Days} days remaining",
                            employee.Email, daysUntilExpiry);

                        alertsSent++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send work permit expiry alert to {Employee}", employee.Email);
                    }
                }
            }
        }

        return alertsSent;
    }

    private bool ShouldSendAlert(int daysRemaining)
    {
        // Send alerts at specific intervals
        return daysRemaining switch
        {
            90 => true,  // 90 days - First alert
            60 => true,  // 60 days - Warning
            30 => true,  // 30 days - Urgent
            15 => true,  // 15 days - Very urgent
            7 => true,   // 7 days - Critical
            <= 0 => true, // Expired - Daily
            _ => false
        };
    }

    private string GetUrgencyLevel(int daysRemaining)
    {
        return daysRemaining switch
        {
            <= 0 => "Critical - Expired",
            <= 7 => "Critical",
            <= 15 => "Urgent",
            <= 30 => "Warning",
            <= 60 => "Warning",
            _ => "Info"
        };
    }
}
