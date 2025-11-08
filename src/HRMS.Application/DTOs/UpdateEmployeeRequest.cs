using System.ComponentModel.DataAnnotations;
using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class UpdateEmployeeRequest
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    public string? PersonalEmail { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public Gender Gender { get; set; }

    [Required]
    public MaritalStatus MaritalStatus { get; set; }

    // Address (Mauritius Compliant)
    [Required]
    [StringLength(500)]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(500)]
    public string? AddressLine2 { get; set; }

    [StringLength(100)]
    public string? Village { get; set; }

    [StringLength(100)]
    public string? District { get; set; }

    [StringLength(100)]
    public string? Region { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = "Mauritius";

    // Employee Type & Nationality
    [Required]
    public EmployeeType EmployeeType { get; set; }

    [Required]
    [StringLength(100)]
    public string Nationality { get; set; } = "Mauritius";

    [StringLength(100)]
    public string? CountryOfOrigin { get; set; }

    // Identification
    [StringLength(50)]
    public string? NationalIdCard { get; set; }

    [StringLength(50)]
    public string? PassportNumber { get; set; }

    public DateTime? PassportIssueDate { get; set; }
    public DateTime? PassportExpiryDate { get; set; }

    // Visa/Work Permit
    public VisaType? VisaType { get; set; }

    [StringLength(100)]
    public string? VisaNumber { get; set; }

    public DateTime? VisaIssueDate { get; set; }
    public DateTime? VisaExpiryDate { get; set; }

    [StringLength(100)]
    public string? WorkPermitNumber { get; set; }

    public DateTime? WorkPermitExpiryDate { get; set; }

    // Tax & Statutory
    [Required]
    public TaxResidentStatus TaxResidentStatus { get; set; }

    [StringLength(50)]
    public string? TaxIdNumber { get; set; }

    [StringLength(50)]
    public string? NPFNumber { get; set; }

    [StringLength(50)]
    public string? NSFNumber { get; set; }

    [StringLength(50)]
    public string? PRGFNumber { get; set; }

    public bool IsNPFEligible { get; set; }
    public bool IsNSFEligible { get; set; }

    // Employment Details
    [Required]
    [StringLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required]
    public Guid DepartmentId { get; set; }

    public Guid? ManagerId { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public bool IsActive { get; set; }

    // Salary & Bank
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal BasicSalary { get; set; }

    [StringLength(10)]
    public string SalaryCurrency { get; set; } = "MUR";

    [StringLength(200)]
    public string? BankName { get; set; }

    [StringLength(100)]
    public string? BankAccountNumber { get; set; }

    [StringLength(200)]
    public string? BankBranch { get; set; }

    [StringLength(50)]
    public string? BankSwiftCode { get; set; }

    // Emergency Contacts
    public List<EmergencyContactDto> EmergencyContacts { get; set; } = new();

    // Leave Balances
    public decimal AnnualLeaveBalance { get; set; }
    public decimal SickLeaveBalance { get; set; }
    public decimal CasualLeaveBalance { get; set; }
}
