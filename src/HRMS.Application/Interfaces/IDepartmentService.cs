using HRMS.Application.DTOs.DepartmentDtos;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service interface for department management operations
/// Follows SOLID principles and enables dependency injection
/// </summary>
public interface IDepartmentService
{
    /// <summary>
    /// Get all departments with optional filtering and pagination
    /// </summary>
    Task<List<DepartmentDto>> GetAllAsync();

    /// <summary>
    /// Search departments with advanced filtering
    /// </summary>
    /// <param name="searchDto">Search criteria including filters, pagination, and sorting</param>
    /// <returns>Paginated and filtered department list</returns>
    Task<DepartmentSearchResultDto> SearchAsync(DepartmentSearchDto searchDto);

    /// <summary>
    /// Get a single department by ID
    /// </summary>
    Task<DepartmentDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get department with full details including audit trail
    /// </summary>
    Task<DepartmentDetailDto?> GetDetailAsync(Guid id);

    /// <summary>
    /// Create a new department with comprehensive validation
    /// </summary>
    /// <returns>Tuple of success status, message, and created department ID</returns>
    Task<(bool Success, string Message, Guid? DepartmentId)> CreateAsync(
        CreateDepartmentDto dto, string createdBy);

    /// <summary>
    /// Update an existing department
    /// </summary>
    Task<(bool Success, string Message)> UpdateAsync(
        Guid id, UpdateDepartmentDto dto, string updatedBy);

    /// <summary>
    /// Bulk update department status (activate/deactivate)
    /// </summary>
    Task<(bool Success, string Message, int AffectedCount)> BulkUpdateStatusAsync(
        BulkDepartmentStatusDto dto, string updatedBy);

    /// <summary>
    /// Delete a department (soft delete)
    /// </summary>
    Task<(bool Success, string Message)> DeleteAsync(Guid id, string deletedBy);

    /// <summary>
    /// Bulk delete multiple departments
    /// </summary>
    Task<(bool Success, string Message, int DeletedCount, List<string> Errors)> BulkDeleteAsync(
        List<Guid> departmentIds, string deletedBy);

    /// <summary>
    /// Merge two departments - moves all employees and sub-departments from source to target
    /// </summary>
    Task<(bool Success, string Message, DepartmentMergeResultDto? Result)> MergeDepartmentsAsync(
        DepartmentMergeDto dto, string performedBy);

    /// <summary>
    /// Reassign employees from one department to another
    /// </summary>
    Task<(bool Success, string Message, int ReassignedCount)> ReassignEmployeesAsync(
        Guid fromDepartmentId, Guid toDepartmentId, string performedBy);

    /// <summary>
    /// Get department hierarchy as a tree structure with role-based filtering
    /// </summary>
    Task<List<DepartmentHierarchyDto>> GetHierarchyAsync(Guid? userId = null, string? userRole = null);

    /// <summary>
    /// Get simplified list for dropdowns (cached)
    /// </summary>
    Task<List<DepartmentDropdownDto>> GetDropdownAsync(bool activeOnly = true);

    /// <summary>
    /// Get department activity history for audit trail
    /// </summary>
    Task<List<DepartmentActivityDto>> GetActivityHistoryAsync(Guid departmentId);

    /// <summary>
    /// Validate if a department code is available
    /// </summary>
    Task<bool> IsCodeAvailableAsync(string code, Guid? excludeDepartmentId = null);

    /// <summary>
    /// Validate if an employee is already a department head
    /// </summary>
    Task<(bool IsHead, string? DepartmentName)> IsEmployeeDepartmentHeadAsync(Guid employeeId);

    /// <summary>
    /// Get departments by employee (shows department and all parent departments)
    /// </summary>
    Task<List<DepartmentDto>> GetDepartmentsByEmployeeAsync(Guid employeeId);

    /// <summary>
    /// Validate department business rules
    /// </summary>
    Task<(bool IsValid, List<string> Errors)> ValidateDepartmentRulesAsync(
        CreateDepartmentDto dto, Guid? departmentId = null);
}
