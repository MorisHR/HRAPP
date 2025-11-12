using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

/// <summary>
/// Complete employee information - visible only to the employee themselves
/// SECURITY: All fields unmasked, includes full bank details
/// Used for: Self-service portal, personal profile view
/// </summary>
public class EmployeeSelfDto
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PersonalEmail { get; set; }
    public DateTime DateOfBirth { get; set; }
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public MaritalStatus MaritalStatus { get; set; }

    // Address
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string? Village { get; set; }
    public string? District { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "Mauritius";

    // Employee Type & Nationality
    public EmployeeType EmployeeType { get; set; }
    public bool IsExpatriate { get; set; }
    public string Nationality { get; set; } = string.Empty;
    public string? CountryOfOrigin { get; set; }

    // Identification
    public string? NationalIdCard { get; set; }
    public string? PassportNumber { get; set; }
    public DateTime? PassportIssueDate { get; set; }
    public DateTime? PassportExpiryDate { get; set; }
    public DocumentExpiryStatus PassportExpiryStatus { get; set; }

    // Visa/Work Permit
    public VisaType? VisaType { get; set; }
    public string? VisaNumber { get; set; }
    public DateTime? VisaIssueDate { get; set; }
    public DateTime? VisaExpiryDate { get; set; }
    public DocumentExpiryStatus VisaExpiryStatus { get; set; }
    public string? WorkPermitNumber { get; set; }
    public DateTime? WorkPermitExpiryDate { get; set; }

    // Document Status Flags
    public bool HasExpiredDocuments { get; set; }
    public bool HasDocumentsExpiringSoon { get; set; }

    // Tax & Statutory
    public TaxResidentStatus TaxResidentStatus { get; set; }
    public string? TaxIdNumber { get; set; }
    public string? NPFNumber { get; set; }
    public string? NSFNumber { get; set; }
    public string? PRGFNumber { get; set; }
    public bool IsNPFEligible { get; set; }
    public bool IsNSFEligible { get; set; }

    // Employment Details
    public string JobTitle { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public DateTime JoiningDate { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public DateTime? ConfirmationDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public DateTime? ResignationDate { get; set; }
    public DateTime? LastWorkingDate { get; set; }
    public bool IsActive { get; set; }
    public int YearsOfService { get; set; }

    // Salary & Bank (UNMASKED for self)
    public decimal BasicSalary { get; set; }
    public string SalaryCurrency { get; set; } = "MUR";
    public string? BankName { get; set; }
    /// <summary>
    /// Full bank account number - UNMASKED (employee can see their own)
    /// </summary>
    public string? BankAccountNumber { get; set; }
    public string? BankBranch { get; set; }
    public string? BankSwiftCode { get; set; }

    // Emergency Contacts
    public List<EmergencyContactDto> EmergencyContacts { get; set; } = new();

    // Leave Balances
    public decimal AnnualLeaveBalance { get; set; }
    public decimal SickLeaveBalance { get; set; }
    public decimal CasualLeaveBalance { get; set; }

    // Offboarding
    public bool IsOffboarded { get; set; }
    public DateTime? OffboardingDate { get; set; }
    public string? OffboardingReason { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
