using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.SectorDtos;

/// <summary>
/// Create new industry sector
/// </summary>
public class CreateSectorDto
{
    [Required]
    [StringLength(100)]
    public string SectorCode { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string SectorName { get; set; } = string.Empty;

    [StringLength(300)]
    public string? SectorNameFrench { get; set; }

    public int? ParentSectorId { get; set; }

    [StringLength(200)]
    public string? RemunerationOrderReference { get; set; }

    public int? RemunerationOrderYear { get; set; }

    public bool IsActive { get; set; } = true;
    public bool RequiresSpecialPermits { get; set; } = false;
}
