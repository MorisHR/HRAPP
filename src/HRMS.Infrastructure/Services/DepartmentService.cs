using HRMS.Application.DTOs.DepartmentDtos;
using HRMS.Application.Interfaces;
using HRMS.Infrastructure.Validators;
using HRMS.Core.Entities.Tenant;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Fortune 500-grade department management service
/// Implements comprehensive validation, caching, performance optimization, and audit logging
/// </summary>
public class DepartmentService : IDepartmentService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<DepartmentService> _logger;
    private readonly IRedisCacheService _cache;
    private readonly DepartmentValidator _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CACHE_KEY_PREFIX = "dept:";
    private const string CACHE_KEY_DROPDOWN = "dept:dropdown";
    private const string CACHE_KEY_HIERARCHY = "dept:hierarchy";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

    public DepartmentService(
        TenantDbContext context,
        ILogger<DepartmentService> logger,
        IRedisCacheService cache,
        DepartmentValidator validator,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
    }

    #region Read Operations

    /// <summary>
    /// Get all departments with optimized query
    /// </summary>
    public async Task<List<DepartmentDto>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all departments");

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
                // Fixed: Use SQL COUNT instead of loading all employees (Issue #18)
                EmployeeCount = _context.Employees
                    .Count(e => e.DepartmentId == d.Id && !e.IsDeleted && !e.IsOffboarded),
                CreatedAt = d.CreatedAt,
                CreatedBy = d.CreatedBy,
                UpdatedAt = d.UpdatedAt,
                UpdatedBy = d.UpdatedBy
            })
            .AsNoTracking()
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} departments", departments.Count);
        return departments;
    }

    /// <summary>
    /// Advanced search with filtering, sorting, and pagination (Issue #19 - Missing search endpoint)
    /// </summary>
    public async Task<DepartmentSearchResultDto> SearchAsync(DepartmentSearchDto searchDto)
    {
        _logger.LogDebug("Searching departments with criteria: {Criteria}", JsonSerializer.Serialize(searchDto));

        // Start with base query
        var query = _context.Departments
            .Include(d => d.ParentDepartment)
            .Include(d => d.DepartmentHead)
            .Where(d => !d.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchDto.Query))
        {
            var searchTerm = searchDto.Query.ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(searchTerm) ||
                d.Code.ToLower().Contains(searchTerm) ||
                (d.Description != null && d.Description.ToLower().Contains(searchTerm)));
        }

        if (searchDto.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == searchDto.IsActive.Value);
        }

        if (searchDto.ParentDepartmentId.HasValue)
        {
            query = query.Where(d => d.ParentDepartmentId == searchDto.ParentDepartmentId.Value);
        }

        if (searchDto.RootOnly == true)
        {
            query = query.Where(d => d.ParentDepartmentId == null);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.CostCenterCode))
        {
            query = query.Where(d => d.CostCenterCode == searchDto.CostCenterCode);
        }

        if (searchDto.HasHead.HasValue)
        {
            if (searchDto.HasHead.Value)
                query = query.Where(d => d.DepartmentHeadId != null);
            else
                query = query.Where(d => d.DepartmentHeadId == null);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        var sortBy = searchDto.SortBy ?? "Name";
        var sortDirection = searchDto.SortDirection?.ToLower() == "desc" ? "desc" : "asc";

        query = sortBy switch
        {
            "Code" => sortDirection == "desc" ? query.OrderByDescending(d => d.Code) : query.OrderBy(d => d.Code),
            "CreatedAt" => sortDirection == "desc" ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
            "UpdatedAt" => sortDirection == "desc" ? query.OrderByDescending(d => d.UpdatedAt) : query.OrderBy(d => d.UpdatedAt),
            _ => sortDirection == "desc" ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name)
        };

        // Apply pagination
        var pageNumber = searchDto.PageNumber > 0 ? searchDto.PageNumber : 1;
        var pageSize = Math.Min(searchDto.PageSize, 100); // Cap at 100

        var departments = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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
                EmployeeCount = _context.Employees
                    .Count(e => e.DepartmentId == d.Id && !e.IsDeleted && !e.IsOffboarded),
                CreatedAt = d.CreatedAt,
                CreatedBy = d.CreatedBy,
                UpdatedAt = d.UpdatedAt,
                UpdatedBy = d.UpdatedBy
            })
            .AsNoTracking()
            .ToListAsync();

        // Apply employee count filters (post-query since it's a computed field)
        if (searchDto.MinEmployeeCount.HasValue || searchDto.MaxEmployeeCount.HasValue)
        {
            if (searchDto.MinEmployeeCount.HasValue)
                departments = departments.Where(d => d.EmployeeCount >= searchDto.MinEmployeeCount.Value).ToList();

            if (searchDto.MaxEmployeeCount.HasValue)
                departments = departments.Where(d => d.EmployeeCount <= searchDto.MaxEmployeeCount.Value).ToList();
        }

        _logger.LogInformation("Search returned {Count} of {Total} departments", departments.Count, totalCount);

        return new DepartmentSearchResultDto
        {
            Departments = departments,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Get department by ID with caching
    /// </summary>
    public async Task<DepartmentDto?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}{id}";

        // Try cache first
        var cached = await _cache.GetAsync<DepartmentDto>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Department {Id} retrieved from cache", id);
            return cached;
        }

        _logger.LogDebug("Fetching department {Id} from database", id);

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
                EmployeeCount = _context.Employees
                    .Count(e => e.DepartmentId == d.Id && !e.IsDeleted && !e.IsOffboarded),
                CreatedAt = d.CreatedAt,
                CreatedBy = d.CreatedBy,
                UpdatedAt = d.UpdatedAt,
                UpdatedBy = d.UpdatedBy
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        // Cache the result
        if (department != null)
        {
            await _cache.SetAsync(cacheKey, department, CacheExpiration);
        }

        return department;
    }

    /// <summary>
    /// Get detailed department information with audit trail (Issue #36)
    /// </summary>
    public async Task<DepartmentDetailDto?> GetDetailAsync(Guid id)
    {
        _logger.LogDebug("Fetching detailed information for department {Id}", id);

        var department = await _context.Departments
            .Include(d => d.ParentDepartment)
            .Include(d => d.DepartmentHead)
            .Include(d => d.SubDepartments.Where(sd => !sd.IsDeleted))
            .Where(d => d.Id == id && !d.IsDeleted)
            .FirstOrDefaultAsync();

        if (department == null)
            return null;

        // Build hierarchy path
        var hierarchyPath = await BuildHierarchyPathAsync(id);

        var detail = new DepartmentDetailDto
        {
            Id = department.Id,
            Name = department.Name,
            Code = department.Code,
            Description = department.Description,
            ParentDepartmentId = department.ParentDepartmentId,
            ParentDepartmentName = department.ParentDepartment?.Name,
            DepartmentHeadId = department.DepartmentHeadId,
            DepartmentHeadName = department.DepartmentHead != null
                ? $"{department.DepartmentHead.FirstName} {department.DepartmentHead.LastName}"
                : null,
            CostCenterCode = department.CostCenterCode,
            IsActive = department.IsActive,
            EmployeeCount = await _context.Employees
                .CountAsync(e => e.DepartmentId == id && !e.IsDeleted && !e.IsOffboarded),
            CreatedAt = department.CreatedAt,
            CreatedBy = department.CreatedBy,
            UpdatedAt = department.UpdatedAt,
            UpdatedBy = department.UpdatedBy,
            HierarchyPath = hierarchyPath,
            HierarchyLevel = hierarchyPath.Split('>').Length - 1,
            SubDepartments = department.SubDepartments.Select(sd => new DepartmentDto
            {
                Id = sd.Id,
                Name = sd.Name,
                Code = sd.Code,
                IsActive = sd.IsActive
            }).ToList(),
            ActiveEmployeeCount = await _context.Employees
                .CountAsync(e => e.DepartmentId == id && !e.IsDeleted && !e.IsOffboarded && e.IsActive),
            InactiveEmployeeCount = await _context.Employees
                .CountAsync(e => e.DepartmentId == id && !e.IsDeleted && !e.IsOffboarded && !e.IsActive)
        };

        return detail;
    }

    #endregion

    #region Create/Update Operations

    /// <summary>
    /// Create department with comprehensive validation and audit logging
    /// </summary>
    public async Task<(bool Success, string Message, Guid? DepartmentId)> CreateAsync(
        CreateDepartmentDto dto, string createdBy)
    {
        _logger.LogInformation("Creating department with code {Code} by {User}", dto.Code, createdBy);

        // Comprehensive validation
        var validationResult = await _validator.ValidateCreateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors);
            _logger.LogWarning("Department creation validation failed: {Errors}", errors);
            return (false, errors, null);
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Code = dto.Code.ToUpper().Trim(),
            Description = dto.Description?.Trim(),
            ParentDepartmentId = dto.ParentDepartmentId,
            DepartmentHeadId = dto.DepartmentHeadId,
            CostCenterCode = dto.CostCenterCode?.ToUpper().Trim(),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            IsDeleted = false
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        // Invalidate caches
        await InvalidateCachesAsync();

        // Log activity for audit trail
        await LogActivityAsync(department.Id, "Created", $"Department '{department.Name}' ({department.Code}) created", null, null, createdBy);

        _logger.LogInformation("Department created successfully: {Id} - {Code} - {Name}", department.Id, department.Code, department.Name);

        return (true, "Department created successfully.", department.Id);
    }

    /// <summary>
    /// Update department with comprehensive validation
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAsync(
        Guid id, UpdateDepartmentDto dto, string updatedBy)
    {
        _logger.LogInformation("Updating department {Id} by {User}", id, updatedBy);

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (department == null)
        {
            _logger.LogWarning("Department {Id} not found for update", id);
            return (false, "Department not found.");
        }

        // Comprehensive validation
        var validationResult = await _validator.ValidateUpdateAsync(id, dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors);
            _logger.LogWarning("Department update validation failed for {Id}: {Errors}", id, errors);
            return (false, errors);
        }

        // Track changes for audit log
        var changes = new List<string>();
        if (department.Name != dto.Name)
            changes.Add($"Name: '{department.Name}' → '{dto.Name}'");
        if (department.Code != dto.Code.ToUpper())
            changes.Add($"Code: '{department.Code}' → '{dto.Code.ToUpper()}'");
        if (department.IsActive != dto.IsActive)
        {
            changes.Add($"Status: {(department.IsActive ? "Active" : "Inactive")} → {(dto.IsActive ? "Active" : "Inactive")}");

            // Track deactivation (Issue #7 - Audit trail for status changes)
            if (!dto.IsActive && department.IsActive)
            {
                await LogActivityAsync(id, "Deactivated", $"Department deactivated", null, null, updatedBy);
            }
        }

        // Update fields
        department.Name = dto.Name.Trim();
        department.Code = dto.Code.ToUpper().Trim();
        department.Description = dto.Description?.Trim();
        department.ParentDepartmentId = dto.ParentDepartmentId;
        department.DepartmentHeadId = dto.DepartmentHeadId;
        department.CostCenterCode = dto.CostCenterCode?.ToUpper().Trim();
        department.IsActive = dto.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync();

        // Invalidate caches
        await InvalidateCachesAsync();
        await _cache.RemoveAsync($"{CACHE_KEY_PREFIX}{id}");

        // Log activity
        if (changes.Any())
        {
            await LogActivityAsync(id, "Updated", string.Join(", ", changes), null, null, updatedBy);
        }

        _logger.LogInformation("Department updated successfully: {Id} - {Code}", id, department.Code);

        return (true, "Department updated successfully.");
    }

    /// <summary>
    /// Bulk update department status (Issue #20 - Bulk operations)
    /// </summary>
    public async Task<(bool Success, string Message, int AffectedCount)> BulkUpdateStatusAsync(
        BulkDepartmentStatusDto dto, string updatedBy)
    {
        _logger.LogInformation("Bulk updating status for {Count} departments by {User}", dto.DepartmentIds.Count, updatedBy);

        var departments = await _context.Departments
            .Where(d => dto.DepartmentIds.Contains(d.Id) && !d.IsDeleted)
            .ToListAsync();

        if (!departments.Any())
        {
            return (false, "No departments found with the provided IDs", 0);
        }

        var affectedCount = 0;
        foreach (var dept in departments)
        {
            if (dept.IsActive != dto.IsActive)
            {
                dept.IsActive = dto.IsActive;
                dept.UpdatedAt = DateTime.UtcNow;
                dept.UpdatedBy = updatedBy;
                affectedCount++;

                // Log activity
                var action = dto.IsActive ? "Activated" : "Deactivated";
                var description = $"Department {action} (Bulk Operation)";
                if (!string.IsNullOrWhiteSpace(dto.Reason))
                {
                    description += $" - Reason: {dto.Reason}";
                }
                await LogActivityAsync(dept.Id, action, description, null, null, updatedBy);
            }
        }

        if (affectedCount > 0)
        {
            await _context.SaveChangesAsync();
            await InvalidateCachesAsync();
        }

        _logger.LogInformation("Bulk status update completed: {Affected} of {Total} departments updated",
            affectedCount, departments.Count);

        return (true, $"Successfully updated {affectedCount} department(s)", affectedCount);
    }

    #endregion

    #region Delete Operations

    /// <summary>
    /// Delete department with enhanced validation
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAsync(Guid id, string deletedBy)
    {
        _logger.LogInformation("Deleting department {Id} by {User}", id, deletedBy);

        // Comprehensive validation
        var validationResult = await _validator.ValidateDeleteAsync(id);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors);
            _logger.LogWarning("Department deletion validation failed for {Id}: {Errors}", id, errors);
            return (false, errors);
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (department == null)
        {
            return (false, "Department not found.");
        }

        department.IsDeleted = true;
        department.DeletedAt = DateTime.UtcNow;
        department.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();

        // Invalidate caches
        await InvalidateCachesAsync();
        await _cache.RemoveAsync($"{CACHE_KEY_PREFIX}{id}");

        // Log activity
        await LogActivityAsync(id, "Deleted", $"Department '{department.Name}' ({department.Code}) deleted", null, null, deletedBy);

        _logger.LogInformation("Department deleted successfully: {Id} - {Code}", id, department.Code);

        return (true, "Department deleted successfully.");
    }

    /// <summary>
    /// Bulk delete departments (Issue #20)
    /// </summary>
    public async Task<(bool Success, string Message, int DeletedCount, List<string> Errors)> BulkDeleteAsync(
        List<Guid> departmentIds, string deletedBy)
    {
        _logger.LogInformation("Bulk deleting {Count} departments by {User}", departmentIds.Count, deletedBy);

        var deletedCount = 0;
        var errors = new List<string>();

        foreach (var id in departmentIds)
        {
            var result = await DeleteAsync(id, deletedBy);
            if (result.Success)
            {
                deletedCount++;
            }
            else
            {
                var dept = await _context.Departments.Where(d => d.Id == id).Select(d => d.Name).FirstOrDefaultAsync();
                errors.Add($"{dept ?? id.ToString()}: {result.Message}");
            }
        }

        var message = deletedCount > 0
            ? $"Successfully deleted {deletedCount} of {departmentIds.Count} department(s)"
            : "No departments were deleted";

        _logger.LogInformation("Bulk delete completed: {Deleted} of {Total} departments deleted",
            deletedCount, departmentIds.Count);

        return (deletedCount > 0, message, deletedCount, errors);
    }

    #endregion

    #region Advanced Operations

    /// <summary>
    /// Merge two departments - Enterprise feature (Issue #31)
    /// </summary>
    public async Task<(bool Success, string Message, DepartmentMergeResultDto? Result)> MergeDepartmentsAsync(
        DepartmentMergeDto dto, string performedBy)
    {
        _logger.LogInformation("Merging department {Source} into {Target} by {User}",
            dto.SourceDepartmentId, dto.TargetDepartmentId, performedBy);

        // Validate merge
        var validationResult = await _validator.ValidateMergeAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors);
            _logger.LogWarning("Department merge validation failed: {Errors}", errors);
            return (false, errors, null);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var source = await _context.Departments
                .Include(d => d.Employees)
                .Include(d => d.SubDepartments)
                .FirstOrDefaultAsync(d => d.Id == dto.SourceDepartmentId && !d.IsDeleted);

            var target = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == dto.TargetDepartmentId && !d.IsDeleted);

            if (source == null || target == null)
            {
                return (false, "Source or target department not found", null);
            }

            // Move employees
            var employeesMoved = 0;
            foreach (var employee in source.Employees.Where(e => !e.IsDeleted && !e.IsOffboarded))
            {
                employee.DepartmentId = dto.TargetDepartmentId;
                employeesMoved++;
            }

            // Move sub-departments
            var subDeptsMoved = 0;
            foreach (var subDept in source.SubDepartments.Where(d => !d.IsDeleted))
            {
                subDept.ParentDepartmentId = dto.TargetDepartmentId;
                subDeptsMoved++;
            }

            // Handle source department
            if (dto.DeleteSource)
            {
                source.IsDeleted = true;
                source.DeletedAt = DateTime.UtcNow;
                source.DeletedBy = performedBy;
            }
            else
            {
                source.IsActive = false;
                source.UpdatedAt = DateTime.UtcNow;
                source.UpdatedBy = performedBy;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Log activity
            await LogActivityAsync(dto.SourceDepartmentId, "Merged",
                $"Department merged into '{target.Name}' - {employeesMoved} employees, {subDeptsMoved} sub-departments moved",
                null, null, performedBy);

            await LogActivityAsync(dto.TargetDepartmentId, "MergeTarget",
                $"Received merge from '{source.Name}' - {employeesMoved} employees, {subDeptsMoved} sub-departments received",
                null, null, performedBy);

            // Invalidate caches
            await InvalidateCachesAsync();

            var result = new DepartmentMergeResultDto
            {
                SourceDepartmentId = source.Id,
                SourceDepartmentName = source.Name,
                TargetDepartmentId = target.Id,
                TargetDepartmentName = target.Name,
                EmployeesMoved = employeesMoved,
                SubDepartmentsMoved = subDeptsMoved,
                SourceDeleted = dto.DeleteSource,
                MergedAt = DateTime.UtcNow,
                MergedBy = performedBy
            };

            _logger.LogInformation("Department merge completed successfully: {Source} → {Target}",
                source.Code, target.Code);

            return (true, "Departments merged successfully", result);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error merging departments {Source} → {Target}",
                dto.SourceDepartmentId, dto.TargetDepartmentId);
            return (false, $"Error merging departments: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Reassign employees between departments (Issue #8 - Helper for deletion)
    /// </summary>
    public async Task<(bool Success, string Message, int ReassignedCount)> ReassignEmployeesAsync(
        Guid fromDepartmentId, Guid toDepartmentId, string performedBy)
    {
        _logger.LogInformation("Reassigning employees from {From} to {To} by {User}",
            fromDepartmentId, toDepartmentId, performedBy);

        var fromDept = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == fromDepartmentId && !d.IsDeleted);

        var toDept = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == toDepartmentId && !d.IsDeleted && d.IsActive);

        if (fromDept == null)
            return (false, "Source department not found", 0);

        if (toDept == null)
            return (false, "Target department not found or is inactive", 0);

        var employees = await _context.Employees
            .Where(e => e.DepartmentId == fromDepartmentId && !e.IsDeleted && !e.IsOffboarded)
            .ToListAsync();

        foreach (var employee in employees)
        {
            employee.DepartmentId = toDepartmentId;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reassigned {Count} employees from {From} to {To}",
            employees.Count, fromDept.Code, toDept.Code);

        return (true, $"Successfully reassigned {employees.Count} employee(s)", employees.Count);
    }

    #endregion

    #region Hierarchy & Dropdown

    /// <summary>
    /// Get department hierarchy with role-based filtering (Issue #12 - Security)
    /// Fixed: Performance optimization using efficient query (Issue #16)
    /// </summary>
    public async Task<List<DepartmentHierarchyDto>> GetHierarchyAsync(Guid? userId = null, string? userRole = null)
    {
        _logger.LogDebug("Fetching department hierarchy for user {UserId} with role {Role}", userId, userRole);

        // Try cache first (only for full hierarchy without user filtering)
        if (userId == null && userRole == null)
        {
            var cached = await _cache.GetAsync<List<DepartmentHierarchyDto>>(CACHE_KEY_HIERARCHY);
            if (cached != null)
            {
                _logger.LogDebug("Department hierarchy retrieved from cache");
                return cached;
            }
        }

        // Optimized query - load all departments once
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
                EmployeeCount = _context.Employees
                    .Count(e => e.DepartmentId == d.Id && !e.IsDeleted && !e.IsOffboarded),
                IsActive = d.IsActive,
                Children = new List<DepartmentHierarchyDto>()
            })
            .AsNoTracking()
            .ToListAsync();

        // Role-based filtering (Issue #12)
        if (userRole == "Manager" && userId.HasValue)
        {
            // Managers only see their department and sub-departments
            var managerDept = await _context.Employees
                .Where(e => e.Id == userId.Value && !e.IsDeleted)
                .Select(e => e.DepartmentId)
                .FirstOrDefaultAsync();

            if (managerDept != Guid.Empty)
            {
                var allowedDeptIds = new HashSet<Guid> { managerDept };
                CollectSubDepartmentIds(managerDept, allDepartments, allowedDeptIds);
                allDepartments = allDepartments.Where(d => allowedDeptIds.Contains(d.Id)).ToList();
            }
        }

        // Build tree structure
        var rootDepartments = allDepartments.Where(d => d.ParentDepartmentId == null).ToList();
        foreach (var root in rootDepartments)
        {
            BuildHierarchy(root, allDepartments);
        }

        // Cache if full hierarchy
        if (userId == null && userRole == null)
        {
            await _cache.SetAsync(CACHE_KEY_HIERARCHY, rootDepartments, CacheExpiration);
        }

        return rootDepartments;
    }

    /// <summary>
    /// Get dropdown list with caching (Issue #17 - Missing cache)
    /// </summary>
    public async Task<List<DepartmentDropdownDto>> GetDropdownAsync(bool activeOnly = true)
    {
        var cacheKey = $"{CACHE_KEY_DROPDOWN}:{activeOnly}";

        // Try cache first
        var cached = await _cache.GetAsync<List<DepartmentDropdownDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Department dropdown retrieved from cache");
            return cached;
        }

        _logger.LogDebug("Fetching department dropdown from database");

        var query = _context.Departments.Where(d => !d.IsDeleted);

        if (activeOnly)
        {
            query = query.Where(d => d.IsActive);
        }

        var dropdown = await query
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDropdownDto
            {
                Id = d.Id,
                Name = d.Name,
                Code = d.Code
            })
            .AsNoTracking()
            .ToListAsync();

        // Cache result
        await _cache.SetAsync(cacheKey, dropdown, CacheExpiration);

        return dropdown;
    }

    #endregion

    #region Audit & Validation Helpers

    /// <summary>
    /// Get department activity history (Issue #32)
    /// </summary>
    public async Task<List<DepartmentActivityDto>> GetActivityHistoryAsync(Guid departmentId)
    {
        _logger.LogDebug("Fetching activity history for department {Id}", departmentId);

        var activities = await _context.DepartmentAuditLogs
            .Where(log => log.DepartmentId == departmentId)
            .OrderByDescending(log => log.PerformedAt)
            .Select(log => new DepartmentActivityDto
            {
                Id = log.Id,
                DepartmentId = log.DepartmentId,
                ActivityType = log.ActivityType,
                Description = log.Description,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                PerformedBy = log.PerformedBy,
                PerformedAt = log.PerformedAt,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent
            })
            .AsNoTracking()
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} activity records for department {Id}", activities.Count, departmentId);

        return activities;
    }

    /// <summary>
    /// Check if department code is available
    /// </summary>
    public async Task<bool> IsCodeAvailableAsync(string code, Guid? excludeDepartmentId = null)
    {
        var query = _context.Departments
            .Where(d => d.Code == code.ToUpper() && !d.IsDeleted);

        if (excludeDepartmentId.HasValue)
        {
            query = query.Where(d => d.Id != excludeDepartmentId.Value);
        }

        return !await query.AnyAsync();
    }

    /// <summary>
    /// Check if employee is already a department head (Issue #2)
    /// </summary>
    public async Task<(bool IsHead, string? DepartmentName)> IsEmployeeDepartmentHeadAsync(Guid employeeId)
    {
        var department = await _context.Departments
            .Where(d => d.DepartmentHeadId == employeeId && !d.IsDeleted)
            .Select(d => d.Name)
            .FirstOrDefaultAsync();

        return (department != null, department);
    }

    /// <summary>
    /// Get departments by employee (shows full hierarchy path)
    /// </summary>
    public async Task<List<DepartmentDto>> GetDepartmentsByEmployeeAsync(Guid employeeId)
    {
        var employeeDeptId = await _context.Employees
            .Where(e => e.Id == employeeId && !e.IsDeleted)
            .Select(e => e.DepartmentId)
            .FirstOrDefaultAsync();

        if (employeeDeptId == Guid.Empty)
            return new List<DepartmentDto>();

        var departments = new List<DepartmentDto>();
        var currentDeptId = employeeDeptId;

        // Walk up the hierarchy
        while (currentDeptId != Guid.Empty)
        {
            var dept = await GetByIdAsync(currentDeptId);
            if (dept == null) break;

            departments.Insert(0, dept); // Add to beginning
            currentDeptId = dept.ParentDepartmentId ?? Guid.Empty;
        }

        return departments;
    }

    /// <summary>
    /// Validate department business rules
    /// </summary>
    public async Task<(bool IsValid, List<string> Errors)> ValidateDepartmentRulesAsync(
        CreateDepartmentDto dto, Guid? departmentId = null)
    {
        if (departmentId.HasValue)
        {
            var updateDto = new UpdateDepartmentDto
            {
                Name = dto.Name,
                Code = dto.Code,
                Description = dto.Description,
                ParentDepartmentId = dto.ParentDepartmentId,
                DepartmentHeadId = dto.DepartmentHeadId,
                CostCenterCode = dto.CostCenterCode,
                IsActive = dto.IsActive
            };
            return await _validator.ValidateUpdateAsync(departmentId.Value, updateDto);
        }

        return await _validator.ValidateCreateAsync(dto);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Build hierarchy recursively
    /// </summary>
    private void BuildHierarchy(DepartmentHierarchyDto parent, List<DepartmentHierarchyDto> allDepartments)
    {
        var children = allDepartments.Where(d => d.ParentDepartmentId == parent.Id).ToList();
        parent.Children = children;

        foreach (var child in children)
        {
            BuildHierarchy(child, allDepartments);
        }
    }

    /// <summary>
    /// Collect all sub-department IDs recursively
    /// </summary>
    private void CollectSubDepartmentIds(Guid parentId, List<DepartmentHierarchyDto> allDepartments, HashSet<Guid> result)
    {
        var children = allDepartments.Where(d => d.ParentDepartmentId == parentId).ToList();
        foreach (var child in children)
        {
            result.Add(child.Id);
            CollectSubDepartmentIds(child.Id, allDepartments, result);
        }
    }

    /// <summary>
    /// Build full hierarchy path for a department
    /// </summary>
    private async Task<string> BuildHierarchyPathAsync(Guid departmentId)
    {
        var path = new List<string>();
        var currentId = departmentId;

        while (currentId != Guid.Empty)
        {
            var dept = await _context.Departments
                .Where(d => d.Id == currentId && !d.IsDeleted)
                .Select(d => new { d.Name, d.ParentDepartmentId })
                .FirstOrDefaultAsync();

            if (dept == null) break;

            path.Insert(0, dept.Name);
            currentId = dept.ParentDepartmentId ?? Guid.Empty;
        }

        return string.Join(" > ", path);
    }

    /// <summary>
    /// Invalidate all department caches
    /// </summary>
    private async Task InvalidateCachesAsync()
    {
        await _cache.RemoveAsync(CACHE_KEY_DROPDOWN);
        await _cache.RemoveAsync($"{CACHE_KEY_DROPDOWN}:true");
        await _cache.RemoveAsync($"{CACHE_KEY_DROPDOWN}:false");
        await _cache.RemoveAsync(CACHE_KEY_HIERARCHY);
    }

    /// <summary>
    /// Log department activity for audit trail
    /// </summary>
    private async Task LogActivityAsync(Guid departmentId, string activityType, string description,
        string? oldValue, string? newValue, string performedBy)
    {
        try
        {
            // Capture IP address and User Agent from HttpContext for forensic audit trail
            string? ipAddress = null;
            string? userAgent = null;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                // Get IP address (handles proxy scenarios with X-Forwarded-For)
                ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                if (string.IsNullOrEmpty(ipAddress) && httpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
                {
                    ipAddress = httpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',').FirstOrDefault()?.Trim();
                }

                // Get User Agent
                if (httpContext.Request.Headers.ContainsKey("User-Agent"))
                {
                    userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                    // Truncate to max length (500 chars as per DB schema)
                    if (userAgent?.Length > 500)
                    {
                        userAgent = userAgent.Substring(0, 500);
                    }
                }
            }

            var auditLog = new DepartmentAuditLog
            {
                Id = Guid.NewGuid(),
                DepartmentId = departmentId,
                ActivityType = activityType,
                Description = description,
                OldValue = oldValue,
                NewValue = newValue,
                PerformedBy = performedBy,
                PerformedAt = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.DepartmentAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Department Activity Logged: {DepartmentId} | {Type} | {Description} | By: {User}",
                departmentId, activityType, description, performedBy);
        }
        catch (Exception ex)
        {
            // Don't fail the operation if audit logging fails
            _logger.LogError(ex, "Failed to log department activity for {DepartmentId}", departmentId);
        }
    }

    #endregion
}
