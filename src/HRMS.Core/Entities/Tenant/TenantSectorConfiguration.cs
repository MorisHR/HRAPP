using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Tenant's selected industry sector
/// Stored in Tenant Schema, references Master.IndustrySectors
/// </summary>
public class TenantSectorConfiguration : BaseEntity
{
    public int SectorId { get; set; }  // References master.IndustrySectors.Id

    public DateTime SelectedAt { get; set; } = DateTime.UtcNow;
    public Guid? SelectedByUserId { get; set; }
    public string? Notes { get; set; }

    // Additional metadata
    public string? SectorName { get; set; }  // Cached for quick access
    public string? SectorCode { get; set; }   // Cached for quick access
}
