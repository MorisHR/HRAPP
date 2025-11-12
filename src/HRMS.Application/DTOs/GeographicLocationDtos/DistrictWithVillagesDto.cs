namespace HRMS.Application.DTOs.GeographicLocationDtos;

/// <summary>
/// District with its villages for cascading dropdown support
/// Used for address autocomplete and hierarchical address selection
/// </summary>
public class DistrictWithVillagesDto
{
    public int Id { get; set; }
    public string DistrictCode { get; set; } = string.Empty;
    public string DistrictName { get; set; } = string.Empty;
    public string? DistrictNameFrench { get; set; }
    public string Region { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>
    /// List of villages/towns/cities in this district
    /// </summary>
    public List<VillageDto> Villages { get; set; } = new List<VillageDto>();

    /// <summary>
    /// Number of villages in this district
    /// </summary>
    public int VillageCount => Villages.Count;
}
