using HRMS.Application.DTOs;
using HRMS.Core.Entities.Tenant;

namespace HRMS.Application.Interfaces;

public interface IEmployeeService
{
    // CRUD Operations
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request);
    Task<EmployeeDto> GetEmployeeByIdAsync(Guid id);
    Task<EmployeeDto> GetEmployeeByCodeAsync(string employeeCode);
    Task<List<EmployeeListDto>> GetAllEmployeesAsync(bool includeInactive = false);
    Task<EmployeeDto> UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request);
    Task<bool> DeleteEmployeeAsync(Guid id); // Soft delete

    // Expatriate-specific
    Task<List<EmployeeListDto>> GetExpatriateEmployeesAsync();
    Task<Dictionary<string, int>> GetEmployeesByCountryAsync();
    Task<List<DocumentExpiryInfoDto>> GetExpiringDocumentsAsync(int daysAhead = 90);
    Task<EmployeeDto> RenewVisaAsync(Guid id, DateTime newExpiryDate, string? newVisaNumber = null);

    // Document status
    Task<DocumentExpiryInfoDto> GetDocumentStatusAsync(Guid id);

    // Search & Filter
    Task<List<EmployeeListDto>> SearchEmployeesAsync(string searchTerm);
    Task<List<EmployeeListDto>> GetEmployeesByDepartmentAsync(Guid departmentId);

    // Validation
    string? ValidateExpatriateMandatoryFields(Employee employee);
    Task<bool> IsEmployeeCodeUniqueAsync(string employeeCode, Guid? excludeEmployeeId = null);
    Task<bool> IsEmailUniqueAsync(string email, Guid? excludeEmployeeId = null);

    // Leave calculation
    decimal CalculateProRatedAnnualLeave(DateTime joiningDate, DateTime calculationDate);
}
