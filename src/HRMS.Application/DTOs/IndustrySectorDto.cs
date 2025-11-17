using System.Text.Json.Serialization;

namespace HRMS.Application.DTOs;

/// <summary>
/// FORTUNE 500 PATTERN: Minimal payload DTO for industry sectors dropdown
/// Used for: Frontend dropdowns, minimal data transfer
/// Size: ~100-150 bytes per sector (compressed: ~40 bytes)
/// Total: 52 sectors × 40 bytes = ~2 KB compressed
/// </summary>
public class IndustrySectorDto
{
    /// <summary>
    /// Sector unique identifier
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Sector code (e.g., "CAT", "BPO", "CONST")
    /// Used for display and filtering
    /// </summary>
    [JsonPropertyName("code")]
    public string SectorCode { get; set; } = string.Empty;

    /// <summary>
    /// Sector name (English)
    /// Primary display name
    /// </summary>
    [JsonPropertyName("name")]
    public string SectorName { get; set; } = string.Empty;

    /// <summary>
    /// Sector name (French) - optional
    /// For bilingual support
    /// </summary>
    [JsonPropertyName("nameFrench")]
    public string? SectorNameFrench { get; set; }

    /// <summary>
    /// Whether sector is currently active
    /// Inactive sectors hidden from dropdowns
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}
