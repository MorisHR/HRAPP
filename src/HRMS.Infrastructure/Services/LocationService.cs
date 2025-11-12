using HRMS.Application.DTOs.LocationDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for managing physical work locations
/// </summary>
public class LocationService : ILocationService
{
    private readonly TenantDbContext _context;

    public LocationService(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateLocationAsync(CreateLocationDto dto, string createdBy)
    {
        // Check for duplicate location code
        var existingLocation = await _context.Locations
            .FirstOrDefaultAsync(l => l.LocationCode == dto.LocationCode && !l.IsDeleted);

        if (existingLocation != null)
        {
            throw new InvalidOperationException($"Location with code '{dto.LocationCode}' already exists");
        }

        var location = new Location
        {
            Id = Guid.NewGuid(),
            LocationCode = dto.LocationCode,
            LocationName = dto.LocationName,
            LocationType = dto.LocationType,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            City = dto.City,
            District = dto.District,
            Region = dto.Region,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            Phone = dto.Phone,
            Email = dto.Email,
            WorkingHoursJson = dto.WorkingHoursJson,
            Timezone = dto.Timezone,
            LocationManagerId = dto.LocationManagerId,
            CapacityHeadcount = dto.CapacityHeadcount,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        return location.Id;
    }

    public async Task UpdateLocationAsync(Guid id, UpdateLocationDto dto, string updatedBy)
    {
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (location == null)
        {
            throw new KeyNotFoundException($"Location with ID {id} not found");
        }

        // Check for duplicate location code if changed
        if (dto.LocationCode != location.LocationCode)
        {
            var existingLocation = await _context.Locations
                .FirstOrDefaultAsync(l => l.LocationCode == dto.LocationCode && l.Id != id && !l.IsDeleted);

            if (existingLocation != null)
            {
                throw new InvalidOperationException($"Location with code '{dto.LocationCode}' already exists");
            }
        }

        // Update fields
        location.LocationCode = dto.LocationCode;
        location.LocationName = dto.LocationName;
        location.LocationType = dto.LocationType;
        location.AddressLine1 = dto.AddressLine1;
        location.AddressLine2 = dto.AddressLine2;
        location.City = dto.City;
        location.District = dto.District;
        location.Region = dto.Region;
        location.PostalCode = dto.PostalCode;
        location.Country = dto.Country;
        location.Phone = dto.Phone;
        location.Email = dto.Email;
        location.WorkingHoursJson = dto.WorkingHoursJson;
        location.Timezone = dto.Timezone;
        location.LocationManagerId = dto.LocationManagerId;
        location.CapacityHeadcount = dto.CapacityHeadcount;
        location.Latitude = dto.Latitude;
        location.Longitude = dto.Longitude;
        location.IsActive = dto.IsActive;
        location.UpdatedAt = DateTime.UtcNow;
        location.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteLocationAsync(Guid id, string deletedBy)
    {
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (location == null)
        {
            throw new KeyNotFoundException($"Location with ID {id} not found");
        }

        // Check if location has active devices
        var hasDevices = await _context.AttendanceMachines
            .AnyAsync(d => d.LocationId == id && !d.IsDeleted);

        if (hasDevices)
        {
            throw new InvalidOperationException("Cannot delete location that has biometric devices assigned. Please reassign or delete devices first.");
        }

        // Check if location has active employees
        var hasEmployees = await _context.Employees
            .AnyAsync(e => e.PrimaryLocationId == id && !e.IsDeleted);

        if (hasEmployees)
        {
            throw new InvalidOperationException("Cannot delete location that has employees assigned. Please reassign employees first.");
        }

        // Soft delete
        location.IsDeleted = true;
        location.DeletedAt = DateTime.UtcNow;
        location.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();
    }

    public async Task<LocationDto?> GetLocationByIdAsync(Guid id)
    {
        var location = await _context.Locations
            .Include(l => l.LocationManager)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (location == null)
        {
            return null;
        }

        var deviceCount = await GetDeviceCountByLocationAsync(id);
        var employeeCount = await GetEmployeeCountByLocationAsync(id);

        return new LocationDto
        {
            Id = location.Id,
            LocationCode = location.LocationCode,
            LocationName = location.LocationName,
            LocationType = location.LocationType,
            AddressLine1 = location.AddressLine1,
            AddressLine2 = location.AddressLine2,
            City = location.City,
            District = location.District,
            Region = location.Region,
            PostalCode = location.PostalCode,
            Country = location.Country,
            Phone = location.Phone,
            Email = location.Email,
            WorkingHoursJson = location.WorkingHoursJson,
            Timezone = location.Timezone,
            LocationManagerId = location.LocationManagerId,
            LocationManagerName = location.LocationManager != null ? location.LocationManager.FullName : null,
            CapacityHeadcount = location.CapacityHeadcount,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            IsActive = location.IsActive,
            DeviceCount = deviceCount,
            EmployeeCount = employeeCount,
            CreatedAt = location.CreatedAt,
            UpdatedAt = location.UpdatedAt,
            CreatedBy = location.CreatedBy,
            UpdatedBy = location.UpdatedBy
        };
    }

    public async Task<LocationDto?> GetLocationByCodeAsync(string locationCode)
    {
        var location = await _context.Locations
            .Include(l => l.LocationManager)
            .FirstOrDefaultAsync(l => l.LocationCode == locationCode && !l.IsDeleted);

        if (location == null)
        {
            return null;
        }

        var deviceCount = await GetDeviceCountByLocationAsync(location.Id);
        var employeeCount = await GetEmployeeCountByLocationAsync(location.Id);

        return new LocationDto
        {
            Id = location.Id,
            LocationCode = location.LocationCode,
            LocationName = location.LocationName,
            LocationType = location.LocationType,
            AddressLine1 = location.AddressLine1,
            AddressLine2 = location.AddressLine2,
            City = location.City,
            District = location.District,
            Region = location.Region,
            PostalCode = location.PostalCode,
            Country = location.Country,
            Phone = location.Phone,
            Email = location.Email,
            WorkingHoursJson = location.WorkingHoursJson,
            Timezone = location.Timezone,
            LocationManagerId = location.LocationManagerId,
            LocationManagerName = location.LocationManager != null ? location.LocationManager.FullName : null,
            CapacityHeadcount = location.CapacityHeadcount,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            IsActive = location.IsActive,
            DeviceCount = deviceCount,
            EmployeeCount = employeeCount,
            CreatedAt = location.CreatedAt,
            UpdatedAt = location.UpdatedAt,
            CreatedBy = location.CreatedBy,
            UpdatedBy = location.UpdatedBy
        };
    }

    public async Task<List<LocationSummaryDto>> GetAllLocationsAsync(bool activeOnly = true)
    {
        var query = _context.Locations
            .Include(l => l.LocationManager)
            .Where(l => !l.IsDeleted);

        if (activeOnly)
        {
            query = query.Where(l => l.IsActive);
        }

        var locations = await query
            .OrderBy(l => l.LocationName)
            .ToListAsync();

        var result = new List<LocationSummaryDto>();

        foreach (var location in locations)
        {
            var deviceCount = await GetDeviceCountByLocationAsync(location.Id);
            var employeeCount = await GetEmployeeCountByLocationAsync(location.Id);

            result.Add(new LocationSummaryDto
            {
                Id = location.Id,
                LocationCode = location.LocationCode,
                LocationName = location.LocationName,
                LocationType = location.LocationType,
                City = location.City,
                District = location.District,
                Region = location.Region,
                Country = location.Country,
                Phone = location.Phone,
                Email = location.Email,
                LocationManagerId = location.LocationManagerId,
                LocationManagerName = location.LocationManager != null ? location.LocationManager.FullName : null,
                CapacityHeadcount = location.CapacityHeadcount,
                DeviceCount = deviceCount,
                EmployeeCount = employeeCount,
                IsActive = location.IsActive,
                CreatedAt = location.CreatedAt,
                UpdatedAt = location.UpdatedAt
            });
        }

        return result;
    }

    public async Task<List<LocationDropdownDto>> GetLocationsForDropdownAsync(bool activeOnly = true)
    {
        var query = _context.Locations
            .Where(l => !l.IsDeleted);

        if (activeOnly)
        {
            query = query.Where(l => l.IsActive);
        }

        return await query
            .OrderBy(l => l.LocationName)
            .Select(l => new LocationDropdownDto
            {
                Id = l.Id,
                LocationCode = l.LocationCode,
                LocationName = l.LocationName,
                LocationType = l.LocationType,
                IsActive = l.IsActive
            })
            .ToListAsync();
    }

    public async Task<int> GetDeviceCountByLocationAsync(Guid locationId)
    {
        return await _context.AttendanceMachines
            .CountAsync(d => d.LocationId == locationId && !d.IsDeleted);
    }

    public async Task<int> GetEmployeeCountByLocationAsync(Guid locationId)
    {
        return await _context.Employees
            .CountAsync(e => e.PrimaryLocationId == locationId && !e.IsDeleted);
    }

    public async Task<(List<LocationSummaryDto> Locations, int TotalCount)> GetLocationsWithFilterAsync(LocationFilterDto filter)
    {
        var query = _context.Locations
            .Include(l => l.LocationManager)
            .Where(l => !l.IsDeleted);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.District))
        {
            query = query.Where(l => l.District == filter.District);
        }

        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            query = query.Where(l => l.LocationType == filter.Type);
        }

        if (!string.IsNullOrWhiteSpace(filter.Region))
        {
            query = query.Where(l => l.Region == filter.Region);
        }

        if (!string.IsNullOrWhiteSpace(filter.PostalCode))
        {
            query = query.Where(l => l.PostalCode == filter.PostalCode);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(l => l.IsActive == filter.IsActive.Value);
        }

        if (filter.LocationManagerId.HasValue)
        {
            query = query.Where(l => l.LocationManagerId == filter.LocationManagerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(l =>
                l.LocationName.ToLower().Contains(searchTerm) ||
                l.LocationCode.ToLower().Contains(searchTerm) ||
                (l.City != null && l.City.ToLower().Contains(searchTerm)) ||
                (l.District != null && l.District.ToLower().Contains(searchTerm))
            );
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        var sortBy = filter.SortBy?.ToLower() ?? "locationname";
        var sortDirection = filter.SortDirection?.ToLower() ?? "asc";

        query = sortBy switch
        {
            "locationcode" => sortDirection == "desc"
                ? query.OrderByDescending(l => l.LocationCode)
                : query.OrderBy(l => l.LocationCode),
            "district" => sortDirection == "desc"
                ? query.OrderByDescending(l => l.District)
                : query.OrderBy(l => l.District),
            "city" => sortDirection == "desc"
                ? query.OrderByDescending(l => l.City)
                : query.OrderBy(l => l.City),
            "createdat" => sortDirection == "desc"
                ? query.OrderByDescending(l => l.CreatedAt)
                : query.OrderBy(l => l.CreatedAt),
            _ => sortDirection == "desc"
                ? query.OrderByDescending(l => l.LocationName)
                : query.OrderBy(l => l.LocationName)
        };

        // Apply pagination
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;
        var skip = (page - 1) * pageSize;

        var locations = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<LocationSummaryDto>();

        foreach (var location in locations)
        {
            var deviceCount = await GetDeviceCountByLocationAsync(location.Id);
            var employeeCount = await GetEmployeeCountByLocationAsync(location.Id);

            result.Add(new LocationSummaryDto
            {
                Id = location.Id,
                LocationCode = location.LocationCode,
                LocationName = location.LocationName,
                LocationType = location.LocationType,
                City = location.City,
                District = location.District,
                Region = location.Region,
                Country = location.Country,
                Phone = location.Phone,
                Email = location.Email,
                LocationManagerId = location.LocationManagerId,
                LocationManagerName = location.LocationManager != null ? location.LocationManager.FullName : null,
                CapacityHeadcount = location.CapacityHeadcount,
                DeviceCount = deviceCount,
                EmployeeCount = employeeCount,
                IsActive = location.IsActive,
                CreatedAt = location.CreatedAt,
                UpdatedAt = location.UpdatedAt
            });
        }

        return (result, totalCount);
    }

    public async Task<List<string>> GetDistrictsAsync()
    {
        return await _context.Locations
            .Where(l => !l.IsDeleted && l.District != null)
            .Select(l => l.District!)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }

    public async Task<List<LocationSummaryDto>> GetLocationsByDistrictAsync(string district, bool activeOnly = true)
    {
        var query = _context.Locations
            .Include(l => l.LocationManager)
            .Where(l => !l.IsDeleted && l.District == district);

        if (activeOnly)
        {
            query = query.Where(l => l.IsActive);
        }

        var locations = await query
            .OrderBy(l => l.LocationName)
            .ToListAsync();

        var result = new List<LocationSummaryDto>();

        foreach (var location in locations)
        {
            var deviceCount = await GetDeviceCountByLocationAsync(location.Id);
            var employeeCount = await GetEmployeeCountByLocationAsync(location.Id);

            result.Add(new LocationSummaryDto
            {
                Id = location.Id,
                LocationCode = location.LocationCode,
                LocationName = location.LocationName,
                LocationType = location.LocationType,
                City = location.City,
                District = location.District,
                Region = location.Region,
                Country = location.Country,
                Phone = location.Phone,
                Email = location.Email,
                LocationManagerId = location.LocationManagerId,
                LocationManagerName = location.LocationManager != null ? location.LocationManager.FullName : null,
                CapacityHeadcount = location.CapacityHeadcount,
                DeviceCount = deviceCount,
                EmployeeCount = employeeCount,
                IsActive = location.IsActive,
                CreatedAt = location.CreatedAt,
                UpdatedAt = location.UpdatedAt
            });
        }

        return result;
    }

    public async Task<List<LocationSummaryDto>> SearchLocationsAsync(string searchTerm, bool activeOnly = true)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<LocationSummaryDto>();
        }

        var search = searchTerm.ToLower();
        var query = _context.Locations
            .Include(l => l.LocationManager)
            .Where(l => !l.IsDeleted &&
                (l.LocationName.ToLower().Contains(search) ||
                 l.LocationCode.ToLower().Contains(search) ||
                 (l.City != null && l.City.ToLower().Contains(search)) ||
                 (l.District != null && l.District.ToLower().Contains(search)) ||
                 (l.Region != null && l.Region.ToLower().Contains(search))));

        if (activeOnly)
        {
            query = query.Where(l => l.IsActive);
        }

        var locations = await query
            .OrderBy(l => l.LocationName)
            .Take(50) // Limit search results
            .ToListAsync();

        var result = new List<LocationSummaryDto>();

        foreach (var location in locations)
        {
            var deviceCount = await GetDeviceCountByLocationAsync(location.Id);
            var employeeCount = await GetEmployeeCountByLocationAsync(location.Id);

            result.Add(new LocationSummaryDto
            {
                Id = location.Id,
                LocationCode = location.LocationCode,
                LocationName = location.LocationName,
                LocationType = location.LocationType,
                City = location.City,
                District = location.District,
                Region = location.Region,
                Country = location.Country,
                Phone = location.Phone,
                Email = location.Email,
                LocationManagerId = location.LocationManagerId,
                LocationManagerName = location.LocationManager != null ? location.LocationManager.FullName : null,
                CapacityHeadcount = location.CapacityHeadcount,
                DeviceCount = deviceCount,
                EmployeeCount = employeeCount,
                IsActive = location.IsActive,
                CreatedAt = location.CreatedAt,
                UpdatedAt = location.UpdatedAt
            });
        }

        return result;
    }

    public async Task SeedMauritiusLocationsAsync(string createdBy)
    {
        // Check if locations already exist
        var existingLocationsCount = await _context.Locations.CountAsync(l => !l.IsDeleted);
        if (existingLocationsCount > 0)
        {
            throw new InvalidOperationException("Locations already exist. Cannot seed data.");
        }

        // Mauritius Districts with major cities/towns
        var mauritiusLocations = new[]
        {
            // Port Louis District
            new { Code = "PL-001", Name = "Port Louis", Type = "City", District = "Port Louis", Region = "North", PostalCode = "11101", Lat = -20.1609m, Lng = 57.5012m },
            new { Code = "PL-002", Name = "Baie du Tombeau", Type = "Village", District = "Port Louis", Region = "North", PostalCode = "11701", Lat = -20.1264m, Lng = 57.4897m },
            new { Code = "PL-003", Name = "Roche Bois", Type = "Village", District = "Port Louis", Region = "North", PostalCode = "11901", Lat = -20.1291m, Lng = 57.5113m },

            // Pamplemousses District
            new { Code = "PA-001", Name = "Pamplemousses", Type = "Village", District = "Pamplemousses", Region = "North", PostalCode = "21301", Lat = -20.1102m, Lng = 57.5750m },
            new { Code = "PA-002", Name = "Triolet", Type = "Village", District = "Pamplemousses", Region = "North", PostalCode = "22025", Lat = -20.0555m, Lng = 57.5480m },
            new { Code = "PA-003", Name = "Grand Baie", Type = "Town", District = "Pamplemousses", Region = "North", PostalCode = "30501", Lat = -20.0097m, Lng = 57.5812m },

            // Rivière du Rempart District
            new { Code = "RR-001", Name = "Goodlands", Type = "Town", District = "Rivière du Rempart", Region = "North", PostalCode = "30403", Lat = -20.0364m, Lng = 57.6428m },
            new { Code = "RR-002", Name = "Cap Malheureux", Type = "Village", District = "Rivière du Rempart", Region = "North", PostalCode = "31706", Lat = -19.9833m, Lng = 57.6167m },
            new { Code = "RR-003", Name = "Mapou", Type = "Village", District = "Rivière du Rempart", Region = "North", PostalCode = "31803", Lat = -20.0519m, Lng = 57.6897m },

            // Flacq District
            new { Code = "FL-001", Name = "Centre de Flacq", Type = "Town", District = "Flacq", Region = "East", PostalCode = "40901", Lat = -20.1897m, Lng = 57.7167m },
            new { Code = "FL-002", Name = "Bon Accueil", Type = "Village", District = "Flacq", Region = "East", PostalCode = "40502", Lat = -20.1778m, Lng = 57.6889m },
            new { Code = "FL-003", Name = "Quatre Cocos", Type = "Village", District = "Flacq", Region = "East", PostalCode = "41518", Lat = -20.2167m, Lng = 57.7500m },

            // Grand Port District
            new { Code = "GP-001", Name = "Mahébourg", Type = "Town", District = "Grand Port", Region = "South East", PostalCode = "50801", Lat = -20.4089m, Lng = 57.7000m },
            new { Code = "GP-002", Name = "Rose Belle", Type = "Town", District = "Grand Port", Region = "South East", PostalCode = "51301", Lat = -20.3947m, Lng = 57.6019m },
            new { Code = "GP-003", Name = "Plaine Magnien", Type = "Village", District = "Grand Port", Region = "South East", PostalCode = "51502", Lat = -20.4333m, Lng = 57.6667m },

            // Savanne District
            new { Code = "SA-001", Name = "Souillac", Type = "Town", District = "Savanne", Region = "South", PostalCode = "60801", Lat = -20.5167m, Lng = 57.5167m },
            new { Code = "SA-002", Name = "Rivière des Anguilles", Type = "Village", District = "Savanne", Region = "South", PostalCode = "60402", Lat = -20.4667m, Lng = 57.5500m },
            new { Code = "SA-003", Name = "Surinam", Type = "Village", District = "Savanne", Region = "South", PostalCode = "60903", Lat = -20.5000m, Lng = 57.5000m },

            // Plaines Wilhems District
            new { Code = "PW-001", Name = "Curepipe", Type = "Town", District = "Plaines Wilhems", Region = "Central", PostalCode = "74101", Lat = -20.3167m, Lng = 57.5167m },
            new { Code = "PW-002", Name = "Quatre Bornes", Type = "Town", District = "Plaines Wilhems", Region = "Central", PostalCode = "72301", Lat = -20.2653m, Lng = 57.4792m },
            new { Code = "PW-003", Name = "Vacoas-Phoenix", Type = "Town", District = "Plaines Wilhems", Region = "Central", PostalCode = "73401", Lat = -20.2981m, Lng = 57.4931m },
            new { Code = "PW-004", Name = "Rose Hill", Type = "Town", District = "Plaines Wilhems", Region = "Central", PostalCode = "71201", Lat = -20.1333m, Lng = 57.4833m },
            new { Code = "PW-005", Name = "Beau Bassin", Type = "Town", District = "Plaines Wilhems", Region = "Central", PostalCode = "71504", Lat = -20.2333m, Lng = 57.4667m },

            // Moka District
            new { Code = "MO-001", Name = "Moka", Type = "Village", District = "Moka", Region = "Central", PostalCode = "80801", Lat = -20.2281m, Lng = 57.5783m },
            new { Code = "MO-002", Name = "Quartier Militaire", Type = "Village", District = "Moka", Region = "Central", PostalCode = "80503", Lat = -20.2667m, Lng = 57.6167m },
            new { Code = "MO-003", Name = "Saint Pierre", Type = "Village", District = "Moka", Region = "Central", PostalCode = "81301", Lat = -20.2139m, Lng = 57.5228m },

            // Black River District
            new { Code = "BR-001", Name = "Tamarin", Type = "Village", District = "Black River", Region = "West", PostalCode = "90901", Lat = -20.3242m, Lng = 57.3739m },
            new { Code = "BR-002", Name = "Flic en Flac", Type = "Village", District = "Black River", Region = "West", PostalCode = "90203", Lat = -20.2764m, Lng = 57.3694m },
            new { Code = "BR-003", Name = "Grande Rivière Noire", Type = "Village", District = "Black River", Region = "West", PostalCode = "90601", Lat = -20.3667m, Lng = 57.3667m },
        };

        foreach (var loc in mauritiusLocations)
        {
            var location = new Location
            {
                Id = Guid.NewGuid(),
                LocationCode = loc.Code,
                LocationName = loc.Name,
                LocationType = loc.Type,
                City = loc.Name,
                District = loc.District,
                Region = loc.Region,
                PostalCode = loc.PostalCode,
                Country = "Mauritius",
                Latitude = loc.Lat,
                Longitude = loc.Lng,
                Timezone = "Indian/Mauritius",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            _context.Locations.Add(location);
        }

        await _context.SaveChangesAsync();
    }
}
