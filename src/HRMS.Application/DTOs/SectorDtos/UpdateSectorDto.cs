using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs.SectorDtos;

/// <summary>
/// Update existing industry sector
/// </summary>
public class UpdateSectorDto
{
    [Required]
    [StringLength(300)]
    public string SectorName { get; set; } = string.Empty;

    [StringLength(300)]
    public string? SectorNameFrench { get; set; }

    [StringLength(200)]
    public string? RemunerationOrderReference { get; set; }

    public int? RemunerationOrderYear { get; set; }

    public bool IsActive { get; set; }
    public bool RequiresSpecialPermits { get; set; }
}
