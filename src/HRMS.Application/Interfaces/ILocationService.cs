using HRMS.Application.DTOs.LocationDtos;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service interface for managing physical work locations
/// </summary>
public interface ILocationService
{
    // CRUD Operations
    Task<Guid> CreateLocationAsync(CreateLocationDto dto, string createdBy);
    Task UpdateLocationAsync(Guid id, UpdateLocationDto dto, string updatedBy);
    Task DeleteLocationAsync(Guid id, string deletedBy);

    // Retrieval
    Task<LocationDto?> GetLocationByIdAsync(Guid id);
    Task<LocationDto?> GetLocationByCodeAsync(string locationCode);
    Task<List<LocationSummaryDto>> GetAllLocationsAsync(bool activeOnly = true);
    Task<List<LocationDropdownDto>> GetLocationsForDropdownAsync(bool activeOnly = true);

    // Statistics
    Task<int> GetDeviceCountByLocationAsync(Guid locationId);
    Task<int> GetEmployeeCountByLocationAsync(Guid locationId);
}
