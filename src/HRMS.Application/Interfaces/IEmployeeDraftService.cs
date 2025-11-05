using HRMS.Application.DTOs;
using HRMS.Core.Entities.Tenant;

namespace HRMS.Application.Interfaces;

public interface IEmployeeDraftService
{
    Task<List<EmployeeDraftDto>> GetDraftsForTenantAsync();
    Task<EmployeeDraftDto?> GetDraftByIdAsync(Guid id);
    Task<EmployeeDraft> SaveDraftAsync(SaveEmployeeDraftRequest request, Guid userId, string userName);
    Task<Employee> FinalizeDraftAsync(Guid draftId);
    Task DeleteDraftAsync(Guid id);
    Task DeleteExpiredDraftsAsync();
}
