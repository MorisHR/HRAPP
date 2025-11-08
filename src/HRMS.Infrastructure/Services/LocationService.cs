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
}
