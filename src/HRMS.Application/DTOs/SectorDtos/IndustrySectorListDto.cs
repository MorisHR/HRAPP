namespace HRMS.Application.DTOs.SectorDtos;

/// <summary>
/// Simplified sector information for lists
/// </summary>
public class IndustrySectorListDto
{
    public int Id { get; set; }
    public string SectorCode { get; set; } = string.Empty;
    public string SectorName { get; set; } = string.Empty;
    public string? SectorNameFrench { get; set; }
    public int? ParentSectorId { get; set; }
    public string? ParentSectorName { get; set; }
    public string? RemunerationOrderReference { get; set; }
    public int? RemunerationOrderYear { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresSpecialPermits { get; set; }
    public int SubSectorsCount { get; set; }
    public int ComplianceRulesCount { get; set; }
}
