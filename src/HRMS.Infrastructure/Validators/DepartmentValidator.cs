using HRMS.Application.DTOs.DepartmentDtos;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Validators;

/// <summary>
/// Comprehensive business rule validator for department operations
/// Implements Fortune 500-grade validation logic
/// </summary>
public class DepartmentValidator
{
    private readonly TenantDbContext _context;
    private readonly ILogger<DepartmentValidator> _logger;

    public DepartmentValidator(TenantDbContext context, ILogger<DepartmentValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Validate department creation rules
    /// </summary>
    public async Task<(bool IsValid, List<string> Errors)> ValidateCreateAsync(CreateDepartmentDto dto)
    {
        var errors = new List<string>();

        // Validate code uniqueness with detailed error
        var existingDept = await _context.Departments
            .Where(d => d.Code == dto.Code.ToUpper() && !d.IsDeleted)
            .Select(d => new { d.Code, d.Name, d.CreatedBy, d.CreatedAt })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (existingDept != null)
        {
            errors.Add($"Department code '{dto.Code}' already exists (Department: '{existingDept.Name}', Created: {existingDept.CreatedAt:yyyy-MM-dd})");
        }

        // Validate name duplication (soft warning converted to error)
        var duplicateName = await _context.Departments
            .Where(d => d.Name == dto.Name && !d.IsDeleted)
            .AsNoTracking()
            .AnyAsync();

        if (duplicateName)
        {
            errors.Add($"A department with name '{dto.Name}' already exists. Consider using a different name.");
        }

        // Validate parent department
        if (dto.ParentDepartmentId.HasValue)
        {
            var parentValid = await ValidateParentDepartmentAsync(dto.ParentDepartmentId.Value, null);
            if (!parentValid.IsValid)
            {
                errors.AddRange(parentValid.Errors);
            }
        }

        // Validate department head
        if (dto.DepartmentHeadId.HasValue)
        {
            var headValid = await ValidateDepartmentHeadAsync(dto.DepartmentHeadId.Value, dto.ParentDepartmentId, null);
            if (!headValid.IsValid)
            {
                errors.AddRange(headValid.Errors);
            }
        }

        // Validate cost center code format if provided
        if (!string.IsNullOrWhiteSpace(dto.CostCenterCode))
        {
            if (dto.CostCenterCode.Length < 3)
            {
                errors.Add("Cost center code must be at least 3 characters long");
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Validate department update rules
    /// </summary>
    public async Task<(bool IsValid, List<string> Errors)> ValidateUpdateAsync(Guid departmentId, UpdateDepartmentDto dto)
    {
        var errors = new List<string>();

        // Check department exists
        var exists = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d => d.Id == departmentId && !d.IsDeleted);

        if (!exists)
        {
            errors.Add("Department not found");
            return (false, errors);
        }

        // Validate code uniqueness (excluding current department)
        var existingDept = await _context.Departments
            .Where(d => d.Code == dto.Code.ToUpper() && d.Id != departmentId && !d.IsDeleted)
            .Select(d => new { d.Code, d.Name, d.CreatedAt })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (existingDept != null)
        {
            errors.Add($"Department code '{dto.Code}' already exists (Department: '{existingDept.Name}', Created: {existingDept.CreatedAt:yyyy-MM-dd})");
        }

        // Validate parent department and circular reference
        if (dto.ParentDepartmentId.HasValue)
        {
            // Self-reference check
            if (dto.ParentDepartmentId.Value == departmentId)
            {
                errors.Add("A department cannot be its own parent");
            }
            else
            {
                var parentValid = await ValidateParentDepartmentAsync(dto.ParentDepartmentId.Value, departmentId);
                if (!parentValid.IsValid)
                {
                    errors.AddRange(parentValid.Errors);
                }

                // Circular reference check
                var circularCheck = await CheckCircularReferenceAsync(departmentId, dto.ParentDepartmentId.Value);
                if (!circularCheck.IsValid)
                {
                    errors.AddRange(circularCheck.Errors);
                }
            }
        }

        // Validate department head
        if (dto.DepartmentHeadId.HasValue)
        {
            var headValid = await ValidateDepartmentHeadAsync(dto.DepartmentHeadId.Value, dto.ParentDepartmentId, departmentId);
            if (!headValid.IsValid)
            {
                errors.AddRange(headValid.Errors);
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Validate department deletion (OPTIMIZED: No N+1 queries)
    /// </summary>
    public async Task<(bool IsValid, List<string> Errors)> ValidateDeleteAsync(Guid departmentId)
    {
        var errors = new List<string>();

        // Check if department exists
        var deptExists = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d => d.Id == departmentId && !d.IsDeleted);

        if (!deptExists)
        {
            errors.Add("Department not found");
            return (false, errors);
        }

        // OPTIMIZED: Use SQL COUNT instead of loading all employees
        var activeEmployeeCount = await _context.Employees
            .AsNoTracking()
            .CountAsync(e => e.DepartmentId == departmentId && !e.IsDeleted && !e.IsOffboarded);

        if (activeEmployeeCount > 0)
        {
            // Get sample employee names for better error message
            var sampleEmployees = await _context.Employees
                .Where(e => e.DepartmentId == departmentId && !e.IsDeleted && !e.IsOffboarded)
                .Select(e => new { e.FirstName, e.LastName })
                .AsNoTracking()
                .Take(5)
                .ToListAsync();

            var employeeNames = string.Join(", ", sampleEmployees.Select(e => $"{e.FirstName} {e.LastName}"));
            var remaining = activeEmployeeCount - 5;
            var message = activeEmployeeCount <= 5
                ? $"Cannot delete department. Active employees: {employeeNames}"
                : $"Cannot delete department. {activeEmployeeCount} active employees including: {employeeNames} and {remaining} more";

            errors.Add(message);
        }

        // OPTIMIZED: Use SQL COUNT for sub-departments
        var subDeptCount = await _context.Departments
            .AsNoTracking()
            .CountAsync(d => d.ParentDepartmentId == departmentId && !d.IsDeleted);

        if (subDeptCount > 0)
        {
            // Get sample sub-department names
            var sampleSubDepts = await _context.Departments
                .Where(d => d.ParentDepartmentId == departmentId && !d.IsDeleted)
                .Select(d => d.Name)
                .AsNoTracking()
                .Take(3)
                .ToListAsync();

            var subDeptNames = string.Join(", ", sampleSubDepts);
            var remaining = subDeptCount - 3;
            var message = subDeptCount <= 3
                ? $"Cannot delete department. Sub-departments: {subDeptNames}"
                : $"Cannot delete department. {subDeptCount} sub-departments including: {subDeptNames} and {remaining} more";

            errors.Add(message);
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Validate parent department exists and is active
    /// </summary>
    private async Task<(bool IsValid, List<string> Errors)> ValidateParentDepartmentAsync(Guid parentId, Guid? currentDeptId)
    {
        var errors = new List<string>();

        var parent = await _context.Departments
            .Where(d => d.Id == parentId && !d.IsDeleted)
            .Select(d => new { d.Id, d.Name, d.IsActive })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (parent == null)
        {
            errors.Add("Parent department not found or has been deleted");
        }
        else if (!parent.IsActive)
        {
            errors.Add($"Parent department '{parent.Name}' is inactive. Cannot assign inactive department as parent.");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Validate department head with comprehensive business rules
    /// </summary>
    private async Task<(bool IsValid, List<string> Errors)> ValidateDepartmentHeadAsync(
        Guid employeeId, Guid? assignedDepartmentId, Guid? currentDeptId)
    {
        var errors = new List<string>();

        // Check employee exists and is active
        var employee = await _context.Employees
            .Where(e => e.Id == employeeId && !e.IsDeleted)
            .Select(e => new {
                e.Id,
                e.FirstName,
                e.LastName,
                e.IsOffboarded,
                e.DepartmentId,
                e.IsActive
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (employee == null)
        {
            errors.Add("Employee not found or has been deleted");
            return (false, errors);
        }

        if (employee.IsOffboarded)
        {
            errors.Add($"Employee '{employee.FirstName} {employee.LastName}' is offboarded and cannot be assigned as department head");
        }

        if (!employee.IsActive)
        {
            errors.Add($"Employee '{employee.FirstName} {employee.LastName}' is inactive and cannot be assigned as department head");
        }

        // Check if employee is already a department head elsewhere (CRITICAL: Issue #2)
        var existingHeadAssignment = await _context.Departments
            .Where(d => d.DepartmentHeadId == employeeId && !d.IsDeleted)
            .Where(d => !currentDeptId.HasValue || d.Id != currentDeptId.Value)
            .Select(d => new { d.Name, d.Code })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (existingHeadAssignment != null)
        {
            errors.Add($"Employee '{employee.FirstName} {employee.LastName}' is already head of department '{existingHeadAssignment.Name}' ({existingHeadAssignment.Code}). " +
                      "An employee can only be head of one department at a time.");
        }

        // Optional: Validate that department head works in the same department or parent department (CRITICAL: Issue #3)
        // This is a soft validation - some organizations allow cross-department heads
        if (assignedDepartmentId.HasValue && employee.DepartmentId != Guid.Empty && employee.DepartmentId != assignedDepartmentId.Value)
        {
            var employeeDept = await _context.Departments
                .Where(d => d.Id == employee.DepartmentId)
                .Select(d => d.Name)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var targetDept = await _context.Departments
                .Where(d => d.Id == assignedDepartmentId.Value)
                .Select(d => d.Name)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            // Log warning but don't block (can be made strict by uncommenting the error)
            _logger.LogWarning(
                "Department head {EmployeeName} works in '{EmployeeDept}' but is being assigned as head of '{TargetDept}'",
                $"{employee.FirstName} {employee.LastName}", employeeDept, targetDept);

            // Uncomment to enforce strict rule:
            // errors.Add($"Employee works in '{employeeDept}' but cannot be head of '{targetDept}'. Department head must work in the same department.");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Check for circular references in department hierarchy (OPTIMIZED: Single query)
    /// CRITICAL: Issue #4 - Prevents infinite loops
    /// </summary>
    private async Task<(bool IsValid, List<string> Errors)> CheckCircularReferenceAsync(Guid departmentId, Guid proposedParentId)
    {
        var errors = new List<string>();
        var maxDepth = 50; // Prevent infinite loops

        // OPTIMIZED: Load entire hierarchy path in ONE query instead of N queries
        var hierarchyMap = await _context.Departments
            .Where(d => !d.IsDeleted)
            .Select(d => new { d.Id, d.ParentDepartmentId })
            .AsNoTracking()
            .ToDictionaryAsync(d => d.Id, d => d.ParentDepartmentId);

        // Check if proposed parent exists
        if (!hierarchyMap.ContainsKey(proposedParentId))
        {
            errors.Add("Proposed parent department not found");
            return (false, errors);
        }

        // Walk up the hierarchy using in-memory map (no DB calls in loop!)
        var visited = new HashSet<Guid>();
        var currentId = proposedParentId;
        var depth = 0;

        while (currentId != Guid.Empty && hierarchyMap.ContainsKey(currentId) && depth < maxDepth)
        {
            // Circular reference detected
            if (currentId == departmentId)
            {
                errors.Add("This would create a circular reference in the department hierarchy. " +
                          "A department cannot have itself as an ancestor.");
                return (false, errors);
            }

            // Check for duplicate visits (loop detection)
            if (visited.Contains(currentId))
            {
                errors.Add("Department hierarchy contains a loop. Please contact system administrator.");
                _logger.LogError("Circular reference loop detected in department hierarchy at {DepartmentId}", currentId);
                return (false, errors);
            }

            visited.Add(currentId);

            // Get parent from in-memory map
            currentId = hierarchyMap[currentId] ?? Guid.Empty;
            depth++;
        }

        if (depth >= maxDepth)
        {
            errors.Add($"Department hierarchy depth exceeds maximum allowed depth of {maxDepth} levels");
            _logger.LogWarning("Department hierarchy depth exceeded at {DepartmentId}", departmentId);
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Validate department merge operation
    /// </summary>
    public async Task<(bool IsValid, List<string> Errors)> ValidateMergeAsync(DepartmentMergeDto dto)
    {
        var errors = new List<string>();

        if (dto.SourceDepartmentId == dto.TargetDepartmentId)
        {
            errors.Add("Source and target departments must be different");
            return (false, errors);
        }

        var source = await _context.Departments
            .Where(d => d.Id == dto.SourceDepartmentId && !d.IsDeleted)
            .Select(d => new { d.Id, d.Name, d.IsActive })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (source == null)
        {
            errors.Add("Source department not found");
        }

        var target = await _context.Departments
            .Where(d => d.Id == dto.TargetDepartmentId && !d.IsDeleted)
            .Select(d => new { d.Id, d.Name, d.IsActive })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (target == null)
        {
            errors.Add("Target department not found");
        }
        else if (!target.IsActive)
        {
            errors.Add($"Target department '{target.Name}' is inactive. Cannot merge into inactive department.");
        }

        return (errors.Count == 0, errors);
    }
}
