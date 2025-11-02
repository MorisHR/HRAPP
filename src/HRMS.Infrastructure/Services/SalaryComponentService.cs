using HRMS.Application.DTOs.PayrollDtos;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for managing employee salary components (allowances and deductions)
/// </summary>
public class SalaryComponentService : ISalaryComponentService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<SalaryComponentService> _logger;

    public SalaryComponentService(
        TenantDbContext context,
        ILogger<SalaryComponentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> CreateComponentAsync(CreateSalaryComponentDto dto, string createdBy)
    {
        _logger.LogInformation("Creating salary component {ComponentName} for employee {EmployeeId}",
            dto.ComponentName, dto.EmployeeId);

        // Validate employee exists
        var employee = await _context.Employees.FindAsync(dto.EmployeeId);
        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        var component = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            EmployeeId = dto.EmployeeId,
            ComponentType = dto.ComponentType,
            ComponentName = dto.ComponentName,
            Amount = dto.Amount,
            IsRecurring = dto.IsRecurring,
            IsDeduction = dto.IsDeduction,
            IsTaxable = dto.IsTaxable,
            IncludeInStatutory = dto.IncludeInStatutory,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            CalculationMethod = dto.CalculationMethod,
            PercentageBase = dto.PercentageBase,
            Description = dto.Description,
            RequiresApproval = dto.RequiresApproval,
            IsApproved = !dto.RequiresApproval, // Auto-approve if doesn't require approval
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.SalaryComponents.Add(component);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Salary component {ComponentId} created successfully", component.Id);

        return component.Id;
    }

    public async Task<SalaryComponentDto?> GetComponentAsync(Guid id)
    {
        var component = await _context.SalaryComponents
            .Include(sc => sc.Employee)
            .Where(sc => sc.Id == id && !sc.IsDeleted)
            .FirstOrDefaultAsync();

        if (component == null)
            return null;

        return MapToDto(component);
    }

    public async Task<List<SalaryComponentDto>> GetEmployeeComponentsAsync(Guid employeeId, bool activeOnly = true)
    {
        var query = _context.SalaryComponents
            .Include(sc => sc.Employee)
            .Where(sc => sc.EmployeeId == employeeId && !sc.IsDeleted);

        if (activeOnly)
            query = query.Where(sc => sc.IsActive);

        var components = await query
            .OrderBy(sc => sc.CalculationOrder)
            .ThenBy(sc => sc.ComponentName)
            .ToListAsync();

        return components.Select(MapToDto).ToList();
    }

    public async Task<List<SalaryComponentDto>> GetAllComponentsAsync(bool activeOnly = true)
    {
        var query = _context.SalaryComponents
            .Include(sc => sc.Employee)
            .Where(sc => !sc.IsDeleted);

        if (activeOnly)
            query = query.Where(sc => sc.IsActive);

        var components = await query
            .OrderBy(sc => sc.Employee.EmployeeCode)
            .ThenBy(sc => sc.ComponentName)
            .ToListAsync();

        return components.Select(MapToDto).ToList();
    }

    public async Task UpdateComponentAsync(Guid id, UpdateSalaryComponentDto dto, string updatedBy)
    {
        var component = await _context.SalaryComponents
            .FirstOrDefaultAsync(sc => sc.Id == id && !sc.IsDeleted);

        if (component == null)
            throw new InvalidOperationException("Salary component not found");

        // Update only provided fields
        if (dto.Amount.HasValue)
            component.Amount = dto.Amount.Value;

        if (dto.EffectiveTo.HasValue)
            component.EffectiveTo = dto.EffectiveTo.Value;

        if (dto.IsActive.HasValue)
            component.IsActive = dto.IsActive.Value;

        if (!string.IsNullOrEmpty(dto.Description))
            component.Description = dto.Description;

        component.UpdatedBy = updatedBy;
        component.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Salary component {ComponentId} updated", id);
    }

    public async Task DeactivateComponentAsync(Guid id, string updatedBy)
    {
        var component = await _context.SalaryComponents
            .FirstOrDefaultAsync(sc => sc.Id == id && !sc.IsDeleted);

        if (component == null)
            throw new InvalidOperationException("Salary component not found");

        component.IsActive = false;
        component.EffectiveTo = DateTime.UtcNow.Date;
        component.UpdatedBy = updatedBy;
        component.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Salary component {ComponentId} deactivated", id);
    }

    public async Task DeleteComponentAsync(Guid id)
    {
        var component = await _context.SalaryComponents
            .FirstOrDefaultAsync(sc => sc.Id == id && !sc.IsDeleted);

        if (component == null)
            throw new InvalidOperationException("Salary component not found");

        component.IsDeleted = true;
        component.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Salary component {ComponentId} deleted", id);
    }

    public async Task<decimal> GetTotalAllowancesAsync(Guid employeeId, int month, int year)
    {
        var targetDate = new DateTime(year, month, 1);

        var allowances = await _context.SalaryComponents
            .Where(sc => sc.EmployeeId == employeeId)
            .Where(sc => !sc.IsDeleted && sc.IsActive && sc.IsApproved)
            .Where(sc => !sc.IsDeduction) // Allowances only
            .Where(sc => sc.EffectiveFrom <= targetDate)
            .Where(sc => !sc.EffectiveTo.HasValue || sc.EffectiveTo.Value >= targetDate)
            .ToListAsync();

        // For recurring components or one-time components that fall in this month
        var applicableAllowances = allowances.Where(a =>
            a.IsRecurring ||
            (a.EffectiveFrom.Year == year && a.EffectiveFrom.Month == month)
        ).ToList();

        decimal total = 0m;

        foreach (var allowance in applicableAllowances)
        {
            if (allowance.CalculationMethod == "Fixed")
            {
                total += allowance.Amount;
            }
            else if (allowance.CalculationMethod == "Percentage")
            {
                // Calculate based on percentage of base salary
                var employee = await _context.Employees.FindAsync(employeeId);
                if (employee != null)
                {
                    var baseAmount = allowance.PercentageBase == "GrossSalary"
                        ? employee.BasicSalary // Simplified - should calculate full gross
                        : employee.BasicSalary;

                    total += baseAmount * (allowance.Amount / 100);
                }
            }
        }

        return Math.Round(total, 2);
    }

    public async Task<decimal> GetTotalDeductionsAsync(Guid employeeId, int month, int year)
    {
        var targetDate = new DateTime(year, month, 1);

        var deductions = await _context.SalaryComponents
            .Where(sc => sc.EmployeeId == employeeId)
            .Where(sc => !sc.IsDeleted && sc.IsActive && sc.IsApproved)
            .Where(sc => sc.IsDeduction) // Deductions only
            .Where(sc => sc.EffectiveFrom <= targetDate)
            .Where(sc => !sc.EffectiveTo.HasValue || sc.EffectiveTo.Value >= targetDate)
            .ToListAsync();

        // For recurring components or one-time components that fall in this month
        var applicableDeductions = deductions.Where(d =>
            d.IsRecurring ||
            (d.EffectiveFrom.Year == year && d.EffectiveFrom.Month == month)
        ).ToList();

        decimal total = 0m;

        foreach (var deduction in applicableDeductions)
        {
            if (deduction.CalculationMethod == "Fixed")
            {
                total += deduction.Amount;
            }
            else if (deduction.CalculationMethod == "Percentage")
            {
                // Calculate based on percentage of base salary
                var employee = await _context.Employees.FindAsync(employeeId);
                if (employee != null)
                {
                    var baseAmount = deduction.PercentageBase == "GrossSalary"
                        ? employee.BasicSalary // Simplified - should calculate full gross
                        : employee.BasicSalary;

                    total += baseAmount * (deduction.Amount / 100);
                }
            }
        }

        return Math.Round(total, 2);
    }

    public async Task ApproveComponentAsync(Guid id, string approvedBy)
    {
        var component = await _context.SalaryComponents
            .FirstOrDefaultAsync(sc => sc.Id == id && !sc.IsDeleted);

        if (component == null)
            throw new InvalidOperationException("Salary component not found");

        if (!component.RequiresApproval)
            throw new InvalidOperationException("This component does not require approval");

        if (component.IsApproved)
            throw new InvalidOperationException("Component is already approved");

        component.IsApproved = true;
        component.ApprovedBy = Guid.Parse(approvedBy); // Simplified - should lookup user
        component.ApprovedAt = DateTime.UtcNow;
        component.UpdatedBy = approvedBy;
        component.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Salary component {ComponentId} approved by {ApprovedBy}", id, approvedBy);
    }

    public async Task<List<Guid>> BulkCreateComponentsAsync(List<Guid> employeeIds, CreateSalaryComponentDto dto, string createdBy)
    {
        _logger.LogInformation("Bulk creating salary component {ComponentName} for {EmployeeCount} employees",
            dto.ComponentName, employeeIds.Count);

        // Validate all employees exist
        var employees = await _context.Employees
            .Where(e => employeeIds.Contains(e.Id) && !e.IsDeleted)
            .ToListAsync();

        if (employees.Count != employeeIds.Count)
            throw new InvalidOperationException("One or more employees not found");

        var components = new List<SalaryComponent>();
        var createdIds = new List<Guid>();

        foreach (var employeeId in employeeIds)
        {
            var component = new SalaryComponent
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                ComponentType = dto.ComponentType,
                ComponentName = dto.ComponentName,
                Amount = dto.Amount,
                IsRecurring = dto.IsRecurring,
                IsDeduction = dto.IsDeduction,
                IsTaxable = dto.IsTaxable,
                IncludeInStatutory = dto.IncludeInStatutory,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                CalculationMethod = dto.CalculationMethod,
                PercentageBase = dto.PercentageBase,
                Description = dto.Description,
                RequiresApproval = dto.RequiresApproval,
                IsApproved = !dto.RequiresApproval,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            components.Add(component);
            createdIds.Add(component.Id);
        }

        _context.SalaryComponents.AddRange(components);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk created {ComponentCount} salary components", components.Count);

        return createdIds;
    }

    // ==================== HELPER METHODS ====================

    private SalaryComponentDto MapToDto(SalaryComponent component)
    {
        return new SalaryComponentDto
        {
            Id = component.Id,
            EmployeeId = component.EmployeeId,
            EmployeeCode = component.Employee.EmployeeCode,
            EmployeeName = $"{component.Employee.FirstName} {component.Employee.LastName}",
            ComponentType = component.ComponentType,
            ComponentTypeDisplay = component.ComponentType.ToString(),
            ComponentName = component.ComponentName,
            Amount = component.Amount,
            Currency = component.Currency,
            IsRecurring = component.IsRecurring,
            IsDeduction = component.IsDeduction,
            IsTaxable = component.IsTaxable,
            IncludeInStatutory = component.IncludeInStatutory,
            EffectiveFrom = component.EffectiveFrom,
            EffectiveTo = component.EffectiveTo,
            IsActive = component.IsActive,
            Description = component.Description,
            CalculationMethod = component.CalculationMethod,
            PercentageBase = component.PercentageBase,
            RequiresApproval = component.RequiresApproval,
            IsApproved = component.IsApproved,
            ApprovedAt = component.ApprovedAt,
            CreatedAt = component.CreatedAt,
            UpdatedAt = component.UpdatedAt
        };
    }
}
