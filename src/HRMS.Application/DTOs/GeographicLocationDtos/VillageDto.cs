namespace HRMS.Application.DTOs.GeographicLocationDtos;

/// <summary>
/// Village/Town/City information for Mauritius localities
/// Represents a populated place within a district
/// </summary>
public class VillageDto
{
    public int Id { get; set; }

    /// <summary>
    /// Village/Town code (e.g., "PLOU" for Port Louis, "CURE" for Curepipe)
    /// </summary>
    public string VillageCode { get; set; } = string.Empty;

    /// <summary>
    /// Official village/town name in English
    /// </summary>
    public string VillageName { get; set; } = string.Empty;

    /// <summary>
    /// Village name in French (official Mauritius language)
    /// </summary>
    public string? VillageNameFrench { get; set; }

    /// <summary>
    /// Primary postal code for this village
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// District ID (foreign key)
    /// </summary>
    public int DistrictId { get; set; }

    /// <summary>
    /// District name (denormalized for quick display)
    /// </summary>
    public string? DistrictName { get; set; }

    /// <summary>
    /// District code (denormalized for quick display)
    /// </summary>
    public string? DistrictCode { get; set; }

    /// <summary>
    /// Locality type: City, Town, Village, Hamlet, Suburb
    /// </summary>
    public string? LocalityType { get; set; }

    /// <summary>
    /// Latitude for mapping (optional)
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Longitude for mapping (optional)
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Display order within district
    /// </summary>
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Full address string for display (Village, District, Postal Code)
    /// </summary>
    public string FullAddress => $"{VillageName}, {DistrictName} {PostalCode}";
}
