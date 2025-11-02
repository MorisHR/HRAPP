using HRMS.Application.DTOs.SectorDtos;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service for managing industry sectors and hierarchical structures
/// </summary>
public interface ISectorService
{
    /// <summary>
    /// Get all sectors with hierarchical structure
    /// </summary>
    Task<List<IndustrySectorDto>> GetAllSectorsHierarchicalAsync();

    /// <summary>
    /// Get all sectors as flat list
    /// </summary>
    Task<List<IndustrySectorListDto>> GetAllSectorsFlatAsync(bool activeOnly = true);

    /// <summary>
    /// Get sector by ID with full details
    /// </summary>
    Task<IndustrySectorDto?> GetSectorByIdAsync(int id);

    /// <summary>
    /// Get sector by code
    /// </summary>
    Task<IndustrySectorDto?> GetSectorByCodeAsync(string sectorCode);

    /// <summary>
    /// Get all compliance rules for a specific sector
    /// </summary>
    Task<List<SectorComplianceRuleDto>> GetComplianceRulesForSectorAsync(int sectorId);

    /// <summary>
    /// Get compliance rule by category for a sector
    /// </summary>
    Task<SectorComplianceRuleDto?> GetComplianceRuleByCategoryAsync(int sectorId, string ruleCategory);

    /// <summary>
    /// Get all parent sectors (top-level sectors without parents)
    /// </summary>
    Task<List<IndustrySectorListDto>> GetParentSectorsAsync();

    /// <summary>
    /// Get all sub-sectors for a parent sector
    /// </summary>
    Task<List<IndustrySectorListDto>> GetSubSectorsAsync(int parentSectorId);

    /// <summary>
    /// Create new industry sector (Super Admin only)
    /// </summary>
    Task<IndustrySectorDto> CreateSectorAsync(CreateSectorDto dto);

    /// <summary>
    /// Update existing sector (Super Admin only)
    /// </summary>
    Task<IndustrySectorDto> UpdateSectorAsync(int id, UpdateSectorDto dto);

    /// <summary>
    /// Search sectors by name or code
    /// </summary>
    Task<List<IndustrySectorListDto>> SearchSectorsAsync(string query);

    /// <summary>
    /// Get sectors that require special permits
    /// </summary>
    Task<List<IndustrySectorListDto>> GetSectorsRequiringPermitsAsync();
}
