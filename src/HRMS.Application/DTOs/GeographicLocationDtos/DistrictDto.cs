namespace HRMS.Application.DTOs.GeographicLocationDtos;

/// <summary>
/// District information for Mauritius administrative divisions
/// Represents one of the 9 districts of Mauritius
/// </summary>
public class DistrictDto
{
    public int Id { get; set; }

    /// <summary>
    /// District code (e.g., "BL" for Black River, "PL" for Port Louis)
    /// </summary>
    public string DistrictCode { get; set; } = string.Empty;

    /// <summary>
    /// Official district name in English
    /// </summary>
    public string DistrictName { get; set; } = string.Empty;

    /// <summary>
    /// District name in French (official Mauritius language)
    /// </summary>
    public string? DistrictNameFrench { get; set; }

    /// <summary>
    /// Geographic region: North, South, East, West, Central
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Area in square kilometers
    /// </summary>
    public decimal? AreaSqKm { get; set; }

    /// <summary>
    /// Approximate population
    /// </summary>
    public int? Population { get; set; }

    /// <summary>
    /// Display order for dropdowns
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Number of villages/towns/cities in this district
    /// </summary>
    public int VillageCount { get; set; }

    public bool IsActive { get; set; }
}
