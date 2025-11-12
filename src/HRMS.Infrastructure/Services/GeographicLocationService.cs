using HRMS.Application.DTOs.GeographicLocationDtos;
using HRMS.Application.Interfaces;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for managing Mauritius geographic locations
/// Provides reference data for districts, villages, and postal codes
/// </summary>
public class GeographicLocationService : IGeographicLocationService
{
    private readonly MasterDbContext _context;

    public GeographicLocationService(MasterDbContext context)
    {
        _context = context;
    }

    // ============================================
    // DISTRICTS
    // ============================================

    public async Task<List<DistrictDto>> GetAllDistrictsAsync(bool activeOnly = true)
    {
        var query = _context.Districts.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(d => d.IsActive);
        }

        var districts = await query
            .OrderBy(d => d.DisplayOrder)
            .ToListAsync();

        var result = new List<DistrictDto>();

        foreach (var district in districts)
        {
            var villageCount = await _context.Villages
                .CountAsync(v => v.DistrictId == district.Id && !v.IsDeleted);

            result.Add(new DistrictDto
            {
                Id = district.Id,
                DistrictCode = district.DistrictCode,
                DistrictName = district.DistrictName,
                DistrictNameFrench = district.DistrictNameFrench,
                Region = district.Region,
                AreaSqKm = district.AreaSqKm,
                Population = district.Population,
                DisplayOrder = district.DisplayOrder,
                VillageCount = villageCount,
                IsActive = district.IsActive
            });
        }

        return result;
    }

    public async Task<DistrictDto?> GetDistrictByIdAsync(int districtId)
    {
        var district = await _context.Districts
            .FirstOrDefaultAsync(d => d.Id == districtId);

        if (district == null)
        {
            return null;
        }

        var villageCount = await _context.Villages
            .CountAsync(v => v.DistrictId == district.Id && !v.IsDeleted);

        return new DistrictDto
        {
            Id = district.Id,
            DistrictCode = district.DistrictCode,
            DistrictName = district.DistrictName,
            DistrictNameFrench = district.DistrictNameFrench,
            Region = district.Region,
            AreaSqKm = district.AreaSqKm,
            Population = district.Population,
            DisplayOrder = district.DisplayOrder,
            VillageCount = villageCount,
            IsActive = district.IsActive
        };
    }

    public async Task<DistrictDto?> GetDistrictByCodeAsync(string districtCode)
    {
        var district = await _context.Districts
            .FirstOrDefaultAsync(d => d.DistrictCode == districtCode);

        if (district == null)
        {
            return null;
        }

        var villageCount = await _context.Villages
            .CountAsync(v => v.DistrictId == district.Id && !v.IsDeleted);

        return new DistrictDto
        {
            Id = district.Id,
            DistrictCode = district.DistrictCode,
            DistrictName = district.DistrictName,
            DistrictNameFrench = district.DistrictNameFrench,
            Region = district.Region,
            AreaSqKm = district.AreaSqKm,
            Population = district.Population,
            DisplayOrder = district.DisplayOrder,
            VillageCount = villageCount,
            IsActive = district.IsActive
        };
    }

    public async Task<List<DistrictDto>> GetDistrictsByRegionAsync(string region)
    {
        var districts = await _context.Districts
            .Where(d => d.Region == region && d.IsActive)
            .OrderBy(d => d.DisplayOrder)
            .ToListAsync();

        var result = new List<DistrictDto>();

        foreach (var district in districts)
        {
            var villageCount = await _context.Villages
                .CountAsync(v => v.DistrictId == district.Id && !v.IsDeleted);

            result.Add(new DistrictDto
            {
                Id = district.Id,
                DistrictCode = district.DistrictCode,
                DistrictName = district.DistrictName,
                DistrictNameFrench = district.DistrictNameFrench,
                Region = district.Region,
                AreaSqKm = district.AreaSqKm,
                Population = district.Population,
                DisplayOrder = district.DisplayOrder,
                VillageCount = villageCount,
                IsActive = district.IsActive
            });
        }

        return result;
    }

    // ============================================
    // VILLAGES (Cities, Towns, Villages)
    // ============================================

    public async Task<List<VillageDto>> GetAllVillagesAsync(bool activeOnly = true)
    {
        var query = _context.Villages
            .Include(v => v.District)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(v => v.IsActive);
        }

        var villages = await query
            .OrderBy(v => v.DisplayOrder)
            .ToListAsync();

        return villages.Select(v => new VillageDto
        {
            Id = v.Id,
            VillageCode = v.VillageCode,
            VillageName = v.VillageName,
            VillageNameFrench = v.VillageNameFrench,
            PostalCode = v.PostalCode,
            DistrictId = v.DistrictId,
            DistrictName = v.District?.DistrictName,
            DistrictCode = v.District?.DistrictCode,
            LocalityType = v.LocalityType,
            Latitude = v.Latitude,
            Longitude = v.Longitude,
            DisplayOrder = v.DisplayOrder,
            IsActive = v.IsActive
        }).ToList();
    }

    public async Task<VillageDto?> GetVillageByIdAsync(int villageId)
    {
        var village = await _context.Villages
            .Include(v => v.District)
            .FirstOrDefaultAsync(v => v.Id == villageId);

        if (village == null)
        {
            return null;
        }

        return new VillageDto
        {
            Id = village.Id,
            VillageCode = village.VillageCode,
            VillageName = village.VillageName,
            VillageNameFrench = village.VillageNameFrench,
            PostalCode = village.PostalCode,
            DistrictId = village.DistrictId,
            DistrictName = village.District?.DistrictName,
            DistrictCode = village.District?.DistrictCode,
            LocalityType = village.LocalityType,
            Latitude = village.Latitude,
            Longitude = village.Longitude,
            DisplayOrder = village.DisplayOrder,
            IsActive = village.IsActive
        };
    }

    public async Task<VillageDto?> GetVillageByCodeAsync(string villageCode)
    {
        var village = await _context.Villages
            .Include(v => v.District)
            .FirstOrDefaultAsync(v => v.VillageCode == villageCode);

        if (village == null)
        {
            return null;
        }

        return new VillageDto
        {
            Id = village.Id,
            VillageCode = village.VillageCode,
            VillageName = village.VillageName,
            VillageNameFrench = village.VillageNameFrench,
            PostalCode = village.PostalCode,
            DistrictId = village.DistrictId,
            DistrictName = village.District?.DistrictName,
            DistrictCode = village.District?.DistrictCode,
            LocalityType = village.LocalityType,
            Latitude = village.Latitude,
            Longitude = village.Longitude,
            DisplayOrder = village.DisplayOrder,
            IsActive = village.IsActive
        };
    }

    public async Task<List<VillageDto>> GetVillagesByDistrictIdAsync(int districtId, bool activeOnly = true)
    {
        var query = _context.Villages
            .Include(v => v.District)
            .Where(v => v.DistrictId == districtId);

        if (activeOnly)
        {
            query = query.Where(v => v.IsActive);
        }

        var villages = await query
            .OrderBy(v => v.DisplayOrder)
            .ToListAsync();

        return villages.Select(v => new VillageDto
        {
            Id = v.Id,
            VillageCode = v.VillageCode,
            VillageName = v.VillageName,
            VillageNameFrench = v.VillageNameFrench,
            PostalCode = v.PostalCode,
            DistrictId = v.DistrictId,
            DistrictName = v.District?.DistrictName,
            DistrictCode = v.District?.DistrictCode,
            LocalityType = v.LocalityType,
            Latitude = v.Latitude,
            Longitude = v.Longitude,
            DisplayOrder = v.DisplayOrder,
            IsActive = v.IsActive
        }).ToList();
    }

    public async Task<List<VillageDto>> GetVillagesByDistrictCodeAsync(string districtCode, bool activeOnly = true)
    {
        var district = await _context.Districts
            .FirstOrDefaultAsync(d => d.DistrictCode == districtCode);

        if (district == null)
        {
            return new List<VillageDto>();
        }

        return await GetVillagesByDistrictIdAsync(district.Id, activeOnly);
    }

    public async Task<List<VillageDto>> GetVillagesByLocalityTypeAsync(string localityType, bool activeOnly = true)
    {
        var query = _context.Villages
            .Include(v => v.District)
            .Where(v => v.LocalityType == localityType);

        if (activeOnly)
        {
            query = query.Where(v => v.IsActive);
        }

        var villages = await query
            .OrderBy(v => v.DisplayOrder)
            .ToListAsync();

        return villages.Select(v => new VillageDto
        {
            Id = v.Id,
            VillageCode = v.VillageCode,
            VillageName = v.VillageName,
            VillageNameFrench = v.VillageNameFrench,
            PostalCode = v.PostalCode,
            DistrictId = v.DistrictId,
            DistrictName = v.District?.DistrictName,
            DistrictCode = v.District?.DistrictCode,
            LocalityType = v.LocalityType,
            Latitude = v.Latitude,
            Longitude = v.Longitude,
            DisplayOrder = v.DisplayOrder,
            IsActive = v.IsActive
        }).ToList();
    }

    public async Task<List<VillageDto>> SearchVillagesAsync(string searchTerm, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<VillageDto>();
        }

        var searchLower = searchTerm.ToLower();

        var villages = await _context.Villages
            .Include(v => v.District)
            .Where(v => v.IsActive && (
                v.VillageName.ToLower().Contains(searchLower) ||
                (v.VillageNameFrench != null && v.VillageNameFrench.ToLower().Contains(searchLower)) ||
                v.VillageCode.ToLower().Contains(searchLower) ||
                v.PostalCode.Contains(searchTerm)
            ))
            .OrderBy(v => v.VillageName.ToLower().StartsWith(searchLower) ? 0 : 1) // Exact prefix matches first
            .ThenBy(v => v.VillageName)
            .Take(maxResults)
            .ToListAsync();

        return villages.Select(v => new VillageDto
        {
            Id = v.Id,
            VillageCode = v.VillageCode,
            VillageName = v.VillageName,
            VillageNameFrench = v.VillageNameFrench,
            PostalCode = v.PostalCode,
            DistrictId = v.DistrictId,
            DistrictName = v.District?.DistrictName,
            DistrictCode = v.District?.DistrictCode,
            LocalityType = v.LocalityType,
            Latitude = v.Latitude,
            Longitude = v.Longitude,
            DisplayOrder = v.DisplayOrder,
            IsActive = v.IsActive
        }).ToList();
    }

    // ============================================
    // POSTAL CODES
    // ============================================

    public async Task<List<PostalCodeDto>> GetAllPostalCodesAsync(bool activeOnly = true)
    {
        var query = _context.PostalCodes.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(pc => pc.IsActive);
        }

        var postalCodes = await query
            .OrderBy(pc => pc.Code)
            .ToListAsync();

        return postalCodes.Select(pc => new PostalCodeDto
        {
            Id = pc.Id,
            Code = pc.Code,
            VillageName = pc.VillageName,
            DistrictName = pc.DistrictName,
            Region = pc.Region,
            VillageId = pc.VillageId,
            DistrictId = pc.DistrictId,
            LocalityType = pc.LocalityType,
            IsPrimary = pc.IsPrimary,
            Notes = pc.Notes,
            IsActive = pc.IsActive
        }).ToList();
    }

    public async Task<PostalCodeDto?> GetPostalCodeByCodeAsync(string code)
    {
        var postalCode = await _context.PostalCodes
            .FirstOrDefaultAsync(pc => pc.Code == code);

        if (postalCode == null)
        {
            return null;
        }

        return new PostalCodeDto
        {
            Id = postalCode.Id,
            Code = postalCode.Code,
            VillageName = postalCode.VillageName,
            DistrictName = postalCode.DistrictName,
            Region = postalCode.Region,
            VillageId = postalCode.VillageId,
            DistrictId = postalCode.DistrictId,
            LocalityType = postalCode.LocalityType,
            IsPrimary = postalCode.IsPrimary,
            Notes = postalCode.Notes,
            IsActive = postalCode.IsActive
        };
    }

    public async Task<List<PostalCodeDto>> GetPostalCodesByVillageIdAsync(int villageId, bool activeOnly = true)
    {
        var query = _context.PostalCodes
            .Where(pc => pc.VillageId == villageId);

        if (activeOnly)
        {
            query = query.Where(pc => pc.IsActive);
        }

        var postalCodes = await query
            .OrderByDescending(pc => pc.IsPrimary)
            .ThenBy(pc => pc.Code)
            .ToListAsync();

        return postalCodes.Select(pc => new PostalCodeDto
        {
            Id = pc.Id,
            Code = pc.Code,
            VillageName = pc.VillageName,
            DistrictName = pc.DistrictName,
            Region = pc.Region,
            VillageId = pc.VillageId,
            DistrictId = pc.DistrictId,
            LocalityType = pc.LocalityType,
            IsPrimary = pc.IsPrimary,
            Notes = pc.Notes,
            IsActive = pc.IsActive
        }).ToList();
    }

    public async Task<List<PostalCodeDto>> GetPostalCodesByDistrictIdAsync(int districtId, bool activeOnly = true)
    {
        var query = _context.PostalCodes
            .Where(pc => pc.DistrictId == districtId);

        if (activeOnly)
        {
            query = query.Where(pc => pc.IsActive);
        }

        var postalCodes = await query
            .OrderBy(pc => pc.Code)
            .ToListAsync();

        return postalCodes.Select(pc => new PostalCodeDto
        {
            Id = pc.Id,
            Code = pc.Code,
            VillageName = pc.VillageName,
            DistrictName = pc.DistrictName,
            Region = pc.Region,
            VillageId = pc.VillageId,
            DistrictId = pc.DistrictId,
            LocalityType = pc.LocalityType,
            IsPrimary = pc.IsPrimary,
            Notes = pc.Notes,
            IsActive = pc.IsActive
        }).ToList();
    }

    public async Task<List<PostalCodeDto>> SearchPostalCodesAsync(string searchTerm, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<PostalCodeDto>();
        }

        var searchLower = searchTerm.ToLower();

        var postalCodes = await _context.PostalCodes
            .Where(pc => pc.IsActive && (
                pc.Code.Contains(searchTerm) ||
                pc.VillageName.ToLower().Contains(searchLower) ||
                pc.DistrictName.ToLower().Contains(searchLower)
            ))
            .OrderBy(pc => pc.Code.StartsWith(searchTerm) ? 0 : 1) // Exact prefix matches first
            .ThenBy(pc => pc.Code)
            .Take(maxResults)
            .ToListAsync();

        return postalCodes.Select(pc => new PostalCodeDto
        {
            Id = pc.Id,
            Code = pc.Code,
            VillageName = pc.VillageName,
            DistrictName = pc.DistrictName,
            Region = pc.Region,
            VillageId = pc.VillageId,
            DistrictId = pc.DistrictId,
            LocalityType = pc.LocalityType,
            IsPrimary = pc.IsPrimary,
            Notes = pc.Notes,
            IsActive = pc.IsActive
        }).ToList();
    }

    // ============================================
    // ADDRESS AUTOCOMPLETE & VALIDATION
    // ============================================

    public async Task<List<DistrictWithVillagesDto>> GetAddressHierarchyAsync()
    {
        var districts = await _context.Districts
            .Where(d => d.IsActive)
            .OrderBy(d => d.DisplayOrder)
            .ToListAsync();

        var result = new List<DistrictWithVillagesDto>();

        foreach (var district in districts)
        {
            var villages = await GetVillagesByDistrictIdAsync(district.Id, activeOnly: true);

            result.Add(new DistrictWithVillagesDto
            {
                Id = district.Id,
                DistrictCode = district.DistrictCode,
                DistrictName = district.DistrictName,
                DistrictNameFrench = district.DistrictNameFrench,
                Region = district.Region,
                IsActive = district.IsActive,
                Villages = villages
            });
        }

        return result;
    }

    public async Task<AddressValidationResult> ValidateAddressAsync(string? districtCode, string? villageCode, string? postalCode)
    {
        var result = new AddressValidationResult();

        // Validate District
        DistrictDto? district = null;
        if (!string.IsNullOrWhiteSpace(districtCode))
        {
            district = await GetDistrictByCodeAsync(districtCode);
            if (district == null)
            {
                result.AddError($"Invalid district code: {districtCode}");
                result.AddSuggestion("Please select a valid Mauritius district (PL, PW, FL, etc.)");
            }
            else
            {
                result.District = district;
            }
        }

        // Validate Village
        VillageDto? village = null;
        if (!string.IsNullOrWhiteSpace(villageCode))
        {
            village = await GetVillageByCodeAsync(villageCode);
            if (village == null)
            {
                result.AddError($"Invalid village code: {villageCode}");
                result.AddSuggestion("Please select a valid village/town from the list");
            }
            else
            {
                result.Village = village;

                // Check if village belongs to district
                if (district != null && village.DistrictId != district.Id)
                {
                    result.AddError($"Village '{village.VillageName}' does not belong to district '{district.DistrictName}'");
                    result.AddSuggestion($"Village '{village.VillageName}' belongs to '{village.DistrictName}' district");
                }
            }
        }

        // Validate Postal Code
        PostalCodeDto? postal = null;
        if (!string.IsNullOrWhiteSpace(postalCode))
        {
            postal = await GetPostalCodeByCodeAsync(postalCode);
            if (postal == null)
            {
                result.AddError($"Invalid postal code: {postalCode}");
                result.AddSuggestion("Please enter a valid 5-digit Mauritius postal code");
            }
            else
            {
                result.PostalCode = postal;

                // Check if postal code matches village
                if (village != null && postal.VillageId != village.Id)
                {
                    result.AddError($"Postal code '{postalCode}' does not match village '{village.VillageName}'");
                    result.AddSuggestion($"Postal code '{postalCode}' belongs to '{postal.VillageName}'");
                }

                // Check if postal code matches district
                if (district != null && postal.DistrictId != district.Id)
                {
                    result.AddError($"Postal code '{postalCode}' does not belong to district '{district.DistrictName}'");
                    result.AddSuggestion($"Postal code '{postalCode}' belongs to '{postal.DistrictName}' district");
                }
            }
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    public async Task<List<AddressSuggestionDto>> GetAddressSuggestionsAsync(string searchTerm, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<AddressSuggestionDto>();
        }

        var suggestions = new List<AddressSuggestionDto>();
        var searchLower = searchTerm.ToLower();

        // Search villages (highest priority)
        var villages = await _context.Villages
            .Include(v => v.District)
            .Where(v => v.IsActive && (
                v.VillageName.ToLower().Contains(searchLower) ||
                (v.VillageNameFrench != null && v.VillageNameFrench.ToLower().Contains(searchLower)) ||
                v.VillageCode.ToLower().Contains(searchLower)
            ))
            .Take(maxResults)
            .ToListAsync();

        foreach (var village in villages)
        {
            var relevanceScore = 100;
            if (village.VillageName.ToLower().StartsWith(searchLower))
            {
                relevanceScore = 200; // Exact prefix match
            }

            suggestions.Add(new AddressSuggestionDto
            {
                DisplayText = $"{village.VillageName}, {village.District?.DistrictName} {village.PostalCode}",
                VillageId = village.Id,
                VillageName = village.VillageName,
                VillageCode = village.VillageCode,
                DistrictId = village.DistrictId,
                DistrictName = village.District?.DistrictName,
                DistrictCode = village.District?.DistrictCode,
                PostalCode = village.PostalCode,
                LocalityType = village.LocalityType,
                Region = village.District?.Region,
                MatchType = "Village",
                RelevanceScore = relevanceScore
            });
        }

        // Search postal codes (if not enough results)
        if (suggestions.Count < maxResults && searchTerm.Length >= 3)
        {
            var postalCodes = await _context.PostalCodes
                .Where(pc => pc.IsActive && pc.Code.Contains(searchTerm))
                .Take(maxResults - suggestions.Count)
                .ToListAsync();

            foreach (var pc in postalCodes)
            {
                suggestions.Add(new AddressSuggestionDto
                {
                    DisplayText = $"{pc.Code} - {pc.VillageName}, {pc.DistrictName}",
                    VillageId = pc.VillageId,
                    VillageName = pc.VillageName,
                    DistrictId = pc.DistrictId,
                    DistrictName = pc.DistrictName,
                    PostalCode = pc.Code,
                    LocalityType = pc.LocalityType,
                    Region = pc.Region,
                    MatchType = "PostalCode",
                    RelevanceScore = 50
                });
            }
        }

        // Sort by relevance score (descending)
        return suggestions
            .OrderByDescending(s => s.RelevanceScore)
            .ThenBy(s => s.VillageName)
            .Take(maxResults)
            .ToList();
    }

    // ============================================
    // STATISTICS & ANALYTICS
    // ============================================

    public async Task<LocationStatisticsDto> GetLocationStatisticsAsync()
    {
        var totalDistricts = await _context.Districts.CountAsync(d => !d.IsDeleted);
        var activeDistricts = await _context.Districts.CountAsync(d => d.IsActive && !d.IsDeleted);

        var totalVillages = await _context.Villages.CountAsync(v => !v.IsDeleted);
        var activeVillages = await _context.Villages.CountAsync(v => v.IsActive && !v.IsDeleted);

        var totalPostalCodes = await _context.PostalCodes.CountAsync(pc => !pc.IsDeleted);
        var activePostalCodes = await _context.PostalCodes.CountAsync(pc => pc.IsActive && !pc.IsDeleted);

        // Villages by region
        var villagesByRegion = await _context.Villages
            .Include(v => v.District)
            .Where(v => v.IsActive && !v.IsDeleted)
            .GroupBy(v => v.District!.Region)
            .Select(g => new { Region = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Region, x => x.Count);

        // Villages by type
        var villagesByType = await _context.Villages
            .Where(v => v.IsActive && !v.IsDeleted && v.LocalityType != null)
            .GroupBy(v => v.LocalityType!)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        // Most populous district
        var mostPopulous = await _context.Villages
            .Include(v => v.District)
            .Where(v => v.IsActive && !v.IsDeleted)
            .GroupBy(v => new { v.District!.DistrictName, v.DistrictId })
            .Select(g => new { DistrictName = g.Key.DistrictName, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefaultAsync();

        return new LocationStatisticsDto
        {
            TotalDistricts = totalDistricts,
            ActiveDistricts = activeDistricts,
            TotalVillages = totalVillages,
            ActiveVillages = activeVillages,
            TotalPostalCodes = totalPostalCodes,
            ActivePostalCodes = activePostalCodes,
            VillagesByRegion = villagesByRegion,
            VillagesByType = villagesByType,
            MostPopulousDistrict = mostPopulous?.DistrictName,
            MostPopulousDistrictCount = mostPopulous?.Count ?? 0
        };
    }
}
