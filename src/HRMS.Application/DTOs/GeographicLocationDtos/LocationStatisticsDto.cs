namespace HRMS.Application.DTOs.GeographicLocationDtos;

/// <summary>
/// Statistics about Mauritius geographic locations
/// Provides overview of districts, villages, and postal codes
/// </summary>
public class LocationStatisticsDto
{
    /// <summary>
    /// Total number of districts (should be 9 for Mauritius)
    /// </summary>
    public int TotalDistricts { get; set; }

    /// <summary>
    /// Number of active districts
    /// </summary>
    public int ActiveDistricts { get; set; }

    /// <summary>
    /// Total number of villages/towns/cities
    /// </summary>
    public int TotalVillages { get; set; }

    /// <summary>
    /// Number of active villages
    /// </summary>
    public int ActiveVillages { get; set; }

    /// <summary>
    /// Total number of postal codes
    /// </summary>
    public int TotalPostalCodes { get; set; }

    /// <summary>
    /// Number of active postal codes
    /// </summary>
    public int ActivePostalCodes { get; set; }

    /// <summary>
    /// Breakdown by region
    /// </summary>
    public Dictionary<string, int> VillagesByRegion { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// Breakdown by locality type
    /// </summary>
    public Dictionary<string, int> VillagesByType { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// District with most villages
    /// </summary>
    public string? MostPopulousDistrict { get; set; }

    /// <summary>
    /// Number of villages in most populous district
    /// </summary>
    public int MostPopulousDistrictCount { get; set; }
}
