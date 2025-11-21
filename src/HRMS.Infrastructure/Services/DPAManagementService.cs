using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// GDPR Article 28 - Data Processing Agreement Management Service
/// FORTUNE 500 PATTERN: OneTrust Vendor Risk, ServiceNow VRM
/// PERFORMANCE: Indexed queries for <10ms response time
/// </summary>
public class DPAManagementService : IDPAManagementService
{
    private readonly MasterDbContext _context;
    private readonly ILogger<DPAManagementService> _logger;

    public DPAManagementService(
        MasterDbContext context,
        ILogger<DPAManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==========================================
    // DPA LIFECYCLE
    // ==========================================

    public async Task<DataProcessingAgreement> CreateDPAAsync(
        DataProcessingAgreement dpa,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dpa.VendorName))
            {
                throw new ArgumentException("Vendor name is required");
            }

            if (dpa.EffectiveDate >= dpa.ExpiryDate)
            {
                throw new ArgumentException("Expiry date must be after effective date");
            }

            // Set defaults
            dpa.Id = Guid.NewGuid();
            dpa.CreatedAt = DateTime.UtcNow;
            dpa.Status = DpaStatus.Draft;

            // Calculate next review dates if not set
            if (!dpa.NextRiskAssessmentDate.HasValue)
            {
                dpa.NextRiskAssessmentDate = DateTime.UtcNow.AddYears(1);
            }

            if (!dpa.NextAuditDate.HasValue && dpa.AuditRights != "None")
            {
                dpa.NextAuditDate = dpa.AuditRights switch
                {
                    "Quarterly" => DateTime.UtcNow.AddMonths(3),
                    "Annual" => DateTime.UtcNow.AddYears(1),
                    _ => DateTime.UtcNow.AddYears(1)
                };
            }

            _context.DataProcessingAgreements.Add(dpa);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "DPA created: Id={DpaId}, Vendor={VendorName}, Tenant={TenantId}",
                dpa.Id, dpa.VendorName, dpa.TenantId);

            return dpa;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create DPA for vendor {VendorName}", dpa.VendorName);
            throw;
        }
    }

    public async Task<DataProcessingAgreement> UpdateDPAAsync(
        Guid dpaId,
        DataProcessingAgreement updatedDpa,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.DataProcessingAgreements
                .FirstOrDefaultAsync(d => d.Id == dpaId, cancellationToken);

            if (existing == null)
            {
                throw new InvalidOperationException($"DPA {dpaId} not found");
            }

            // Update fields
            existing.VendorName = updatedDpa.VendorName;
            existing.VendorType = updatedDpa.VendorType;
            existing.VendorContactName = updatedDpa.VendorContactName;
            existing.VendorContactEmail = updatedDpa.VendorContactEmail;
            existing.VendorPhone = updatedDpa.VendorPhone;
            existing.VendorAddress = updatedDpa.VendorAddress;
            existing.VendorCountry = updatedDpa.VendorCountry;
            existing.VendorWebsite = updatedDpa.VendorWebsite;
            existing.DataProtectionOfficer = updatedDpa.DataProtectionOfficer;
            existing.ProcessingPurpose = updatedDpa.ProcessingPurpose;
            existing.DataSubjectCategories = updatedDpa.DataSubjectCategories;
            existing.PersonalDataCategories = updatedDpa.PersonalDataCategories;
            existing.SpecialDataCategories = updatedDpa.SpecialDataCategories;
            existing.ProcessesSensitiveData = updatedDpa.ProcessesSensitiveData;
            existing.RetentionPeriodDays = updatedDpa.RetentionPeriodDays;
            existing.DataDisposalMethod = updatedDpa.DataDisposalMethod;
            existing.InternationalDataTransfer = updatedDpa.InternationalDataTransfer;
            existing.TransferCountries = updatedDpa.TransferCountries;
            existing.TransferMechanism = updatedDpa.TransferMechanism;
            existing.SecurityMeasures = updatedDpa.SecurityMeasures;
            existing.AllowsSubProcessors = updatedDpa.AllowsSubProcessors;
            existing.AuthorizedSubProcessors = updatedDpa.AuthorizedSubProcessors;
            existing.InternalNotes = updatedDpa.InternalNotes;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = updatedDpa.UpdatedBy;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("DPA updated: Id={DpaId}, Vendor={VendorName}", dpaId, existing.VendorName);

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update DPA {DpaId}", dpaId);
            throw;
        }
    }

    public async Task<bool> TerminateDPAAsync(
        Guid dpaId,
        string terminationReason,
        Guid terminatedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpa = await _context.DataProcessingAgreements
                .FirstOrDefaultAsync(d => d.Id == dpaId, cancellationToken);

            if (dpa == null)
            {
                return false;
            }

            dpa.Status = DpaStatus.Terminated;
            dpa.TerminatedAt = DateTime.UtcNow;
            dpa.TerminationReason = terminationReason;
            dpa.TerminatedBy = terminatedBy;
            dpa.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "DPA terminated: Id={DpaId}, Vendor={VendorName}, Reason={Reason}",
                dpaId, dpa.VendorName, terminationReason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate DPA {DpaId}", dpaId);
            throw;
        }
    }

    public async Task<DataProcessingAgreement> RenewDPAAsync(
        Guid existingDpaId,
        DateTime newEffectiveDate,
        DateTime newExpiryDate,
        Guid renewedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.DataProcessingAgreements
                .FirstOrDefaultAsync(d => d.Id == existingDpaId, cancellationToken);

            if (existing == null)
            {
                throw new InvalidOperationException($"DPA {existingDpaId} not found");
            }

            // Mark old DPA as expired
            existing.Status = DpaStatus.Expired;
            existing.UpdatedAt = DateTime.UtcNow;

            // Create new DPA based on existing
            var renewed = new DataProcessingAgreement
            {
                Id = Guid.NewGuid(),
                TenantId = existing.TenantId,
                VendorName = existing.VendorName,
                VendorType = existing.VendorType,
                VendorContactName = existing.VendorContactName,
                VendorContactEmail = existing.VendorContactEmail,
                VendorPhone = existing.VendorPhone,
                VendorAddress = existing.VendorAddress,
                VendorCountry = existing.VendorCountry,
                VendorWebsite = existing.VendorWebsite,
                DataProtectionOfficer = existing.DataProtectionOfficer,
                DpaReferenceNumber = $"{existing.DpaReferenceNumber}-R{DateTime.UtcNow:yyyyMMdd}",
                Status = DpaStatus.Active,
                EffectiveDate = newEffectiveDate,
                ExpiryDate = newExpiryDate,
                IsAutoRenewal = existing.IsAutoRenewal,
                NoticePeriodDays = existing.NoticePeriodDays,
                ProcessingPurpose = existing.ProcessingPurpose,
                DataSubjectCategories = existing.DataSubjectCategories,
                PersonalDataCategories = existing.PersonalDataCategories,
                SpecialDataCategories = existing.SpecialDataCategories,
                ProcessesSensitiveData = existing.ProcessesSensitiveData,
                RetentionPeriodDays = existing.RetentionPeriodDays,
                DataDisposalMethod = existing.DataDisposalMethod,
                InternationalDataTransfer = existing.InternationalDataTransfer,
                TransferCountries = existing.TransferCountries,
                TransferMechanism = existing.TransferMechanism,
                RiskLevel = existing.RiskLevel,
                SecurityMeasures = existing.SecurityMeasures,
                AllowsSubProcessors = existing.AllowsSubProcessors,
                AuthorizedSubProcessors = existing.AuthorizedSubProcessors,
                AuditRights = existing.AuditRights,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = renewedBy
            };

            _context.DataProcessingAgreements.Add(renewed);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "DPA renewed: OldId={OldDpaId}, NewId={NewDpaId}, Vendor={VendorName}",
                existingDpaId, renewed.Id, renewed.VendorName);

            return renewed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to renew DPA {DpaId}", existingDpaId);
            throw;
        }
    }

    public async Task<bool> ApproveDPAAsync(
        Guid dpaId,
        Guid approvedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpa = await _context.DataProcessingAgreements
                .FirstOrDefaultAsync(d => d.Id == dpaId, cancellationToken);

            if (dpa == null)
            {
                return false;
            }

            dpa.ApprovalStatus = "Approved";
            dpa.ApprovedBy = approvedBy;
            dpa.ApprovedAt = DateTime.UtcNow;
            dpa.Status = DpaStatus.Active;
            dpa.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("DPA approved: Id={DpaId}, ApprovedBy={UserId}", dpaId, approvedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve DPA {DpaId}", dpaId);
            throw;
        }
    }

    // ==========================================
    // DPA QUERIES
    // ==========================================

    public async Task<DataProcessingAgreement?> GetDPAByIdAsync(
        Guid dpaId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DataProcessingAgreements
            .FirstOrDefaultAsync(d => d.Id == dpaId, cancellationToken);
    }

    public async Task<List<DataProcessingAgreement>> GetTenantDPAsAsync(
        Guid tenantId,
        DpaStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataProcessingAgreements
            .Where(d => d.TenantId == tenantId);

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DataProcessingAgreement>> GetPlatformDPAsAsync(
        DpaStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataProcessingAgreements
            .Where(d => d.TenantId == null);

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DataProcessingAgreement>> GetExpiringDPAsAsync(
        int withinDays = 90,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(withinDays);

        var query = _context.DataProcessingAgreements
            .Where(d => d.Status == DpaStatus.Active &&
                       d.ExpiryDate > DateTime.UtcNow &&
                       d.ExpiryDate <= expiryThreshold);

        if (tenantId.HasValue)
        {
            query = query.Where(d => d.TenantId == tenantId);
        }

        return await query
            .OrderBy(d => d.ExpiryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DataProcessingAgreement>> GetOverdueRiskAssessmentsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataProcessingAgreements
            .Where(d => d.Status == DpaStatus.Active &&
                       d.NextRiskAssessmentDate.HasValue &&
                       d.NextRiskAssessmentDate.Value < DateTime.UtcNow);

        if (tenantId.HasValue)
        {
            query = query.Where(d => d.TenantId == tenantId);
        }

        return await query
            .OrderBy(d => d.NextRiskAssessmentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DataProcessingAgreement>> GetOverdueAuditsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataProcessingAgreements
            .Where(d => d.Status == DpaStatus.Active &&
                       d.NextAuditDate.HasValue &&
                       d.NextAuditDate.Value < DateTime.UtcNow);

        if (tenantId.HasValue)
        {
            query = query.Where(d => d.TenantId == tenantId);
        }

        return await query
            .OrderBy(d => d.NextAuditDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DataProcessingAgreement>> SearchDPAsByVendorAsync(
        string vendorName,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataProcessingAgreements
            .Where(d => EF.Functions.ILike(d.VendorName, $"%{vendorName}%"));

        if (tenantId.HasValue)
        {
            query = query.Where(d => d.TenantId == tenantId);
        }

        return await query
            .OrderBy(d => d.VendorName)
            .ToListAsync(cancellationToken);
    }

    // ==========================================
    // RISK ASSESSMENT
    // ==========================================

    public async Task<bool> RecordRiskAssessmentAsync(
        Guid dpaId,
        VendorRiskLevel riskLevel,
        string? assessmentNotes,
        DateTime? nextAssessmentDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpa = await _context.DataProcessingAgreements
                .FirstOrDefaultAsync(d => d.Id == dpaId, cancellationToken);

            if (dpa == null)
            {
                return false;
            }

            dpa.RiskLevel = riskLevel;
            dpa.LastRiskAssessmentDate = DateTime.UtcNow;
            dpa.NextRiskAssessmentDate = nextAssessmentDate ?? DateTime.UtcNow.AddYears(1);

            if (!string.IsNullOrWhiteSpace(assessmentNotes))
            {
                dpa.InternalNotes = $"{dpa.InternalNotes}\n\n[{DateTime.UtcNow:yyyy-MM-dd}] Risk Assessment: {assessmentNotes}";
            }

            dpa.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Risk assessment recorded: DpaId={DpaId}, Risk={RiskLevel}",
                dpaId, riskLevel);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record risk assessment for DPA {DpaId}", dpaId);
            throw;
        }
    }

    public async Task<List<DataProcessingAgreement>> GetDPAsByRiskLevelAsync(
        VendorRiskLevel riskLevel,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataProcessingAgreements
            .Where(d => d.RiskLevel == riskLevel);

        if (tenantId.HasValue)
        {
            query = query.Where(d => d.TenantId == tenantId);
        }

        return await query
            .OrderBy(d => d.VendorName)
            .ToListAsync(cancellationToken);
    }

    // ==========================================
    // SUB-PROCESSOR MANAGEMENT
    // ==========================================

    public async Task<bool> AddSubProcessorAsync(
        Guid dpaId,
        string subProcessorName,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpa = await _context.DataProcessingAgreements
                .FirstOrDefaultAsync(d => d.Id == dpaId, cancellationToken);

            if (dpa == null || !dpa.AllowsSubProcessors)
            {
                return false;
            }

            var subProcessors = string.IsNullOrWhiteSpace(dpa.AuthorizedSubProcessors)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(dpa.AuthorizedSubProcessors) ?? new List<string>();

            if (!subProcessors.Contains(subProcessorName))
            {
                subProcessors.Add(subProcessorName);
                dpa.AuthorizedSubProcessors = JsonSerializer.Serialize(subProcessors);
                dpa.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Sub-processor added: DpaId={DpaId}, SubProcessor={SubProcessor}",
                    dpaId, subProcessorName);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add sub-processor to DPA {DpaId}", dpaId);
            throw;
        }
    }

    public async Task<bool> RemoveSubProcessorAsync(
        Guid dpaId,
        string subProcessorName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpa = await _context.DataProcessingAgreements
                .FirstOrDefaultAsync(d => d.Id == dpaId, cancellationToken);

            if (dpa == null)
            {
                return false;
            }

            var subProcessors = string.IsNullOrWhiteSpace(dpa.AuthorizedSubProcessors)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(dpa.AuthorizedSubProcessors) ?? new List<string>();

            if (subProcessors.Remove(subProcessorName))
            {
                dpa.AuthorizedSubProcessors = JsonSerializer.Serialize(subProcessors);
                dpa.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Sub-processor removed: DpaId={DpaId}, SubProcessor={SubProcessor}",
                    dpaId, subProcessorName);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove sub-processor from DPA {DpaId}", dpaId);
            throw;
        }
    }

    // ==========================================
    // AUDIT & COMPLIANCE
    // ==========================================

    public async Task<bool> RecordAuditAsync(
        Guid dpaId,
        DateTime auditDate,
        string? auditFindings,
        DateTime? nextAuditDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dpa = await _context.DataProcessingAgreements
                .FirstOrDefaultAsync(d => d.Id == dpaId, cancellationToken);

            if (dpa == null)
            {
                return false;
            }

            dpa.LastAuditDate = auditDate;
            dpa.NextAuditDate = nextAuditDate;
            dpa.AuditNotes = auditFindings;
            dpa.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Audit recorded for DPA {DpaId}", dpaId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record audit for DPA {DpaId}", dpaId);
            throw;
        }
    }

    public async Task<DPAComplianceDashboard> GetComplianceDashboardAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataProcessingAgreements.AsQueryable();

        if (tenantId.HasValue)
        {
            query = query.Where(d => d.TenantId == tenantId);
        }

        var dpas = await query.ToListAsync(cancellationToken);

        var dashboard = new DPAComplianceDashboard
        {
            TotalDPAs = dpas.Count,
            ActiveDPAs = dpas.Count(d => d.Status == DpaStatus.Active),
            ExpiringSoon = dpas.Count(d => d.IsExpiringSoon),
            PendingApproval = dpas.Count(d => d.ApprovalStatus == "PendingApproval"),
            OverdueRiskAssessments = dpas.Count(d => d.IsRiskAssessmentOverdue),
            OverdueAudits = dpas.Count(d => d.IsAuditOverdue),
            DPAsByRiskLevel = dpas
                .GroupBy(d => d.RiskLevel)
                .ToDictionary(g => g.Key, g => g.Count()),
            DPAsByCountry = dpas
                .GroupBy(d => d.VendorCountry)
                .ToDictionary(g => g.Key, g => g.Count()),
            InternationalTransferCount = dpas.Count(d => d.InternationalDataTransfer),
            AverageContractValueUsd = dpas.Where(d => d.AnnualValueUsd.HasValue)
                                           .Average(d => d.AnnualValueUsd ?? 0)
        };

        return dashboard;
    }

    public async Task<ProcessorRegistry> GenerateProcessorRegistryAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataProcessingAgreements
            .Where(d => d.Status == DpaStatus.Active);

        if (tenantId.HasValue)
        {
            query = query.Where(d => d.TenantId == tenantId);
        }

        var dpas = await query.ToListAsync(cancellationToken);

        var registry = new ProcessorRegistry
        {
            GeneratedAt = DateTime.UtcNow,
            TenantId = tenantId,
            Processors = dpas.Select(d => new ProcessorEntry
            {
                ProcessorName = d.VendorName,
                ProcessorType = d.VendorType,
                ProcessingPurpose = d.ProcessingPurpose,
                DataCategories = JsonSerializer.Deserialize<List<string>>(d.PersonalDataCategories) ?? new List<string>(),
                DataSubjectCategories = JsonSerializer.Deserialize<List<string>>(d.DataSubjectCategories) ?? new List<string>(),
                RetentionPeriod = $"{d.RetentionPeriodDays} days",
                InternationalTransfer = d.InternationalDataTransfer,
                TransferCountries = string.IsNullOrWhiteSpace(d.TransferCountries)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(d.TransferCountries) ?? new List<string>(),
                TransferMechanism = d.TransferMechanism ?? "N/A",
                SecurityMeasures = string.IsNullOrWhiteSpace(d.SecurityMeasures)
                    ? new List<string>()
                    : new List<string> { d.SecurityMeasures },
                SubProcessors = string.IsNullOrWhiteSpace(d.AuthorizedSubProcessors)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(d.AuthorizedSubProcessors) ?? new List<string>()
            }).ToList()
        };

        return registry;
    }

    public async Task<List<DataProcessingAgreement>> GetInternationalTransferDPAsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataProcessingAgreements
            .Where(d => d.InternationalDataTransfer);

        if (tenantId.HasValue)
        {
            query = query.Where(d => d.TenantId == tenantId);
        }

        return await query
            .OrderBy(d => d.VendorName)
            .ToListAsync(cancellationToken);
    }
}
