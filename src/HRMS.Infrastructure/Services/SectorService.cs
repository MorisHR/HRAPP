using HRMS.Application.DTOs.SectorDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Master;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for managing industry sectors and hierarchical structures
/// </summary>
public class SectorService : ISectorService
{
    private readonly MasterDbContext _context;
    private readonly ILogger<SectorService> _logger;

    public SectorService(MasterDbContext context, ILogger<SectorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<IndustrySectorDto>> GetAllSectorsHierarchicalAsync()
    {
        _logger.LogInformation("Fetching all sectors with hierarchical structure");

        var allSectors = await _context.IndustrySectors
            .Include(s => s.ComplianceRules)
            .Where(s => s.IsActive)
            .OrderBy(s => s.ParentSectorId ?? 0)
            .ThenBy(s => s.SectorName)
            .ToListAsync();

        // Get parent sectors first
        var parentSectors = allSectors.Where(s => s.ParentSectorId == null).ToList();

        var result = new List<IndustrySectorDto>();

        foreach (var parent in parentSectors)
        {
            var parentDto = MapToDto(parent);

            // Get sub-sectors
            var subSectors = allSectors.Where(s => s.ParentSectorId == parent.Id).ToList();
            parentDto.SubSectors = subSectors.Select(MapToDto).ToList();

            // Count compliance rules
            parentDto.ComplianceRulesCount = parent.ComplianceRules?.Count ?? 0;

            result.Add(parentDto);
        }

        _logger.LogInformation("Fetched {Count} parent sectors with sub-sectors", result.Count);

        return result;
    }

    public async Task<List<IndustrySectorListDto>> GetAllSectorsFlatAsync(bool activeOnly = true)
    {
        _logger.LogInformation("Fetching all sectors as flat list (activeOnly: {ActiveOnly})", activeOnly);

        var query = _context.IndustrySectors.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        var sectors = await query
            .Include(s => s.ParentSector)
            .Include(s => s.SubSectors)
            .Include(s => s.ComplianceRules)
            .OrderBy(s => s.SectorName)
            .ToListAsync();

        var result = sectors.Select(s => new IndustrySectorListDto
        {
            Id = s.Id,
            SectorCode = s.SectorCode,
            SectorName = s.SectorName,
            SectorNameFrench = s.SectorNameFrench,
            ParentSectorId = s.ParentSectorId,
            ParentSectorName = s.ParentSector?.SectorName,
            RemunerationOrderReference = s.RemunerationOrderReference,
            RemunerationOrderYear = s.RemunerationOrderYear,
            IsActive = s.IsActive,
            RequiresSpecialPermits = s.RequiresSpecialPermits,
            SubSectorsCount = s.SubSectors?.Count ?? 0,
            ComplianceRulesCount = s.ComplianceRules?.Count ?? 0
        }).ToList();

        _logger.LogInformation("Fetched {Count} sectors", result.Count);

        return result;
    }

    public async Task<IndustrySectorDto?> GetSectorByIdAsync(int id)
    {
        _logger.LogInformation("Fetching sector by ID: {SectorId}", id);

        var sector = await _context.IndustrySectors
            .Include(s => s.ParentSector)
            .Include(s => s.SubSectors)
            .Include(s => s.ComplianceRules)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sector == null)
        {
            _logger.LogWarning("Sector not found: {SectorId}", id);
            return null;
        }

        var dto = MapToDto(sector);

        // Add sub-sectors
        if (sector.SubSectors != null && sector.SubSectors.Any())
        {
            dto.SubSectors = sector.SubSectors.Select(MapToDto).ToList();
        }

        // Count compliance rules
        dto.ComplianceRulesCount = sector.ComplianceRules?.Count ?? 0;

        // Count tenants using this sector
        dto.TenantsCount = await _context.Tenants.CountAsync(t => t.SectorId == id);

        _logger.LogInformation("Sector found: {SectorName} ({SectorCode})", sector.SectorName, sector.SectorCode);

        return dto;
    }

    public async Task<IndustrySectorDto?> GetSectorByCodeAsync(string sectorCode)
    {
        _logger.LogInformation("Fetching sector by code: {SectorCode}", sectorCode);

        var sector = await _context.IndustrySectors
            .Include(s => s.ParentSector)
            .Include(s => s.SubSectors)
            .Include(s => s.ComplianceRules)
            .FirstOrDefaultAsync(s => s.SectorCode == sectorCode);

        if (sector == null)
        {
            _logger.LogWarning("Sector not found: {SectorCode}", sectorCode);
            return null;
        }

        return MapToDto(sector);
    }

    public async Task<List<SectorComplianceRuleDto>> GetComplianceRulesForSectorAsync(int sectorId)
    {
        _logger.LogInformation("Fetching compliance rules for sector: {SectorId}", sectorId);

        var sector = await _context.IndustrySectors
            .Include(s => s.ComplianceRules)
            .FirstOrDefaultAsync(s => s.Id == sectorId);

        if (sector == null)
        {
            throw new Exception($"Sector not found: {sectorId}");
        }

        var rules = await _context.SectorComplianceRules
            .Where(r => r.SectorId == sectorId)
            .OrderBy(r => r.RuleCategory)
            .ThenBy(r => r.EffectiveFrom)
            .ToListAsync();

        var result = rules.Select(r => new SectorComplianceRuleDto
        {
            Id = r.Id,
            SectorId = r.SectorId,
            SectorName = sector.SectorName,
            SectorCode = sector.SectorCode,
            RuleCategory = r.RuleCategory,
            RuleName = r.RuleName,
            RuleConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(r.RuleConfig) ?? new(),
            EffectiveFrom = r.EffectiveFrom,
            EffectiveTo = r.EffectiveTo,
            LegalReference = r.LegalReference,
            IsActive = !r.IsDeleted,
            IsCurrent = r.EffectiveFrom <= DateTime.UtcNow && (r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow),
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();

        _logger.LogInformation("Found {Count} compliance rules for sector {SectorId}", result.Count, sectorId);

        return result;
    }

    public async Task<SectorComplianceRuleDto?> GetComplianceRuleByCategoryAsync(int sectorId, string ruleCategory)
    {
        _logger.LogInformation("Fetching compliance rule for sector {SectorId}, category: {RuleCategory}", sectorId, ruleCategory);

        var sector = await _context.IndustrySectors.FindAsync(sectorId);
        if (sector == null)
        {
            throw new Exception($"Sector not found: {sectorId}");
        }

        var now = DateTime.UtcNow;

        var rule = await _context.SectorComplianceRules
            .Where(r => r.SectorId == sectorId && r.RuleCategory == ruleCategory)
            .Where(r => r.EffectiveFrom <= now && (r.EffectiveTo == null || r.EffectiveTo >= now))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (rule == null)
        {
            _logger.LogWarning("Compliance rule not found for sector {SectorId}, category: {RuleCategory}", sectorId, ruleCategory);
            return null;
        }

        return new SectorComplianceRuleDto
        {
            Id = rule.Id,
            SectorId = rule.SectorId,
            SectorName = sector.SectorName,
            SectorCode = sector.SectorCode,
            RuleCategory = rule.RuleCategory,
            RuleName = rule.RuleName,
            RuleConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.RuleConfig) ?? new(),
            EffectiveFrom = rule.EffectiveFrom,
            EffectiveTo = rule.EffectiveTo,
            LegalReference = rule.LegalReference,
            IsActive = !rule.IsDeleted,
            IsCurrent = true,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }

    public async Task<List<IndustrySectorListDto>> GetParentSectorsAsync()
    {
        _logger.LogInformation("Fetching parent sectors");

        var sectors = await _context.IndustrySectors
            .Where(s => s.ParentSectorId == null && s.IsActive)
            .Include(s => s.SubSectors)
            .Include(s => s.ComplianceRules)
            .OrderBy(s => s.SectorName)
            .ToListAsync();

        var result = sectors.Select(s => new IndustrySectorListDto
        {
            Id = s.Id,
            SectorCode = s.SectorCode,
            SectorName = s.SectorName,
            SectorNameFrench = s.SectorNameFrench,
            ParentSectorId = null,
            ParentSectorName = null,
            RemunerationOrderReference = s.RemunerationOrderReference,
            RemunerationOrderYear = s.RemunerationOrderYear,
            IsActive = s.IsActive,
            RequiresSpecialPermits = s.RequiresSpecialPermits,
            SubSectorsCount = s.SubSectors?.Count ?? 0,
            ComplianceRulesCount = s.ComplianceRules?.Count ?? 0
        }).ToList();

        _logger.LogInformation("Found {Count} parent sectors", result.Count);

        return result;
    }

    public async Task<List<IndustrySectorListDto>> GetSubSectorsAsync(int parentSectorId)
    {
        _logger.LogInformation("Fetching sub-sectors for parent: {ParentSectorId}", parentSectorId);

        var parentSector = await _context.IndustrySectors.FindAsync(parentSectorId);
        if (parentSector == null)
        {
            throw new Exception($"Parent sector not found: {parentSectorId}");
        }

        var sectors = await _context.IndustrySectors
            .Where(s => s.ParentSectorId == parentSectorId && s.IsActive)
            .Include(s => s.ComplianceRules)
            .OrderBy(s => s.SectorName)
            .ToListAsync();

        var result = sectors.Select(s => new IndustrySectorListDto
        {
            Id = s.Id,
            SectorCode = s.SectorCode,
            SectorName = s.SectorName,
            SectorNameFrench = s.SectorNameFrench,
            ParentSectorId = s.ParentSectorId,
            ParentSectorName = parentSector.SectorName,
            RemunerationOrderReference = s.RemunerationOrderReference,
            RemunerationOrderYear = s.RemunerationOrderYear,
            IsActive = s.IsActive,
            RequiresSpecialPermits = s.RequiresSpecialPermits,
            SubSectorsCount = 0,
            ComplianceRulesCount = s.ComplianceRules?.Count ?? 0
        }).ToList();

        _logger.LogInformation("Found {Count} sub-sectors for parent {ParentSectorId}", result.Count, parentSectorId);

        return result;
    }

    public async Task<IndustrySectorDto> CreateSectorAsync(CreateSectorDto dto)
    {
        _logger.LogInformation("Creating new sector: {SectorCode} - {SectorName}", dto.SectorCode, dto.SectorName);

        // Check if sector code already exists
        var existingSector = await _context.IndustrySectors
            .FirstOrDefaultAsync(s => s.SectorCode == dto.SectorCode);

        if (existingSector != null)
        {
            throw new Exception($"Sector code '{dto.SectorCode}' already exists");
        }

        // Validate parent sector if provided
        if (dto.ParentSectorId.HasValue)
        {
            var parentSector = await _context.IndustrySectors.FindAsync(dto.ParentSectorId.Value);
            if (parentSector == null)
            {
                throw new Exception($"Parent sector not found: {dto.ParentSectorId}");
            }
        }

        var sector = new IndustrySector
        {
            SectorCode = dto.SectorCode,
            SectorName = dto.SectorName,
            SectorNameFrench = dto.SectorNameFrench,
            ParentSectorId = dto.ParentSectorId,
            RemunerationOrderReference = dto.RemunerationOrderReference,
            RemunerationOrderYear = dto.RemunerationOrderYear,
            IsActive = dto.IsActive,
            RequiresSpecialPermits = dto.RequiresSpecialPermits,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.IndustrySectors.Add(sector);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sector created successfully: {SectorId} - {SectorName}", sector.Id, sector.SectorName);

        return MapToDto(sector);
    }

    public async Task<IndustrySectorDto> UpdateSectorAsync(int id, UpdateSectorDto dto)
    {
        _logger.LogInformation("Updating sector: {SectorId}", id);

        var sector = await _context.IndustrySectors.FindAsync(id);
        if (sector == null)
        {
            throw new Exception($"Sector not found: {id}");
        }

        sector.SectorName = dto.SectorName;
        sector.SectorNameFrench = dto.SectorNameFrench;
        sector.RemunerationOrderReference = dto.RemunerationOrderReference;
        sector.RemunerationOrderYear = dto.RemunerationOrderYear;
        sector.IsActive = dto.IsActive;
        sector.RequiresSpecialPermits = dto.RequiresSpecialPermits;
        sector.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Sector updated successfully: {SectorId} - {SectorName}", sector.Id, sector.SectorName);

        return MapToDto(sector);
    }

    public async Task<List<IndustrySectorListDto>> SearchSectorsAsync(string query)
    {
        _logger.LogInformation("Searching sectors with query: {Query}", query);

        var sectors = await _context.IndustrySectors
            .Where(s => s.IsActive)
            .Where(s => s.SectorName.Contains(query) || s.SectorCode.Contains(query) || (s.SectorNameFrench != null && s.SectorNameFrench.Contains(query)))
            .Include(s => s.ParentSector)
            .Include(s => s.SubSectors)
            .Include(s => s.ComplianceRules)
            .OrderBy(s => s.SectorName)
            .Take(20)
            .ToListAsync();

        var result = sectors.Select(s => new IndustrySectorListDto
        {
            Id = s.Id,
            SectorCode = s.SectorCode,
            SectorName = s.SectorName,
            SectorNameFrench = s.SectorNameFrench,
            ParentSectorId = s.ParentSectorId,
            ParentSectorName = s.ParentSector?.SectorName,
            RemunerationOrderReference = s.RemunerationOrderReference,
            RemunerationOrderYear = s.RemunerationOrderYear,
            IsActive = s.IsActive,
            RequiresSpecialPermits = s.RequiresSpecialPermits,
            SubSectorsCount = s.SubSectors?.Count ?? 0,
            ComplianceRulesCount = s.ComplianceRules?.Count ?? 0
        }).ToList();

        _logger.LogInformation("Found {Count} sectors matching query: {Query}", result.Count, query);

        return result;
    }

    public async Task<List<IndustrySectorListDto>> GetSectorsRequiringPermitsAsync()
    {
        _logger.LogInformation("Fetching sectors requiring special permits");

        var sectors = await _context.IndustrySectors
            .Where(s => s.RequiresSpecialPermits && s.IsActive)
            .Include(s => s.ParentSector)
            .Include(s => s.ComplianceRules)
            .OrderBy(s => s.SectorName)
            .ToListAsync();

        var result = sectors.Select(s => new IndustrySectorListDto
        {
            Id = s.Id,
            SectorCode = s.SectorCode,
            SectorName = s.SectorName,
            SectorNameFrench = s.SectorNameFrench,
            ParentSectorId = s.ParentSectorId,
            ParentSectorName = s.ParentSector?.SectorName,
            RemunerationOrderReference = s.RemunerationOrderReference,
            RemunerationOrderYear = s.RemunerationOrderYear,
            IsActive = s.IsActive,
            RequiresSpecialPermits = s.RequiresSpecialPermits,
            SubSectorsCount = 0,
            ComplianceRulesCount = s.ComplianceRules?.Count ?? 0
        }).ToList();

        _logger.LogInformation("Found {Count} sectors requiring permits", result.Count);

        return result;
    }

    // Helper method to map entity to DTO
    private IndustrySectorDto MapToDto(IndustrySector sector)
    {
        return new IndustrySectorDto
        {
            Id = sector.Id,
            SectorCode = sector.SectorCode,
            SectorName = sector.SectorName,
            SectorNameFrench = sector.SectorNameFrench,
            ParentSectorId = sector.ParentSectorId,
            ParentSectorName = sector.ParentSector?.SectorName,
            RemunerationOrderReference = sector.RemunerationOrderReference,
            RemunerationOrderYear = sector.RemunerationOrderYear,
            IsActive = sector.IsActive,
            RequiresSpecialPermits = sector.RequiresSpecialPermits,
            SubSectors = new List<IndustrySectorDto>(),
            ComplianceRulesCount = 0,
            TenantsCount = 0,
            CreatedAt = sector.CreatedAt,
            UpdatedAt = sector.UpdatedAt
        };
    }
}
