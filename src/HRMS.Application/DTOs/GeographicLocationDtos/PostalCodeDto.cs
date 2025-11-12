namespace HRMS.Application.DTOs.GeographicLocationDtos;

/// <summary>
/// Postal code information for Mauritius
/// Optimized for fast postal code lookups with denormalized data
/// </summary>
public class PostalCodeDto
{
    public int Id { get; set; }

    /// <summary>
    /// Postal code (5-digit format)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Village/Town name (denormalized)
    /// </summary>
    public string VillageName { get; set; } = string.Empty;

    /// <summary>
    /// District name (denormalized)
    /// </summary>
    public string DistrictName { get; set; } = string.Empty;

    /// <summary>
    /// Region: North, South, East, West, Central
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to Village
    /// </summary>
    public int VillageId { get; set; }

    /// <summary>
    /// Foreign key to District
    /// </summary>
    public int DistrictId { get; set; }

    /// <summary>
    /// Locality type: City, Town, Village
    /// </summary>
    public string? LocalityType { get; set; }

    /// <summary>
    /// Is this the primary postal code for the village?
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Additional notes (e.g., "Industrial Zone", "Coastal Area")
    /// </summary>
    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Full formatted address
    /// </summary>
    public string FormattedAddress => $"{Code} - {VillageName}, {DistrictName}";
}
