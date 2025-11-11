using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Infrastructure.Data;

namespace HRMS.Infrastructure.Services;

public class EmployeeDraftService : IEmployeeDraftService
{
    private readonly TenantDbContext _context;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeeDraftService> _logger;

    public EmployeeDraftService(
        TenantDbContext context,
        IEmployeeService employeeService,
        ILogger<EmployeeDraftService> logger)
    {
        _context = context;
        _employeeService = employeeService;
        _logger = logger;
    }

    public async Task<List<EmployeeDraftDto>> GetDraftsForTenantAsync()
    {
        var drafts = await _context.EmployeeDrafts
            .Where(d => !d.IsDeleted)
            .OrderByDescending(d => d.LastEditedAt)
            .ToListAsync();

        return drafts.Select(d => new EmployeeDraftDto
        {
            Id = d.Id,
            FormDataJson = d.FormDataJson,
            DraftName = d.DraftName,
            CompletionPercentage = d.CompletionPercentage,
            CreatedBy = d.CreatedBy,
            CreatedByName = d.CreatedByName,
            CreatedAt = d.CreatedAt,
            LastEditedBy = d.LastEditedBy,
            LastEditedByName = d.LastEditedByName,
            LastEditedAt = d.LastEditedAt,
            ExpiresAt = d.ExpiresAt,
            DaysUntilExpiry = d.DaysUntilExpiry,
            IsExpired = d.IsExpired
        }).ToList();
    }

    public async Task<EmployeeDraftDto?> GetDraftByIdAsync(Guid id)
    {
        var draft = await _context.EmployeeDrafts
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (draft == null)
            return null;

        return new EmployeeDraftDto
        {
            Id = draft.Id,
            FormDataJson = draft.FormDataJson,
            DraftName = draft.DraftName,
            CompletionPercentage = draft.CompletionPercentage,
            CreatedBy = draft.CreatedBy,
            CreatedByName = draft.CreatedByName,
            CreatedAt = draft.CreatedAt,
            LastEditedBy = draft.LastEditedBy,
            LastEditedByName = draft.LastEditedByName,
            LastEditedAt = draft.LastEditedAt,
            ExpiresAt = draft.ExpiresAt,
            DaysUntilExpiry = draft.DaysUntilExpiry,
            IsExpired = draft.IsExpired
        };
    }

    public async Task<EmployeeDraft> SaveDraftAsync(
        SaveEmployeeDraftRequest request,
        Guid userId,
        string userName)
    {
        EmployeeDraft draft;

        if (request.Id.HasValue)
        {
            // Update existing draft
            var existingDraft = await _context.EmployeeDrafts
                .FirstOrDefaultAsync(d => d.Id == request.Id.Value && !d.IsDeleted);

            if (existingDraft == null)
                throw new KeyNotFoundException($"Draft {request.Id} not found");

            draft = existingDraft;

            draft.FormDataJson = request.FormDataJson;
            draft.DraftName = request.DraftName;
            draft.CompletionPercentage = request.CompletionPercentage;
            draft.LastEditedBy = userId;
            draft.LastEditedByName = userName;
            draft.LastEditedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new draft
            draft = new EmployeeDraft
            {
                Id = Guid.NewGuid(),
                FormDataJson = request.FormDataJson,
                DraftName = request.DraftName,
                CompletionPercentage = request.CompletionPercentage,
                CreatedBy = userId,
                CreatedByName = userName,
                CreatedAt = DateTime.UtcNow,
                LastEditedBy = userId,
                LastEditedByName = userName,
                LastEditedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30), // 30-day expiry
                IsDeleted = false
            };

            _context.EmployeeDrafts.Add(draft);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Employee draft saved: {DraftId} by {UserName} ({CompletionPercentage}%)",
            draft.Id, userName, draft.CompletionPercentage);

        return draft;
    }

    public async Task<Employee> FinalizeDraftAsync(Guid draftId)
    {
        var draft = await _context.EmployeeDrafts
            .FirstOrDefaultAsync(d => d.Id == draftId && !d.IsDeleted);

        if (draft == null)
            throw new KeyNotFoundException($"Draft {draftId} not found");

        // Deserialize form data
        var formData = JsonSerializer.Deserialize<Dictionary<string, object>>(draft.FormDataJson);

        if (formData == null)
            throw new InvalidOperationException("Invalid draft data");

        // TODO: Map form data to CreateEmployeeRequest
        // For now, this is a placeholder
        var createRequest = new CreateEmployeeRequest
        {
            // Map fields from formData
        };

        // Create employee using employee service
        var employeeDto = await _employeeService.CreateEmployeeAsync(createRequest);

        // Fetch the actual employee entity from database
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeDto.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created employee");

        // Delete draft after successful employee creation
        draft.IsDeleted = true;
        draft.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Employee draft {DraftId} finalized and employee {EmployeeId} created",
            draftId, employee.Id);

        return employee;
    }

    public async Task DeleteDraftAsync(Guid id)
    {
        var draft = await _context.EmployeeDrafts
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (draft == null)
            throw new KeyNotFoundException($"Draft {id} not found");

        draft.IsDeleted = true;
        draft.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee draft {DraftId} deleted", id);
    }

    public async Task DeleteExpiredDraftsAsync()
    {
        var expiredDrafts = await _context.EmployeeDrafts
            .Where(d => !d.IsDeleted && d.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var draft in expiredDrafts)
        {
            draft.IsDeleted = true;
            draft.DeletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted {Count} expired employee drafts",
            expiredDrafts.Count);
    }
}
