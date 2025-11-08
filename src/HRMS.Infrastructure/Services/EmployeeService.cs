using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Core.Entities.Tenant;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(TenantDbContext context, ILogger<EmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        // Validate uniqueness
        if (!await IsEmployeeCodeUniqueAsync(request.EmployeeCode))
            throw new InvalidOperationException($"Employee code '{request.EmployeeCode}' already exists");

        if (!await IsEmailUniqueAsync(request.Email))
            throw new InvalidOperationException($"Email '{request.Email}' is already in use");

        // Map DTO to Entity
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeCode = request.EmployeeCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            MiddleName = request.MiddleName ?? string.Empty,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PersonalEmail = request.PersonalEmail,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            MaritalStatus = request.MaritalStatus,

            // Address
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            Village = request.Village,
            District = request.District,
            Region = request.Region,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,

            // Employee Type & Nationality
            EmployeeType = request.EmployeeType,
            Nationality = request.Nationality,
            CountryOfOrigin = request.CountryOfOrigin,

            // Identification
            NationalIdCard = request.NationalIdCard,
            PassportNumber = request.PassportNumber,
            PassportIssueDate = request.PassportIssueDate,
            PassportExpiryDate = request.PassportExpiryDate,

            // Visa/Work Permit
            VisaType = request.VisaType,
            VisaNumber = request.VisaNumber,
            VisaIssueDate = request.VisaIssueDate,
            VisaExpiryDate = request.VisaExpiryDate,
            WorkPermitNumber = request.WorkPermitNumber,
            WorkPermitExpiryDate = request.WorkPermitExpiryDate,

            // Tax & Statutory
            TaxResidentStatus = request.TaxResidentStatus,
            TaxIdNumber = request.TaxIdNumber,
            NPFNumber = request.NPFNumber,
            NSFNumber = request.NSFNumber,
            PRGFNumber = request.PRGFNumber,
            IsNPFEligible = request.IsNPFEligible,
            IsNSFEligible = request.IsNSFEligible,

            // Employment Details
            JobTitle = request.JobTitle,
            DepartmentId = request.DepartmentId,
            ManagerId = request.ManagerId,
            JoiningDate = request.JoiningDate,
            ProbationEndDate = request.ProbationEndDate,
            ContractEndDate = request.ContractEndDate,
            IsActive = true,

            // Salary & Bank
            BasicSalary = request.BasicSalary,
            SalaryCurrency = request.SalaryCurrency,
            BankName = request.BankName,
            BankAccountNumber = request.BankAccountNumber,
            BankBranch = request.BankBranch,
            BankSwiftCode = request.BankSwiftCode,

            // Calculate pro-rated leave balances
            AnnualLeaveBalance = request.AnnualLeaveBalance ?? CalculateProRatedAnnualLeave(request.JoiningDate, DateTime.UtcNow),
            SickLeaveBalance = request.SickLeaveBalance,
            CasualLeaveBalance = request.CasualLeaveBalance,

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "System", // TODO: Get from auth context
            UpdatedBy = "System"
        };

        // Validate expatriate-specific requirements
        var validationError = ValidateExpatriateMandatoryFields(employee);
        if (validationError != null)
            throw new InvalidOperationException(validationError);

        // Add emergency contacts
        foreach (var contactDto in request.EmergencyContacts)
        {
            var contact = new EmergencyContact
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ContactName = contactDto.ContactName,
                PhoneNumber = contactDto.PhoneNumber,
                AlternatePhoneNumber = contactDto.AlternatePhoneNumber,
                Email = contactDto.Email,
                Relationship = contactDto.Relationship,
                ContactType = contactDto.ContactType,
                Address = contactDto.Address,
                Country = contactDto.Country,
                IsPrimary = contactDto.IsPrimary,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedBy = "System"
            };
            employee.EmergencyContacts.Add(contact);
        }

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created employee: {EmployeeCode} - {Name} ({Type})",
            employee.EmployeeCode, employee.FullName, employee.EmployeeType);

        return await GetEmployeeByIdAsync(employee.Id);
    }

    public async Task<EmployeeDto> GetEmployeeByIdAsync(Guid id)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Include(e => e.EmergencyContacts)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {id} not found");

        return MapToDto(employee);
    }

    public async Task<EmployeeDto> GetEmployeeByCodeAsync(string employeeCode)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Include(e => e.EmergencyContacts)
            .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode);

        if (employee == null)
            throw new KeyNotFoundException($"Employee with code {employeeCode} not found");

        return MapToDto(employee);
    }

    public async Task<List<EmployeeListDto>> GetAllEmployeesAsync(bool includeInactive = false)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(e => e.IsActive);

        var employees = await query
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync();

        return employees.Select(MapToListDto).ToList();
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.EmergencyContacts)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {id} not found");

        // Validate email uniqueness (excluding current employee)
        if (request.Email != employee.Email && !await IsEmailUniqueAsync(request.Email, id))
            throw new InvalidOperationException($"Email '{request.Email}' is already in use");

        // Update fields
        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.MiddleName = request.MiddleName ?? string.Empty;
        employee.Email = request.Email;
        employee.PhoneNumber = request.PhoneNumber;
        employee.PersonalEmail = request.PersonalEmail;
        employee.DateOfBirth = request.DateOfBirth;
        employee.Gender = request.Gender;
        employee.MaritalStatus = request.MaritalStatus;

        // Address
        employee.AddressLine1 = request.AddressLine1;
        employee.AddressLine2 = request.AddressLine2;
        employee.Village = request.Village;
        employee.District = request.District;
        employee.Region = request.Region;
        employee.City = request.City;
        employee.PostalCode = request.PostalCode;
        employee.Country = request.Country;

        employee.EmployeeType = request.EmployeeType;
        employee.Nationality = request.Nationality;
        employee.CountryOfOrigin = request.CountryOfOrigin;

        employee.NationalIdCard = request.NationalIdCard;
        employee.PassportNumber = request.PassportNumber;
        employee.PassportIssueDate = request.PassportIssueDate;
        employee.PassportExpiryDate = request.PassportExpiryDate;

        employee.VisaType = request.VisaType;
        employee.VisaNumber = request.VisaNumber;
        employee.VisaIssueDate = request.VisaIssueDate;
        employee.VisaExpiryDate = request.VisaExpiryDate;
        employee.WorkPermitNumber = request.WorkPermitNumber;
        employee.WorkPermitExpiryDate = request.WorkPermitExpiryDate;

        employee.TaxResidentStatus = request.TaxResidentStatus;
        employee.TaxIdNumber = request.TaxIdNumber;
        employee.NPFNumber = request.NPFNumber;
        employee.NSFNumber = request.NSFNumber;
        employee.PRGFNumber = request.PRGFNumber;
        employee.IsNPFEligible = request.IsNPFEligible;
        employee.IsNSFEligible = request.IsNSFEligible;

        employee.JobTitle = request.JobTitle;
        employee.DepartmentId = request.DepartmentId;
        employee.ManagerId = request.ManagerId;
        employee.ProbationEndDate = request.ProbationEndDate;
        employee.ContractEndDate = request.ContractEndDate;
        employee.IsActive = request.IsActive;

        employee.BasicSalary = request.BasicSalary;
        employee.SalaryCurrency = request.SalaryCurrency;
        employee.BankName = request.BankName;
        employee.BankAccountNumber = request.BankAccountNumber;
        employee.BankBranch = request.BankBranch;
        employee.BankSwiftCode = request.BankSwiftCode;

        employee.AnnualLeaveBalance = request.AnnualLeaveBalance;
        employee.SickLeaveBalance = request.SickLeaveBalance;
        employee.CasualLeaveBalance = request.CasualLeaveBalance;

        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = "System"; // TODO: Get from auth context

        // Validate expatriate requirements after update
        var validationError = ValidateExpatriateMandatoryFields(employee);
        if (validationError != null)
            throw new InvalidOperationException(validationError);

        // Update emergency contacts
        // Remove existing contacts
        _context.EmergencyContacts.RemoveRange(employee.EmergencyContacts);

        // Add new contacts
        foreach (var contactDto in request.EmergencyContacts)
        {
            var contact = new EmergencyContact
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ContactName = contactDto.ContactName,
                PhoneNumber = contactDto.PhoneNumber,
                AlternatePhoneNumber = contactDto.AlternatePhoneNumber,
                Email = contactDto.Email,
                Relationship = contactDto.Relationship,
                ContactType = contactDto.ContactType,
                Address = contactDto.Address,
                Country = contactDto.Country,
                IsPrimary = contactDto.IsPrimary,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedBy = "System"
            };
            employee.EmergencyContacts.Add(contact);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated employee: {EmployeeCode} - {Name}", employee.EmployeeCode, employee.FullName);

        return await GetEmployeeByIdAsync(employee.Id);
    }

    public async Task<bool> DeleteEmployeeAsync(Guid id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
            return false;

        // Soft delete
        employee.IsDeleted = true;
        employee.IsActive = false;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = "System"; // TODO: Get from auth context

        await _context.SaveChangesAsync();

        _logger.LogInformation("Soft deleted employee: {EmployeeCode} - {Name}", employee.EmployeeCode, employee.FullName);

        return true;
    }

    public async Task<List<EmployeeListDto>> GetExpatriateEmployeesAsync()
    {
        var employees = await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.EmployeeType == EmployeeType.Expatriate && e.IsActive)
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync();

        return employees.Select(MapToListDto).ToList();
    }

    public async Task<Dictionary<string, int>> GetEmployeesByCountryAsync()
    {
        var result = await _context.Employees
            .Where(e => e.EmployeeType == EmployeeType.Expatriate && e.IsActive)
            .GroupBy(e => e.CountryOfOrigin ?? "Unknown")
            .Select(g => new { Country = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToDictionaryAsync(x => x.Country, x => x.Count);

        return result;
    }

    public async Task<List<DocumentExpiryInfoDto>> GetExpiringDocumentsAsync(int daysAhead = 90)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(daysAhead);

        var employees = await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.EmployeeType == EmployeeType.Expatriate && e.IsActive)
            .Where(e =>
                (e.PassportExpiryDate.HasValue && e.PassportExpiryDate.Value <= thresholdDate) ||
                (e.VisaExpiryDate.HasValue && e.VisaExpiryDate.Value <= thresholdDate))
            .ToListAsync();

        var result = new List<DocumentExpiryInfoDto>();

        foreach (var emp in employees)
        {
            var info = new DocumentExpiryInfoDto
            {
                EmployeeId = emp.Id,
                EmployeeCode = emp.EmployeeCode,
                EmployeeName = emp.FullName,
                Email = emp.Email,
                Department = emp.Department?.Name ?? "N/A",
                CountryOfOrigin = emp.CountryOfOrigin,

                PassportNumber = emp.PassportNumber,
                PassportExpiryDate = emp.PassportExpiryDate,
                DaysUntilPassportExpiry = emp.PassportExpiryDate.HasValue
                    ? (int)(emp.PassportExpiryDate.Value - DateTime.UtcNow).TotalDays
                    : null,
                PassportExpiryStatus = emp.PassportExpiryStatus,

                VisaType = emp.VisaType,
                VisaNumber = emp.VisaNumber,
                VisaExpiryDate = emp.VisaExpiryDate,
                DaysUntilVisaExpiry = emp.VisaExpiryDate.HasValue
                    ? (int)(emp.VisaExpiryDate.Value - DateTime.UtcNow).TotalDays
                    : null,
                VisaExpiryStatus = emp.VisaExpiryStatus,

                WorkPermitNumber = emp.WorkPermitNumber,
                WorkPermitExpiryDate = emp.WorkPermitExpiryDate,
                DaysUntilWorkPermitExpiry = emp.WorkPermitExpiryDate.HasValue
                    ? (int)(emp.WorkPermitExpiryDate.Value - DateTime.UtcNow).TotalDays
                    : null,

                RequiresUrgentAction = emp.HasDocumentsExpiringSoon || emp.HasExpiredDocuments
            };

            // Set recommended action
            if (emp.HasExpiredDocuments)
                info.RecommendedAction = "CRITICAL: Document(s) expired - Immediate renewal required";
            else if (emp.VisaExpiryStatus == DocumentExpiryStatus.Critical)
                info.RecommendedAction = "URGENT: Visa expiring soon - Start renewal process immediately";
            else if (emp.PassportExpiryStatus == DocumentExpiryStatus.Critical)
                info.RecommendedAction = "URGENT: Passport expiring soon - Renew passport";
            else if (emp.VisaExpiryStatus == DocumentExpiryStatus.ExpiringVerySoon)
                info.RecommendedAction = "Start visa renewal process";
            else if (emp.PassportExpiryStatus == DocumentExpiryStatus.ExpiringVerySoon)
                info.RecommendedAction = "Consider renewing passport";
            else
                info.RecommendedAction = "Monitor expiry dates";

            result.Add(info);
        }

        return result.OrderBy(x => x.DaysUntilVisaExpiry ?? x.DaysUntilPassportExpiry ?? int.MaxValue).ToList();
    }

    public async Task<EmployeeDto> RenewVisaAsync(Guid id, DateTime newExpiryDate, string? newVisaNumber = null)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {id} not found");

        if (employee.EmployeeType != EmployeeType.Expatriate)
            throw new InvalidOperationException("Only expatriate employees have visa/work permits");

        employee.VisaExpiryDate = newExpiryDate;
        if (!string.IsNullOrWhiteSpace(newVisaNumber))
            employee.VisaNumber = newVisaNumber;

        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = "System"; // TODO: Get from auth context

        await _context.SaveChangesAsync();

        _logger.LogInformation("Renewed visa for employee: {EmployeeCode} - New expiry: {ExpiryDate}",
            employee.EmployeeCode, newExpiryDate);

        return await GetEmployeeByIdAsync(id);
    }

    public async Task<DocumentExpiryInfoDto> GetDocumentStatusAsync(Guid id)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {id} not found");

        if (employee.EmployeeType != EmployeeType.Expatriate)
            throw new InvalidOperationException("Document status is only applicable to expatriate employees");

        var info = new DocumentExpiryInfoDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = employee.FullName,
            Email = employee.Email,
            Department = employee.Department?.Name ?? "N/A",
            CountryOfOrigin = employee.CountryOfOrigin,

            PassportNumber = employee.PassportNumber,
            PassportExpiryDate = employee.PassportExpiryDate,
            DaysUntilPassportExpiry = employee.PassportExpiryDate.HasValue
                ? (int)(employee.PassportExpiryDate.Value - DateTime.UtcNow).TotalDays
                : null,
            PassportExpiryStatus = employee.PassportExpiryStatus,

            VisaType = employee.VisaType,
            VisaNumber = employee.VisaNumber,
            VisaExpiryDate = employee.VisaExpiryDate,
            DaysUntilVisaExpiry = employee.VisaExpiryDate.HasValue
                ? (int)(employee.VisaExpiryDate.Value - DateTime.UtcNow).TotalDays
                : null,
            VisaExpiryStatus = employee.VisaExpiryStatus,

            WorkPermitNumber = employee.WorkPermitNumber,
            WorkPermitExpiryDate = employee.WorkPermitExpiryDate,
            DaysUntilWorkPermitExpiry = employee.WorkPermitExpiryDate.HasValue
                ? (int)(employee.WorkPermitExpiryDate.Value - DateTime.UtcNow).TotalDays
                : null,

            RequiresUrgentAction = employee.HasDocumentsExpiringSoon || employee.HasExpiredDocuments
        };

        // Set recommended action
        if (employee.HasExpiredDocuments)
            info.RecommendedAction = "CRITICAL: Document(s) expired - Immediate renewal required";
        else if (employee.VisaExpiryStatus == DocumentExpiryStatus.Critical)
            info.RecommendedAction = "URGENT: Visa expiring soon - Start renewal process immediately";
        else if (employee.PassportExpiryStatus == DocumentExpiryStatus.Critical)
            info.RecommendedAction = "URGENT: Passport expiring soon - Renew passport";
        else if (employee.VisaExpiryStatus == DocumentExpiryStatus.ExpiringVerySoon)
            info.RecommendedAction = "Start visa renewal process";
        else if (employee.PassportExpiryStatus == DocumentExpiryStatus.ExpiringVerySoon)
            info.RecommendedAction = "Consider renewing passport";
        else
            info.RecommendedAction = "All documents valid";

        return info;
    }

    public async Task<List<EmployeeListDto>> SearchEmployeesAsync(string searchTerm)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Where(e => e.IsActive)
            .Where(e =>
                e.EmployeeCode.Contains(searchTerm) ||
                e.FirstName.Contains(searchTerm) ||
                e.LastName.Contains(searchTerm) ||
                e.Email.Contains(searchTerm));

        var employees = await query
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync();

        return employees.Select(MapToListDto).ToList();
    }

    public async Task<List<EmployeeListDto>> GetEmployeesByDepartmentAsync(Guid departmentId)
    {
        var employees = await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.DepartmentId == departmentId && e.IsActive)
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync();

        return employees.Select(MapToListDto).ToList();
    }

    public string? ValidateExpatriateMandatoryFields(Employee employee)
    {
        if (employee.EmployeeType != EmployeeType.Expatriate)
        {
            // For local employees, check National ID
            if (string.IsNullOrWhiteSpace(employee.NationalIdCard))
                return "National ID Card is required for local employees";

            return null; // Local employee validation passed
        }

        var errors = new List<string>();

        // Mandatory fields for expatriates
        if (string.IsNullOrWhiteSpace(employee.CountryOfOrigin))
            errors.Add("Country of Origin is required for expatriates");

        if (string.IsNullOrWhiteSpace(employee.PassportNumber))
            errors.Add("Passport Number is required for expatriates");

        if (!employee.PassportExpiryDate.HasValue)
            errors.Add("Passport Expiry Date is required for expatriates");
        else if (employee.PassportExpiryDate.Value <= DateTime.UtcNow)
            errors.Add("Passport Expiry Date must be in the future");

        if (!employee.VisaType.HasValue)
            errors.Add("Visa Type is required for expatriates");

        if (!employee.VisaExpiryDate.HasValue)
            errors.Add("Visa Expiry Date is required for expatriates");
        else if (employee.VisaExpiryDate.Value <= DateTime.UtcNow)
            errors.Add("Visa Expiry Date must be in the future");

        // Logical validation: Work permit expiry should be before passport expiry
        if (employee.WorkPermitExpiryDate.HasValue &&
            employee.PassportExpiryDate.HasValue &&
            employee.WorkPermitExpiryDate.Value > employee.PassportExpiryDate.Value)
        {
            errors.Add("Work Permit expiry date cannot be after Passport expiry date");
        }

        // Emergency contacts check
        if (employee.EmergencyContacts.Count < 1)
            errors.Add("At least one emergency contact is required");

        return errors.Any() ? string.Join("; ", errors) : null;
    }

    public async Task<bool> IsEmployeeCodeUniqueAsync(string employeeCode, Guid? excludeEmployeeId = null)
    {
        var query = _context.Employees.Where(e => e.EmployeeCode == employeeCode);

        if (excludeEmployeeId.HasValue)
            query = query.Where(e => e.Id != excludeEmployeeId.Value);

        return !await query.AnyAsync();
    }

    public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeEmployeeId = null)
    {
        var query = _context.Employees.Where(e => e.Email == email);

        if (excludeEmployeeId.HasValue)
            query = query.Where(e => e.Id != excludeEmployeeId.Value);

        return !await query.AnyAsync();
    }

    public decimal CalculateProRatedAnnualLeave(DateTime joiningDate, DateTime calculationDate)
    {
        const decimal mauritiusAnnualLeave = 22m; // Mauritius standard: 22 working days

        // If joined in current year, calculate pro-rated
        if (joiningDate.Year == calculationDate.Year)
        {
            var monthsRemaining = 12 - joiningDate.Month + 1; // Including joining month
            var proRatedLeave = mauritiusAnnualLeave * (monthsRemaining / 12m);
            return Math.Round(proRatedLeave, 2);
        }

        // If joined in previous years, full annual leave
        return mauritiusAnnualLeave;
    }

    // Helper methods for mapping
    private EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            MiddleName = employee.MiddleName,
            FullName = employee.FullName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            PersonalEmail = employee.PersonalEmail,
            DateOfBirth = employee.DateOfBirth,
            Age = employee.Age,
            Gender = employee.Gender,
            MaritalStatus = employee.MaritalStatus,

            // Address
            AddressLine1 = employee.AddressLine1,
            AddressLine2 = employee.AddressLine2,
            Village = employee.Village,
            District = employee.District,
            Region = employee.Region,
            City = employee.City,
            PostalCode = employee.PostalCode,
            Country = employee.Country,

            EmployeeType = employee.EmployeeType,
            IsExpatriate = employee.IsExpatriate,
            Nationality = employee.Nationality,
            CountryOfOrigin = employee.CountryOfOrigin,

            NationalIdCard = employee.NationalIdCard,
            PassportNumber = employee.PassportNumber,
            PassportIssueDate = employee.PassportIssueDate,
            PassportExpiryDate = employee.PassportExpiryDate,
            PassportExpiryStatus = employee.PassportExpiryStatus,

            VisaType = employee.VisaType,
            VisaNumber = employee.VisaNumber,
            VisaIssueDate = employee.VisaIssueDate,
            VisaExpiryDate = employee.VisaExpiryDate,
            VisaExpiryStatus = employee.VisaExpiryStatus,
            WorkPermitNumber = employee.WorkPermitNumber,
            WorkPermitExpiryDate = employee.WorkPermitExpiryDate,

            HasExpiredDocuments = employee.HasExpiredDocuments,
            HasDocumentsExpiringSoon = employee.HasDocumentsExpiringSoon,

            TaxResidentStatus = employee.TaxResidentStatus,
            TaxIdNumber = employee.TaxIdNumber,
            NPFNumber = employee.NPFNumber,
            NSFNumber = employee.NSFNumber,
            PRGFNumber = employee.PRGFNumber,
            IsNPFEligible = employee.IsNPFEligible,
            IsNSFEligible = employee.IsNSFEligible,

            JobTitle = employee.JobTitle,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name ?? "N/A",
            ManagerId = employee.ManagerId,
            ManagerName = employee.Manager?.FullName,
            JoiningDate = employee.JoiningDate,
            ProbationEndDate = employee.ProbationEndDate,
            ConfirmationDate = employee.ConfirmationDate,
            ContractEndDate = employee.ContractEndDate,
            ResignationDate = employee.ResignationDate,
            LastWorkingDate = employee.LastWorkingDate,
            IsActive = employee.IsActive,
            YearsOfService = employee.YearsOfService,

            BasicSalary = employee.BasicSalary,
            SalaryCurrency = employee.SalaryCurrency,
            BankName = employee.BankName,
            BankAccountNumber = employee.BankAccountNumber,
            BankBranch = employee.BankBranch,
            BankSwiftCode = employee.BankSwiftCode,

            EmergencyContacts = employee.EmergencyContacts.Select(c => new EmergencyContactDto
            {
                Id = c.Id,
                ContactName = c.ContactName,
                PhoneNumber = c.PhoneNumber,
                AlternatePhoneNumber = c.AlternatePhoneNumber,
                Email = c.Email,
                Relationship = c.Relationship,
                ContactType = c.ContactType,
                Address = c.Address,
                Country = c.Country,
                IsPrimary = c.IsPrimary
            }).ToList(),

            AnnualLeaveBalance = employee.AnnualLeaveBalance,
            SickLeaveBalance = employee.SickLeaveBalance,
            CasualLeaveBalance = employee.CasualLeaveBalance,

            IsOffboarded = employee.IsOffboarded,
            OffboardingDate = employee.OffboardingDate,
            OffboardingReason = employee.OffboardingReason,

            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt ?? employee.CreatedAt,
            CreatedBy = employee.CreatedBy ?? string.Empty,
            UpdatedBy = employee.UpdatedBy ?? string.Empty
        };
    }

    private EmployeeListDto MapToListDto(Employee employee)
    {
        return new EmployeeListDto
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FullName = employee.FullName,
            Email = employee.Email,
            JobTitle = employee.JobTitle,
            DepartmentName = employee.Department?.Name ?? "N/A",
            EmployeeType = employee.EmployeeType,
            IsExpatriate = employee.IsExpatriate,
            CountryOfOrigin = employee.CountryOfOrigin,
            JoiningDate = employee.JoiningDate,
            YearsOfService = employee.YearsOfService,
            IsActive = employee.IsActive,
            HasExpiredDocuments = employee.HasExpiredDocuments,
            HasDocumentsExpiringSoon = employee.HasDocumentsExpiringSoon,
            PassportExpiryStatus = employee.IsExpatriate ? employee.PassportExpiryStatus : null,
            VisaExpiryStatus = employee.IsExpatriate ? employee.VisaExpiryStatus : null
        };
    }
}
