using HRMS.Application.DTOs.DepartmentDtos;
using HRMS.Core.Entities.Tenant;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Service for managing departments within a tenant
/// </summary>
public class DepartmentService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(TenantDbContext context, ILogger<DepartmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all departments for the current tenant
    /// </summary>
    public async Task<List<DepartmentDto>> GetAllAsync()
    {
        var departments = await _context.Departments
            .Include(d => d.ParentDepartment)
            .Include(d => d.DepartmentHead)
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Code = d.Code,
                Description = d.Description,
                ParentDepartmentId = d.ParentDepartmentId,
                ParentDepartmentName = d.ParentDepartment != null ? d.ParentDepartment.Name : null,
                DepartmentHeadId = d.DepartmentHeadId,
                DepartmentHeadName = d.DepartmentHead != null
                    ? $"{d.DepartmentHead.FirstName} {d.DepartmentHead.LastName}"
                    : null,
                CostCenterCode = d.CostCenterCode,
                IsActive = d.IsActive,
                EmployeeCount = d.Employees.Count(e => !e.IsDeleted && !e.IsOffboarded),
                CreatedAt = d.CreatedAt,
                CreatedBy = d.CreatedBy,
                UpdatedAt = d.UpdatedAt,
                UpdatedBy = d.UpdatedBy
            })
            .ToListAsync();

        return departments;
    }

    /// <summary>
    /// Get a single department by ID
    /// </summary>
    public async Task<DepartmentDto?> GetByIdAsync(Guid id)
    {
        var department = await _context.Departments
            .Include(d => d.ParentDepartment)
            .Include(d => d.DepartmentHead)
            .Where(d => d.Id == id && !d.IsDeleted)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Code = d.Code,
                Description = d.Description,
                ParentDepartmentId = d.ParentDepartmentId,
                ParentDepartmentName = d.ParentDepartment != null ? d.ParentDepartment.Name : null,
                DepartmentHeadId = d.DepartmentHeadId,
                DepartmentHeadName = d.DepartmentHead != null
                    ? $"{d.DepartmentHead.FirstName} {d.DepartmentHead.LastName}"
                    : null,
                CostCenterCode = d.CostCenterCode,
                IsActive = d.IsActive,
                EmployeeCount = d.Employees.Count(e => !e.IsDeleted && !e.IsOffboarded),
                CreatedAt = d.CreatedAt,
                CreatedBy = d.CreatedBy,
                UpdatedAt = d.UpdatedAt,
                UpdatedBy = d.UpdatedBy
            })
            .FirstOrDefaultAsync();

        return department;
    }

    /// <summary>
    /// Create a new department
    /// </summary>
    public async Task<(bool Success, string Message, Guid? DepartmentId)> CreateAsync(
        CreateDepartmentDto dto, string createdBy)
    {
        // Validate unique code
        var codeExists = await _context.Departments
            .AnyAsync(d => d.Code == dto.Code && !d.IsDeleted);

        if (codeExists)
        {
            return (false, "A department with this code already exists.", null);
        }

        // Validate parent department exists if specified
        if (dto.ParentDepartmentId.HasValue)
        {
            var parentExists = await _context.Departments
                .AnyAsync(d => d.Id == dto.ParentDepartmentId.Value && !d.IsDeleted);

            if (!parentExists)
            {
                return (false, "Parent department not found.", null);
            }
        }

        // Validate department head exists if specified
        if (dto.DepartmentHeadId.HasValue)
        {
            var headExists = await _context.Employees
                .AnyAsync(e => e.Id == dto.DepartmentHeadId.Value && !e.IsDeleted && !e.IsOffboarded);

            if (!headExists)
            {
                return (false, "Department head employee not found or is offboarded.", null);
            }
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Code = dto.Code.ToUpper(), // Ensure uppercase
            Description = dto.Description,
            ParentDepartmentId = dto.ParentDepartmentId,
            DepartmentHeadId = dto.DepartmentHeadId,
            CostCenterCode = dto.CostCenterCode,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            IsDeleted = false
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Department created: {Code} - {Name} by {User}",
            department.Code, department.Name, createdBy);

        return (true, "Department created successfully.", department.Id);
    }

    /// <summary>
    /// Update an existing department
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAsync(
        Guid id, UpdateDepartmentDto dto, string updatedBy)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (department == null)
        {
            return (false, "Department not found.");
        }

        // Validate unique code (excluding current department)
        var codeExists = await _context.Departments
            .AnyAsync(d => d.Code == dto.Code && d.Id != id && !d.IsDeleted);

        if (codeExists)
        {
            return (false, "A department with this code already exists.");
        }

        // Validate parent department exists and prevent circular reference
        if (dto.ParentDepartmentId.HasValue)
        {
            if (dto.ParentDepartmentId.Value == id)
            {
                return (false, "A department cannot be its own parent.");
            }

            var parentExists = await _context.Departments
                .AnyAsync(d => d.Id == dto.ParentDepartmentId.Value && !d.IsDeleted);

            if (!parentExists)
            {
                return (false, "Parent department not found.");
            }

            // Check for circular reference (is this department an ancestor of the proposed parent?)
            var isCircular = await IsCircularReferenceAsync(id, dto.ParentDepartmentId.Value);
            if (isCircular)
            {
                return (false, "This would create a circular reference in the department hierarchy.");
            }
        }

        // Validate department head exists if specified
        if (dto.DepartmentHeadId.HasValue)
        {
            var headExists = await _context.Employees
                .AnyAsync(e => e.Id == dto.DepartmentHeadId.Value && !e.IsDeleted && !e.IsOffboarded);

            if (!headExists)
            {
                return (false, "Department head employee not found or is offboarded.");
            }
        }

        department.Name = dto.Name;
        department.Code = dto.Code.ToUpper();
        department.Description = dto.Description;
        department.ParentDepartmentId = dto.ParentDepartmentId;
        department.DepartmentHeadId = dto.DepartmentHeadId;
        department.CostCenterCode = dto.CostCenterCode;
        department.IsActive = dto.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Department updated: {Code} - {Name} by {User}",
            department.Code, department.Name, updatedBy);

        return (true, "Department updated successfully.");
    }

    /// <summary>
    /// Delete a department (soft delete)
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAsync(Guid id, string deletedBy)
    {
        var department = await _context.Departments
            .Include(d => d.Employees)
            .Include(d => d.SubDepartments)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (department == null)
        {
            return (false, "Department not found.");
        }

        // Check if department has active employees
        var activeEmployeeCount = department.Employees.Count(e => !e.IsDeleted && !e.IsOffboarded);
        if (activeEmployeeCount > 0)
        {
            return (false, $"Cannot delete department. It has {activeEmployeeCount} active employee(s). " +
                           "Please reassign employees to another department first.");
        }

        // Check if department has sub-departments
        var subDepartmentCount = department.SubDepartments.Count(d => !d.IsDeleted);
        if (subDepartmentCount > 0)
        {
            return (false, $"Cannot delete department. It has {subDepartmentCount} sub-department(s). " +
                           "Please delete or reassign sub-departments first.");
        }

        department.IsDeleted = true;
        department.DeletedAt = DateTime.UtcNow;
        department.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Department deleted: {Code} - {Name} by {User}",
            department.Code, department.Name, deletedBy);

        return (true, "Department deleted successfully.");
    }

    /// <summary>
    /// Get department hierarchy as a tree structure
    /// </summary>
    public async Task<List<DepartmentHierarchyDto>> GetHierarchyAsync()
    {
        var allDepartments = await _context.Departments
            .Include(d => d.DepartmentHead)
            .Where(d => !d.IsDeleted && d.IsActive)
            .Select(d => new DepartmentHierarchyDto
            {
                Id = d.Id,
                Name = d.Name,
                Code = d.Code,
                ParentDepartmentId = d.ParentDepartmentId,
                DepartmentHeadName = d.DepartmentHead != null
                    ? $"{d.DepartmentHead.FirstName} {d.DepartmentHead.LastName}"
                    : null,
                EmployeeCount = d.Employees.Count(e => !e.IsDeleted && !e.IsOffboarded),
                IsActive = d.IsActive,
                Children = new List<DepartmentHierarchyDto>()
            })
            .ToListAsync();

        // Build tree structure (root departments have no parent)
        var rootDepartments = allDepartments.Where(d => d.ParentDepartmentId == null).ToList();

        foreach (var root in rootDepartments)
        {
            BuildHierarchy(root, allDepartments);
        }

        return rootDepartments;
    }

    /// <summary>
    /// Get simplified list for dropdowns
    /// </summary>
    public async Task<List<DepartmentDropdownDto>> GetDropdownAsync()
    {
        return await _context.Departments
            .Where(d => !d.IsDeleted && d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDropdownDto
            {
                Id = d.Id,
                Name = d.Name,
                Code = d.Code
            })
            .ToListAsync();
    }

    // Helper method to build recursive hierarchy
    private void BuildHierarchy(DepartmentHierarchyDto parent, List<DepartmentHierarchyDto> allDepartments)
    {
        var children = allDepartments.Where(d => d.ParentDepartmentId == parent.Id).ToList();
        parent.Children = children;

        foreach (var child in children)
        {
            BuildHierarchy(child, allDepartments);
        }
    }

    // Helper method to check for circular references
    private async Task<bool> IsCircularReferenceAsync(Guid departmentId, Guid proposedParentId)
    {
        var currentParentId = proposedParentId;

        while (currentParentId != null)
        {
            if (currentParentId == departmentId)
            {
                return true; // Circular reference detected
            }

            var parent = await _context.Departments
                .Where(d => d.Id == currentParentId && !d.IsDeleted)
                .Select(d => d.ParentDepartmentId)
                .FirstOrDefaultAsync();

            currentParentId = parent ?? Guid.Empty;
            if (currentParentId == Guid.Empty) break;
        }

        return false;
    }
}
