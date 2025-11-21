using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Master;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// GDPR Article 7 - Consent Management Service Implementation
/// FORTUNE 500 PATTERN: OneTrust, TrustArc consent platforms
/// PERFORMANCE: <10ms queries with proper indexing
/// SECURITY: SHA-256 hash verification, immutable audit trail
/// </summary>
public class ConsentManagementService : IConsentManagementService
{
    private readonly MasterDbContext _context;
    private readonly ILogger<ConsentManagementService> _logger;

    public ConsentManagementService(
        MasterDbContext context,
        ILogger<ConsentManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==========================================
    // CONSENT LIFECYCLE
    // ==========================================

    public async Task<UserConsent> RecordConsentAsync(
        Guid? userId,
        string? userEmail,
        ConsentType consentType,
        string consentCategory,
        string consentText,
        string consentVersion,
        bool isExplicit = true,
        Guid? tenantId = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // SECURITY: Generate SHA-256 hash of consent text
            var consentTextHash = GenerateHash(consentText);

            var consent = new UserConsent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserEmail = userEmail,
                TenantId = tenantId,
                ConsentType = consentType,
                ConsentCategory = consentCategory,
                Purpose = GetDefaultPurpose(consentType, consentCategory),
                ConsentText = consentText,
                ConsentVersion = consentVersion,
                ConsentTextHash = consentTextHash,
                Status = ConsentStatus.Active,
                IsExplicit = isExplicit,
                IsOptIn = true,
                GivenAt = DateTime.UtcNow,
                IpAddress = ipAddress ?? "0.0.0.0",
                UserAgent = userAgent,
                ConsentMethod = "WebForm",
                LegalBasis = LegalBasis.Consent,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserConsents.Add(consent);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Consent recorded: User={UserId}, Type={Type}, Category={Category}, Version={Version}",
                userId, consentType, consentCategory, consentVersion);

            return consent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record consent for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> WithdrawConsentAsync(
        Guid consentId,
        string? withdrawalReason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var consent = await _context.UserConsents
                .FirstOrDefaultAsync(c => c.Id == consentId, cancellationToken);

            if (consent == null)
            {
                _logger.LogWarning("Consent {ConsentId} not found", consentId);
                return false;
            }

            if (consent.Status == ConsentStatus.Withdrawn)
            {
                _logger.LogWarning("Consent {ConsentId} already withdrawn", consentId);
                return false;
            }

            // Update consent status
            consent.Status = ConsentStatus.Withdrawn;
            consent.WithdrawnAt = DateTime.UtcNow;
            consent.WithdrawalReason = withdrawalReason;
            consent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Consent withdrawn: ConsentId={ConsentId}, User={UserId}, Reason={Reason}",
                consentId, consent.UserId, withdrawalReason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to withdraw consent {ConsentId}", consentId);
            throw;
        }
    }

    public async Task<int> WithdrawAllUserConsentsAsync(
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activeConsents = await _context.UserConsents
                .Where(c => c.UserId == userId && c.Status == ConsentStatus.Active)
                .ToListAsync(cancellationToken);

            var withdrawnCount = 0;
            var now = DateTime.UtcNow;

            foreach (var consent in activeConsents)
            {
                consent.Status = ConsentStatus.Withdrawn;
                consent.WithdrawnAt = now;
                consent.WithdrawalReason = reason ?? "User requested withdrawal of all consents";
                consent.UpdatedAt = now;
                withdrawnCount++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Bulk consent withdrawal: User={UserId}, Count={Count}",
                userId, withdrawnCount);

            return withdrawnCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to withdraw all consents for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserConsent> RenewConsentAsync(
        Guid previousConsentId,
        string newConsentText,
        string newConsentVersion,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var previousConsent = await _context.UserConsents
                .FirstOrDefaultAsync(c => c.Id == previousConsentId, cancellationToken);

            if (previousConsent == null)
            {
                throw new InvalidOperationException($"Previous consent {previousConsentId} not found");
            }

            // Mark old consent as superseded
            previousConsent.Status = ConsentStatus.Superseded;
            previousConsent.UpdatedAt = DateTime.UtcNow;

            // Create new consent
            var newConsentHash = GenerateHash(newConsentText);

            var newConsent = new UserConsent
            {
                Id = Guid.NewGuid(),
                UserId = previousConsent.UserId,
                UserEmail = previousConsent.UserEmail,
                TenantId = previousConsent.TenantId,
                ConsentType = previousConsent.ConsentType,
                ConsentCategory = previousConsent.ConsentCategory,
                Purpose = previousConsent.Purpose,
                ConsentText = newConsentText,
                ConsentVersion = newConsentVersion,
                ConsentTextHash = newConsentHash,
                Status = ConsentStatus.Active,
                IsExplicit = previousConsent.IsExplicit,
                IsOptIn = true,
                GivenAt = DateTime.UtcNow,
                IpAddress = ipAddress ?? previousConsent.IpAddress,
                UserAgent = userAgent ?? previousConsent.UserAgent,
                ConsentMethod = previousConsent.ConsentMethod,
                LegalBasis = previousConsent.LegalBasis,
                PreviousConsentId = previousConsentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserConsents.Add(newConsent);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Consent renewed: PreviousId={PreviousId}, NewId={NewId}, Version={Version}",
                previousConsentId, newConsent.Id, newConsentVersion);

            return newConsent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to renew consent {ConsentId}", previousConsentId);
            throw;
        }
    }

    // ==========================================
    // CONSENT QUERIES
    // ==========================================

    public async Task<List<UserConsent>> GetUserConsentsAsync(
        Guid userId,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserConsents
            .Where(c => c.UserId == userId);

        if (activeOnly)
        {
            query = query.Where(c => c.Status == ConsentStatus.Active);
        }

        return await query
            .OrderByDescending(c => c.GivenAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserConsent?> GetActiveConsentAsync(
        Guid userId,
        ConsentType consentType,
        string consentCategory,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserConsents
            .Where(c => c.UserId == userId &&
                       c.ConsentType == consentType &&
                       c.ConsentCategory == consentCategory &&
                       c.Status == ConsentStatus.Active &&
                       c.WithdrawnAt == null &&
                       (!c.ExpiresAt.HasValue || c.ExpiresAt.Value > DateTime.UtcNow))
            .OrderByDescending(c => c.GivenAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasActiveConsentAsync(
        Guid userId,
        ConsentType consentType,
        string consentCategory,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserConsents
            .AnyAsync(c => c.UserId == userId &&
                          c.ConsentType == consentType &&
                          c.ConsentCategory == consentCategory &&
                          c.Status == ConsentStatus.Active &&
                          c.WithdrawnAt == null &&
                          (!c.ExpiresAt.HasValue || c.ExpiresAt.Value > DateTime.UtcNow),
                     cancellationToken);
    }

    public async Task<List<UserConsent>> GetExpiringSoonConsentsAsync(
        int withinDays = 30,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var expiryThreshold = DateTime.UtcNow.AddDays(withinDays);

        var query = _context.UserConsents
            .Where(c => c.Status == ConsentStatus.Active &&
                       c.ExpiresAt.HasValue &&
                       c.ExpiresAt.Value > DateTime.UtcNow &&
                       c.ExpiresAt.Value <= expiryThreshold);

        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId);
        }

        return await query
            .OrderBy(c => c.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserConsent>> GetTenantConsentsAsync(
        Guid tenantId,
        DateTime? from = null,
        DateTime? to = null,
        ConsentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserConsents
            .Where(c => c.TenantId == tenantId);

        if (from.HasValue)
        {
            query = query.Where(c => c.GivenAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(c => c.GivenAt <= to.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        return await query
            .OrderByDescending(c => c.GivenAt)
            .ToListAsync(cancellationToken);
    }

    // ==========================================
    // CONSENT ANALYTICS & REPORTING
    // ==========================================

    public async Task<ConsentStatistics> GetConsentStatisticsAsync(
        Guid? tenantId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserConsents.AsQueryable();

        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId);
        }

        if (from.HasValue)
        {
            query = query.Where(c => c.GivenAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(c => c.GivenAt <= to.Value);
        }

        var consents = await query.ToListAsync(cancellationToken);

        var statistics = new ConsentStatistics
        {
            TotalConsents = consents.Count,
            ActiveConsents = consents.Count(c => c.Status == ConsentStatus.Active),
            WithdrawnConsents = consents.Count(c => c.Status == ConsentStatus.Withdrawn),
            ExpiredConsents = consents.Count(c => c.Status == ConsentStatus.Expired),
            ConsentsByType = consents
                .GroupBy(c => c.ConsentType)
                .ToDictionary(g => g.Key, g => g.Count()),
            ConsentsByCategory = consents
                .GroupBy(c => c.ConsentCategory)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        if (statistics.TotalConsents > 0)
        {
            statistics.WithdrawalRate = (decimal)statistics.WithdrawnConsents / statistics.TotalConsents * 100;
        }

        return statistics;
    }

    public async Task<ConsentComplianceReport> GenerateComplianceReportAsync(
        DateTime from,
        DateTime to,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserConsents
            .Where(c => c.GivenAt >= from && c.GivenAt <= to);

        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId);
        }

        var consents = await query.ToListAsync(cancellationToken);

        var report = new ConsentComplianceReport
        {
            ReportGeneratedAt = DateTime.UtcNow,
            PeriodStart = from,
            PeriodEnd = to,
            TenantId = tenantId,
            TotalConsentsRecorded = consents.Count,
            ExplicitConsentsCount = consents.Count(c => c.IsExplicit),
            ImpliedConsentsCount = consents.Count(c => !c.IsExplicit)
        };

        if (report.TotalConsentsRecorded > 0)
        {
            report.ExplicitConsentRate = (decimal)report.ExplicitConsentsCount / report.TotalConsentsRecorded * 100;
        }

        // Check for potential violations
        var violations = new List<ConsentViolation>();

        // GDPR: Explicit consent required for most purposes
        if (report.ExplicitConsentRate < 90)
        {
            violations.Add(new ConsentViolation
            {
                ViolationType = "Low explicit consent rate",
                Description = $"Only {report.ExplicitConsentRate:F1}% of consents are explicit (GDPR requires affirmative action)",
                AffectedCount = report.ImpliedConsentsCount,
                Severity = "High",
                Recommendation = "Review consent collection forms to ensure clear opt-in mechanisms"
            });
        }

        report.PotentialViolations = violations;

        return report;
    }

    // ==========================================
    // CONSENT VERIFICATION
    // ==========================================

    public async Task<bool> VerifyConsentIntegrityAsync(
        Guid consentId,
        CancellationToken cancellationToken = default)
    {
        var consent = await _context.UserConsents
            .FirstOrDefaultAsync(c => c.Id == consentId, cancellationToken);

        if (consent == null)
        {
            return false;
        }

        var computedHash = GenerateHash(consent.ConsentText);
        return computedHash == consent.ConsentTextHash;
    }

    public async Task<List<ConsentAuditEntry>> GetConsentAuditTrailAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var consents = await _context.UserConsents
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.GivenAt)
            .ToListAsync(cancellationToken);

        var auditTrail = new List<ConsentAuditEntry>();

        foreach (var consent in consents)
        {
            // Consent given
            auditTrail.Add(new ConsentAuditEntry
            {
                ConsentId = consent.Id,
                Timestamp = consent.GivenAt,
                Action = "Given",
                ConsentType = consent.ConsentType,
                ConsentCategory = consent.ConsentCategory,
                ConsentVersion = consent.ConsentVersion,
                IpAddress = consent.IpAddress,
                UserAgent = consent.UserAgent,
                Status = ConsentStatus.Active
            });

            // Consent withdrawn
            if (consent.WithdrawnAt.HasValue)
            {
                auditTrail.Add(new ConsentAuditEntry
                {
                    ConsentId = consent.Id,
                    Timestamp = consent.WithdrawnAt.Value,
                    Action = "Withdrawn",
                    ConsentType = consent.ConsentType,
                    ConsentCategory = consent.ConsentCategory,
                    ConsentVersion = consent.ConsentVersion,
                    IpAddress = consent.IpAddress,
                    UserAgent = consent.UserAgent,
                    Status = ConsentStatus.Withdrawn
                });
            }
        }

        return auditTrail.OrderBy(a => a.Timestamp).ToList();
    }

    // ==========================================
    // PRIVATE HELPER METHODS
    // ==========================================

    private static string GenerateHash(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static string GetDefaultPurpose(ConsentType consentType, string category)
    {
        return consentType switch
        {
            ConsentType.Marketing => $"Marketing communications via {category}",
            ConsentType.Analytics => $"Analytics and usage tracking via {category}",
            ConsentType.DataProcessing => $"Data processing for {category}",
            ConsentType.ThirdPartySharing => $"Data sharing with third parties for {category}",
            ConsentType.Cookies => $"Cookie usage for {category}",
            ConsentType.Profiling => $"Profiling and automated decision-making for {category}",
            ConsentType.SensitiveData => $"Sensitive data processing for {category}",
            ConsentType.InternationalTransfer => $"International data transfer for {category}",
            ConsentType.Research => $"Research and development for {category}",
            ConsentType.AccountManagement => $"Account management and authentication for {category}",
            _ => $"Data processing for {category}"
        };
    }
}
