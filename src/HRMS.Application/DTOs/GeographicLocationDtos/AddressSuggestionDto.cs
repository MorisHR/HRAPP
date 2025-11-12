namespace HRMS.Application.DTOs.GeographicLocationDtos;

/// <summary>
/// Address suggestion for autocomplete search
/// Combines district, village, and postal code information for comprehensive address search
/// </summary>
public class AddressSuggestionDto
{
    /// <summary>
    /// Suggested text to display in autocomplete dropdown
    /// Format: "Village, District PostalCode"
    /// Example: "Port Louis, Port Louis 11302"
    /// </summary>
    public string DisplayText { get; set; } = string.Empty;

    /// <summary>
    /// Village ID
    /// </summary>
    public int? VillageId { get; set; }

    /// <summary>
    /// Village name
    /// </summary>
    public string? VillageName { get; set; }

    /// <summary>
    /// Village code
    /// </summary>
    public string? VillageCode { get; set; }

    /// <summary>
    /// District ID
    /// </summary>
    public int? DistrictId { get; set; }

    /// <summary>
    /// District name
    /// </summary>
    public string? DistrictName { get; set; }

    /// <summary>
    /// District code
    /// </summary>
    public string? DistrictCode { get; set; }

    /// <summary>
    /// Postal code
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Locality type (City, Town, Village)
    /// </summary>
    public string? LocalityType { get; set; }

    /// <summary>
    /// Region (North, South, East, West, Central)
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Match type: "Village", "District", "PostalCode" (for relevance sorting)
    /// </summary>
    public string MatchType { get; set; } = string.Empty;

    /// <summary>
    /// Relevance score for sorting (higher = more relevant)
    /// </summary>
    public int RelevanceScore { get; set; }
}
