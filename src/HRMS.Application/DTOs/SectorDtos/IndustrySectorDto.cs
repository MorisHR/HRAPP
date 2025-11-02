using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.SectorDtos;

/// <summary>
/// Full industry sector details with hierarchical relationships
/// </summary>
public class IndustrySectorDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string SectorCode { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string SectorName { get; set; } = string.Empty;

    [StringLength(300)]
    public string? SectorNameFrench { get; set; }

    public int? ParentSectorId { get; set; }
    public string? ParentSectorName { get; set; }

    [StringLength(200)]
    public string? RemunerationOrderReference { get; set; }

    public int? RemunerationOrderYear { get; set; }

    public bool IsActive { get; set; }
    public bool RequiresSpecialPermits { get; set; }

    // Hierarchical structure
    public List<IndustrySectorDto> SubSectors { get; set; } = new();

    // Compliance rules count
    public int ComplianceRulesCount { get; set; }

    // Tenants using this sector count
    public int TenantsCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
