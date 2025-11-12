using HRMS.Application.DTOs.GeographicLocationDtos;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service for managing Mauritius geographic locations (Districts, Villages, Postal Codes)
/// Provides reference data for address autocomplete, dropdowns, and validation
/// </summary>
public interface IGeographicLocationService
{
    // ============================================
    // DISTRICTS
    // ============================================

    /// <summary>
    /// Get all Mauritius districts (9 districts)
    /// </summary>
    /// <param name="activeOnly">Include only active districts (default: true)</param>
    /// <returns>List of districts ordered by DisplayOrder</returns>
    Task<List<DistrictDto>> GetAllDistrictsAsync(bool activeOnly = true);

    /// <summary>
    /// Get district by ID
    /// </summary>
    Task<DistrictDto?> GetDistrictByIdAsync(int districtId);

    /// <summary>
    /// Get district by code (e.g., "PL" for Port Louis)
    /// </summary>
    Task<DistrictDto?> GetDistrictByCodeAsync(string districtCode);

    /// <summary>
    /// Get districts by region (North, South, East, West, Central)
    /// </summary>
    Task<List<DistrictDto>> GetDistrictsByRegionAsync(string region);

    // ============================================
    // VILLAGES (Cities, Towns, Villages)
    // ============================================

    /// <summary>
    /// Get all villages/towns/cities across Mauritius
    /// </summary>
    /// <param name="activeOnly">Include only active locations (default: true)</param>
    /// <returns>List of villages ordered by DisplayOrder</returns>
    Task<List<VillageDto>> GetAllVillagesAsync(bool activeOnly = true);

    /// <summary>
    /// Get village by ID
    /// </summary>
    Task<VillageDto?> GetVillageByIdAsync(int villageId);

    /// <summary>
    /// Get village by code (e.g., "PLOU" for Port Louis)
    /// </summary>
    Task<VillageDto?> GetVillageByCodeAsync(string villageCode);

    /// <summary>
    /// Get all villages/towns/cities in a specific district
    /// </summary>
    /// <param name="districtId">District ID</param>
    /// <param name="activeOnly">Include only active locations (default: true)</param>
    /// <returns>List of villages in the district ordered by DisplayOrder</returns>
    Task<List<VillageDto>> GetVillagesByDistrictIdAsync(int districtId, bool activeOnly = true);

    /// <summary>
    /// Get villages by district code (e.g., "PL" for Port Louis)
    /// </summary>
    Task<List<VillageDto>> GetVillagesByDistrictCodeAsync(string districtCode, bool activeOnly = true);

    /// <summary>
    /// Get villages by locality type (City, Town, Village)
    /// </summary>
    /// <param name="localityType">Type of locality (City, Town, Village, Suburb, Other)</param>
    /// <param name="activeOnly">Include only active locations (default: true)</param>
    /// <returns>List of villages matching the type</returns>
    Task<List<VillageDto>> GetVillagesByLocalityTypeAsync(string localityType, bool activeOnly = true);

    /// <summary>
    /// Search villages by name (fuzzy search for autocomplete)
    /// </summary>
    /// <param name="searchTerm">Search term (partial name matching)</param>
    /// <param name="maxResults">Maximum number of results (default: 20)</param>
    /// <returns>List of matching villages ordered by relevance</returns>
    Task<List<VillageDto>> SearchVillagesAsync(string searchTerm, int maxResults = 20);

    // ============================================
    // POSTAL CODES
    // ============================================

    /// <summary>
    /// Get all postal codes across Mauritius
    /// </summary>
    /// <param name="activeOnly">Include only active postal codes (default: true)</param>
    /// <returns>List of postal codes</returns>
    Task<List<PostalCodeDto>> GetAllPostalCodesAsync(bool activeOnly = true);

    /// <summary>
    /// Get postal code by code (e.g., "11302" for Port Louis)
    /// </summary>
    Task<PostalCodeDto?> GetPostalCodeByCodeAsync(string code);

    /// <summary>
    /// Get postal codes by village ID
    /// </summary>
    Task<List<PostalCodeDto>> GetPostalCodesByVillageIdAsync(int villageId, bool activeOnly = true);

    /// <summary>
    /// Get postal codes by district ID
    /// </summary>
    Task<List<PostalCodeDto>> GetPostalCodesByDistrictIdAsync(int districtId, bool activeOnly = true);

    /// <summary>
    /// Search postal codes by partial code or village name
    /// </summary>
    /// <param name="searchTerm">Search term (partial postal code or village name)</param>
    /// <param name="maxResults">Maximum number of results (default: 20)</param>
    /// <returns>List of matching postal codes</returns>
    Task<List<PostalCodeDto>> SearchPostalCodesAsync(string searchTerm, int maxResults = 20);

    // ============================================
    // ADDRESS AUTOCOMPLETE & VALIDATION
    // ============================================

    /// <summary>
    /// Get complete address hierarchy for autocomplete dropdowns
    /// Returns districts with their villages grouped for cascading dropdowns
    /// </summary>
    Task<List<DistrictWithVillagesDto>> GetAddressHierarchyAsync();

    /// <summary>
    /// Validate a Mauritius address (district, village, postal code)
    /// </summary>
    /// <param name="districtCode">District code</param>
    /// <param name="villageCode">Village code</param>
    /// <param name="postalCode">Postal code</param>
    /// <returns>Validation result with error messages if invalid</returns>
    Task<AddressValidationResult> ValidateAddressAsync(string? districtCode, string? villageCode, string? postalCode);

    /// <summary>
    /// Get address suggestions for autocomplete
    /// Combines district, village, and postal code for comprehensive search
    /// </summary>
    /// <param name="searchTerm">Search term (can match district, village, or postal code)</param>
    /// <param name="maxResults">Maximum number of results (default: 10)</param>
    /// <returns>List of address suggestions</returns>
    Task<List<AddressSuggestionDto>> GetAddressSuggestionsAsync(string searchTerm, int maxResults = 10);

    // ============================================
    // STATISTICS & ANALYTICS
    // ============================================

    /// <summary>
    /// Get location statistics (total districts, villages, postal codes)
    /// </summary>
    Task<LocationStatisticsDto> GetLocationStatisticsAsync();
}
