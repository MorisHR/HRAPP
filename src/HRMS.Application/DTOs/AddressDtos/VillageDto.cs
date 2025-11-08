namespace HRMS.Application.DTOs.AddressDtos;

/// <summary>
/// Village/Town information for API responses
/// </summary>
public class VillageDto
{
    public int Id { get; set; }
    public string VillageCode { get; set; } = string.Empty;
    public string VillageName { get; set; } = string.Empty;
    public string? VillageNameFrench { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public int DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public string? Region { get; set; }
    public string? LocalityType { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
