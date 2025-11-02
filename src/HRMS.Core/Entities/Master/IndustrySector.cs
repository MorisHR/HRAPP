using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Master;

/// <summary>
/// Mauritius Industry Sectors based on Remuneration Orders
/// Master data stored in Master Schema
/// </summary>
public class IndustrySector : IntIdBaseEntity
{
    public string SectorCode { get; set; } = string.Empty;
    public string SectorName { get; set; } = string.Empty;
    public string? SectorNameFrench { get; set; }
    public string? Description { get; set; }

    public int? ParentSectorId { get; set; }

    public string? RemunerationOrderReference { get; set; }
    public int? RemunerationOrderYear { get; set; }

    public bool IsActive { get; set; } = true;
    public bool RequiresSpecialPermits { get; set; } = false;

    // Navigation
    public virtual IndustrySector? ParentSector { get; set; }
    public virtual ICollection<IndustrySector> SubSectors { get; set; } = new List<IndustrySector>();
    public virtual ICollection<SectorComplianceRule> ComplianceRules { get; set; } = new List<SectorComplianceRule>();
}
