using HRMS.Application.DTOs.PayrollDtos;

namespace HRMS.Application.Interfaces;

/// <summary>
/// Service interface for managing employee salary components (allowances and deductions)
/// </summary>
public interface ISalaryComponentService
{
    /// <summary>
    /// Creates a new salary component for an employee
    /// </summary>
    /// <param name="dto">Salary component details</param>
    /// <param name="createdBy">Username of the user creating the component</param>
    /// <returns>ID of the created salary component</returns>
    Task<Guid> CreateComponentAsync(CreateSalaryComponentDto dto, string createdBy);

    /// <summary>
    /// Retrieves a salary component by ID
    /// </summary>
    /// <param name="id">Component ID</param>
    /// <returns>Salary component details or null if not found</returns>
    Task<SalaryComponentDto?> GetComponentAsync(Guid id);

    /// <summary>
    /// Retrieves all salary components for a specific employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="activeOnly">If true, returns only active components</param>
    /// <returns>List of salary components</returns>
    Task<List<SalaryComponentDto>> GetEmployeeComponentsAsync(Guid employeeId, bool activeOnly = true);

    /// <summary>
    /// Retrieves all salary components for all employees (for admin/HR)
    /// </summary>
    /// <param name="activeOnly">If true, returns only active components</param>
    /// <returns>List of all salary components</returns>
    Task<List<SalaryComponentDto>> GetAllComponentsAsync(bool activeOnly = true);

    /// <summary>
    /// Updates an existing salary component
    /// </summary>
    /// <param name="id">Component ID to update</param>
    /// <param name="dto">Updated component details</param>
    /// <param name="updatedBy">Username of the user updating the component</param>
    Task UpdateComponentAsync(Guid id, UpdateSalaryComponentDto dto, string updatedBy);

    /// <summary>
    /// Deactivates a salary component (soft delete)
    /// </summary>
    /// <param name="id">Component ID to deactivate</param>
    /// <param name="updatedBy">Username of the user deactivating</param>
    Task DeactivateComponentAsync(Guid id, string updatedBy);

    /// <summary>
    /// Permanently deletes a salary component
    /// </summary>
    /// <param name="id">Component ID to delete</param>
    Task DeleteComponentAsync(Guid id);

    /// <summary>
    /// Calculates total allowances for an employee in a specific month
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="month">Month (1-12)</param>
    /// <param name="year">Year</param>
    /// <returns>Total allowances amount</returns>
    Task<decimal> GetTotalAllowancesAsync(Guid employeeId, int month, int year);

    /// <summary>
    /// Calculates total deductions for an employee in a specific month
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="month">Month (1-12)</param>
    /// <param name="year">Year</param>
    /// <returns>Total deductions amount</returns>
    Task<decimal> GetTotalDeductionsAsync(Guid employeeId, int month, int year);

    /// <summary>
    /// Approves a salary component that requires approval
    /// </summary>
    /// <param name="id">Component ID to approve</param>
    /// <param name="approvedBy">Username of the approver</param>
    Task ApproveComponentAsync(Guid id, string approvedBy);

    /// <summary>
    /// Bulk creates salary components for multiple employees
    /// Useful for company-wide allowances or deductions
    /// </summary>
    /// <param name="employeeIds">List of employee IDs</param>
    /// <param name="dto">Component details to apply</param>
    /// <param name="createdBy">Username of the user creating</param>
    /// <returns>List of created component IDs</returns>
    Task<List<Guid>> BulkCreateComponentsAsync(List<Guid> employeeIds, CreateSalaryComponentDto dto, string createdBy);
}
