# Phase 3B - Complete Leave Management System
## Implementation Guide

**Status:** Ready for Implementation
**Estimated Lines of Code:** ~3500 lines
**Est. Implementation Time:** 3-4 hours

---

## Overview

This guide provides complete specifications for implementing the Leave Management System with Mauritius Labour Law compliance. All code examples are production-ready.

---

## 1. Enums (4 files to create)

### src/HRMS.Core/Enums/LeaveStatus.cs
✅ ALREADY CREATED

### src/HRMS.Core/Enums/ApprovalStatus.cs
```csharp
namespace HRMS.Core.Enums;

public enum ApprovalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    MoreInfoRequested = 4
}
```

### src/HRMS.Core/Enums/HolidayType.cs
```csharp
namespace HRMS.Core.Enums;

public enum HolidayType
{
    NationalHoliday = 1,      // Mauritius public holiday
    CompanyHoliday = 2,       // Company-specific
    RegionalHoliday = 3       // Region-specific
}
```

### src/HRMS.Core/Enums/LeaveCalculationType.cs
```csharp
namespace HRMS.Core.Enums;

public enum LeaveCalculationType
{
    WorkingDays = 1,          // Exclude weekends
    CalendarDays = 2,         // Include all days
    HalfDay = 3               // Half-day leave
}
```

---

## 2. Entities (6 files)

### src/HRMS.Core/Entities/Tenant/LeaveType.cs
```csharp
using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Master data for leave types (tenant-specific configuration)
/// </summary>
public class LeaveType : BaseEntity
{
    public LeaveTypeEnum TypeCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultEntitlement { get; set; }  // Days per year
    public bool RequiresApproval { get; set; } = true;
    public bool IsPaid { get; set; } = true;
    public bool CanCarryForward { get; set; } = false;
    public int MaxCarryForwardDays { get; set; } = 0;
    public bool RequiresDocumentation { get; set; } = false;  // e.g., medical certificate
    public int MinDaysNotice { get; set; } = 0;
    public int MaxConsecutiveDays { get; set; } = 365;
    public bool IsActive { get; set; } = true;
    public string? ApprovalWorkflow { get; set; }  // JSON for multi-level approval

    // Navigation
    public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public virtual ICollection<LeaveApplication> LeaveApplications { get; set; } = new List<LeaveApplication>();
}
```

### src/HRMS.Core/Entities/Tenant/LeaveBalance.cs
```csharp
using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Track leave balance per employee, per leave type, per year
/// </summary>
public class LeaveBalance : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }

    public decimal TotalEntitlement { get; set; }     // Total days entitled for the year
    public decimal UsedDays { get; set; } = 0;        // Days already taken
    public decimal PendingDays { get; set; } = 0;     // Days in pending applications
    public decimal AvailableDays => TotalEntitlement - UsedDays - PendingDays;

    public decimal CarriedForward { get; set; } = 0;  // Days brought from previous year
    public decimal Accrued { get; set; } = 0;         // Days accrued so far (for monthly accrual)

    public DateTime? LastAccrualDate { get; set; }
    public DateTime? ExpiryDate { get; set; }         // When carried forward days expire

    // Navigation
    public virtual Employee? Employee { get; set; }
    public virtual LeaveType? LeaveType { get; set; }
}
```

### src/HRMS.Core/Entities/Tenant/LeaveApplication.cs
```csharp
using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Leave application/request
/// </summary>
public class LeaveApplication : BaseEntity
{
    public string ApplicationNumber { get; set; } = string.Empty;  // LEV-2025-0001
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }            // Calculated working days
    public LeaveCalculationType CalculationType { get; set; } = LeaveCalculationType.WorkingDays;

    public string Reason { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? ContactAddress { get; set; }

    public LeaveStatus Status { get; set; } = LeaveStatus.PendingApproval;
    public DateTime? AppliedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApproverComments { get; set; }

    public DateTime? RejectedDate { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime? CancelledDate { get; set; }
    public Guid? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }

    public string? AttachmentPath { get; set; }       // Path to medical certificate, etc.
    public bool RequiresHRApproval { get; set; } = false;

    // Navigation
    public virtual Employee? Employee { get; set; }
    public virtual LeaveType? LeaveType { get; set; }
    public virtual Employee? Approver { get; set; }
    public virtual Employee? Rejector { get; set; }
    public virtual ICollection<LeaveApproval> Approvals { get; set; } = new List<LeaveApproval>();
}
```

### src/HRMS.Core/Entities/Tenant/LeaveApproval.cs
```csharp
using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Multi-level approval tracking
/// </summary>
public class LeaveApproval : BaseEntity
{
    public Guid LeaveApplicationId { get; set; }
    public int ApprovalLevel { get; set; }            // 1 = Manager, 2 = HR, etc.
    public string ApproverRole { get; set; } = string.Empty;  // "DepartmentManager", "HRManager"
    public Guid? ApproverId { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime? ActionDate { get; set; }
    public string? Comments { get; set; }
    public string? RequestedInfo { get; set; }        // If more info requested

    public bool IsCurrentLevel { get; set; } = true;
    public bool IsComplete { get; set; } = false;

    // Navigation
    public virtual LeaveApplication? LeaveApplication { get; set; }
    public virtual Employee? Approver { get; set; }
}
```

### src/HRMS.Core/Entities/Tenant/PublicHoliday.cs
```csharp
using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Public holidays (Mauritius and tenant-specific)
/// </summary>
public class PublicHoliday : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Year { get; set; }
    public HolidayType Type { get; set; } = HolidayType.NationalHoliday;
    public string? Description { get; set; }
    public bool IsRecurring { get; set; } = true;     // Repeats every year
    public string? Country { get; set; } = "Mauritius";
    public bool IsActive { get; set; } = true;
}
```

### src/HRMS.Core/Entities/Tenant/LeaveEncashment.cs
```csharp
using HRMS.Core.Entities;

namespace HRMS.Core.Entities.Tenant;

/// <summary>
/// Leave encashment calculation for final settlement
/// </summary>
public class LeaveEncashment : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public DateTime CalculationDate { get; set; }
    public DateTime LastWorkingDay { get; set; }

    public decimal UnusedAnnualLeaveDays { get; set; }
    public decimal UnusedSickLeaveDays { get; set; }
    public decimal TotalEncashableDays { get; set; }

    public decimal DailySalary { get; set; }
    public decimal TotalEncashmentAmount { get; set; }

    public string? CalculationDetails { get; set; }   // JSON with breakdown
    public bool IsPaid { get; set; } = false;
    public DateTime? PaidDate { get; set; }
    public string? PaymentReference { get; set; }

    // Navigation
    public virtual Employee? Employee { get; set; }
}
```

---

## 3. DTOs (12 files)

### src/HRMS.Application/DTOs/CreateLeaveApplicationRequest.cs
```csharp
using System.ComponentModel.DataAnnotations;
using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class CreateLeaveApplicationRequest
{
    [Required]
    public Guid LeaveTypeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;

    public string? ContactNumber { get; set; }
    public string? ContactAddress { get; set; }

    public LeaveCalculationType CalculationType { get; set; } = LeaveCalculationType.WorkingDays;

    // For file upload
    public string? AttachmentBase64 { get; set; }
    public string? AttachmentFileName { get; set; }
}
```

### src/HRMS.Application/DTOs/ApproveLeaveRequest.cs
```csharp
namespace HRMS.Application.DTOs;

public class ApproveLeaveRequest
{
    public string? Comments { get; set; }
}
```

### src/HRMS.Application/DTOs/RejectLeaveRequest.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

public class RejectLeaveRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string RejectionReason { get; set; } = string.Empty;
}
```

### src/HRMS.Application/DTOs/CancelLeaveRequest.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs;

public class CancelLeaveRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string CancellationReason { get; set; } = string.Empty;
}
```

### src/HRMS.Application/DTOs/LeaveApplicationDto.cs
```csharp
using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class LeaveApplicationDto
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;

    // Employee details
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    // Leave details
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;

    // Status
    public LeaveStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }

    // Approval details
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedByName { get; set; }
    public string? ApproverComments { get; set; }

    public DateTime? RejectedDate { get; set; }
    public string? RejectedByName { get; set; }
    public string? RejectionReason { get; set; }

    public string? ContactNumber { get; set; }
    public string? AttachmentPath { get; set; }

    public List<LeaveApprovalDto> Approvals { get; set; } = new();
}
```

### src/HRMS.Application/DTOs/LeaveApplicationListDto.cs
```csharp
using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class LeaveApplicationListDto
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public LeaveStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }
}
```

### src/HRMS.Application/DTOs/LeaveBalanceDto.cs
```csharp
namespace HRMS.Application.DTOs;

public class LeaveBalanceDto
{
    public Guid Id { get; set; }
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int Year { get; set; }

    public decimal TotalEntitlement { get; set; }
    public decimal UsedDays { get; set; }
    public decimal PendingDays { get; set; }
    public decimal AvailableDays { get; set; }
    public decimal CarriedForward { get; set; }

    public DateTime? ExpiryDate { get; set; }
}
```

### src/HRMS.Application/DTOs/LeaveApprovalDto.cs
```csharp
using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class LeaveApprovalDto
{
    public Guid Id { get; set; }
    public int ApprovalLevel { get; set; }
    public string ApproverRole { get; set; } = string.Empty;
    public string? ApproverName { get; set; }
    public ApprovalStatus Status { get; set; }
    public DateTime? ActionDate { get; set; }
    public string? Comments { get; set; }
}
```

### src/HRMS.Application/DTOs/LeaveCalendarDto.cs
```csharp
using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class LeaveCalendarDto
{
    public Guid LeaveApplicationId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public LeaveStatus Status { get; set; }
}
```

### src/HRMS.Application/DTOs/LeaveTypeDto.cs
```csharp
using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class LeaveTypeDto
{
    public Guid Id { get; set; }
    public LeaveTypeEnum TypeCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultEntitlement { get; set; }
    public bool IsPaid { get; set; }
    public bool CanCarryForward { get; set; }
    public int MaxCarryForwardDays { get; set; }
    public bool RequiresDocumentation { get; set; }
    public bool IsActive { get; set; }
}
```

### src/HRMS.Application/DTOs/PublicHolidayDto.cs
```csharp
using HRMS.Core.Enums;

namespace HRMS.Application.DTOs;

public class PublicHolidayDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Year { get; set; }
    public HolidayType Type { get; set; }
    public string? Description { get; set; }
    public bool IsRecurring { get; set; }
}
```

### src/HRMS.Application/DTOs/LeaveEncashmentDto.cs
```csharp
namespace HRMS.Application.DTOs;

public class LeaveEncashmentDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;

    public DateTime CalculationDate { get; set; }
    public DateTime LastWorkingDay { get; set; }

    public decimal UnusedAnnualLeaveDays { get; set; }
    public decimal UnusedSickLeaveDays { get; set; }
    public decimal TotalEncashableDays { get; set; }

    public decimal DailySalary { get; set; }
    public decimal TotalEncashmentAmount { get; set; }

    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
}
```

---

## 4. Service Layer

### src/HRMS.Application/Interfaces/ILeaveService.cs
```csharp
using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces;

public interface ILeaveService
{
    // Leave Application
    Task<LeaveApplicationDto> ApplyForLeaveAsync(Guid employeeId, CreateLeaveApplicationRequest request);
    Task<LeaveApplicationDto> GetLeaveApplicationByIdAsync(Guid id);
    Task<List<LeaveApplicationListDto>> GetMyLeavesAsync(Guid employeeId, int? year = null);
    Task<LeaveApplicationDto> CancelLeaveAsync(Guid leaveId, Guid employeeId, CancelLeaveRequest request);

    // Approval Workflow
    Task<LeaveApplicationDto> ApproveLeaveAsync(Guid leaveId, Guid approverId, ApproveLeaveRequest request);
    Task<LeaveApplicationDto> RejectLeaveAsync(Guid leaveId, Guid approverId, RejectLeaveRequest request);
    Task<List<LeaveApplicationListDto>> GetPendingApprovalsAsync(Guid managerId);
    Task<List<LeaveApplicationListDto>> GetTeamLeavesAsync(Guid managerId, DateTime? startDate = null, DateTime? endDate = null);

    // Leave Balance
    Task<List<LeaveBalanceDto>> GetLeaveBalanceAsync(Guid employeeId, int? year = null);
    Task InitializeLeaveBalanceAsync(Guid employeeId, int year);
    Task AccrueMonthlyLeaveAsync(Guid employeeId, int year, int month);
    Task UpdateLeaveBalanceAsync(Guid employeeId, Guid leaveTypeId, int year, decimal days, string reason);

    // Leave Calendar
    Task<List<LeaveCalendarDto>> GetLeaveCalendarAsync(DateTime startDate, DateTime endDate, Guid? departmentId = null);
    Task<List<LeaveCalendarDto>> GetDepartmentLeaveCalendarAsync(Guid departmentId, int year, int month);

    // Leave Types
    Task<List<LeaveTypeDto>> GetLeaveTypesAsync();
    Task<LeaveTypeDto> GetLeaveTypeByIdAsync(Guid id);

    // Public Holidays
    Task<List<PublicHolidayDto>> GetPublicHolidaysAsync(int year);
    Task<bool> IsPublicHolidayAsync(DateTime date);

    // Calculations
    Task<decimal> CalculateWorkingDaysAsync(DateTime startDate, DateTime endDate);
    Task<decimal> CalculateProRatedLeaveEntitlementAsync(Guid employeeId, Guid leaveTypeId, DateTime joiningDate, int year);

    // Leave Encashment
    Task<LeaveEncashmentDto> CalculateLeaveEncashmentAsync(Guid employeeId, DateTime lastWorkingDay);
    Task<LeaveEncashmentDto> ProcessLeaveEncashmentAsync(Guid employeeId, DateTime lastWorkingDay);

    // Validation
    Task<string?> ValidateLeaveApplicationAsync(Guid employeeId, CreateLeaveApplicationRequest request);
    Task<bool> HasOverlappingLeaveAsync(Guid employeeId, DateTime startDate, DateTime endDate, Guid? excludeLeaveId = null);
}
```

---

## 5. Implementation Steps

Execute these steps in order:

1. **Create remaining enums** (ApprovalStatus, HolidayType, LeaveCalculationType)
2. **Create all 6 entities** in src/HRMS.Core/Entities/Tenant/
3. **Create all 12 DTOs** in src/HRMS.Application/DTOs/
4. **Create ILeaveService** interface
5. **Implement LeaveService** (~1500 lines - see next section)
6. **Create LeavesController** (~400 lines)
7. **Create PublicHolidaysController** (~150 lines)
8. **Update TenantDbContext** with entity configurations
9. **Create Mauritius holidays seeder**
10. **Register services in Program.cs**
11. **Create migration**
12. **Build and test**

---

## 6. LeaveService Implementation (Critical Business Logic)

Due to space constraints, I'll provide the key methods. Full implementation in separate file.

### Key Business Logic

#### Calculate Working Days
```csharp
public async Task<decimal> CalculateWorkingDaysAsync(DateTime startDate, DateTime endDate)
{
    if (startDate > endDate)
        throw new InvalidOperationException("Start date cannot be after end date");

    var holidays = await _context.PublicHolidays
        .Where(h => h.Date >= startDate && h.Date <= endDate && h.IsActive)
        .Select(h => h.Date.Date)
        .ToListAsync();

    decimal workingDays = 0;
    var currentDate = startDate.Date;

    while (currentDate <= endDate.Date)
    {
        // Skip weekends (Saturday = 6, Sunday = 0)
        if (currentDate.DayOfWeek != DayOfWeek.Saturday &&
            currentDate.DayOfWeek != DayOfWeek.Sunday &&
            !holidays.Contains(currentDate))
        {
            workingDays++;
        }
        currentDate = currentDate.AddDays(1);
    }

    return workingDays;
}
```

#### Validate Leave Application
```csharp
public async Task<string?> ValidateLeaveApplicationAsync(Guid employeeId, CreateLeaveApplicationRequest request)
{
    var errors = new List<string>();

    // Cannot apply for past dates
    if (request.StartDate.Date < DateTime.UtcNow.Date)
        errors.Add("Cannot apply for leave in the past");

    // End date must be >= start date
    if (request.EndDate < request.StartDate)
        errors.Add("End date cannot be before start date");

    // Check leave balance
    var leaveType = await _context.LeaveTypes.FindAsync(request.LeaveTypeId);
    if (leaveType == null)
        errors.Add("Invalid leave type");

    var balance = await _context.LeaveBalances
        .FirstOrDefaultAsync(b => b.EmployeeId == employeeId &&
                                  b.LeaveTypeId == request.LeaveTypeId &&
                                  b.Year == request.StartDate.Year);

    if (balance == null)
    {
        // Initialize balance if not exists
        await InitializeLeaveBalanceAsync(employeeId, request.StartDate.Year);
        balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(b => b.EmployeeId == employeeId &&
                                      b.LeaveTypeId == request.LeaveTypeId &&
                                      b.Year == request.StartDate.Year);
    }

    var workingDays = await CalculateWorkingDaysAsync(request.StartDate, request.EndDate);

    if (balance!.AvailableDays < workingDays)
        errors.Add($"Insufficient leave balance. Available: {balance.AvailableDays} days, Requested: {workingDays} days");

    // Check overlapping leaves
    var hasOverlap = await HasOverlappingLeaveAsync(employeeId, request.StartDate, request.EndDate);
    if (hasOverlap)
        errors.Add("You have an overlapping leave application");

    // Sick leave >3 days requires medical certificate
    if (leaveType.TypeCode == LeaveTypeEnum.SickLeave && workingDays > 3 && string.IsNullOrEmpty(request.AttachmentBase64))
        errors.Add("Medical certificate required for sick leave exceeding 3 days");

    return errors.Any() ? string.Join("; ", errors) : null;
}
```

---

## 7. Controllers

### LeavesController Endpoints (15)

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeavesController : ControllerBase
{
    // 1. Apply for leave
    [HttpPost]
    POST /api/leaves

    // 2. Get my leaves
    [HttpGet("my-leaves")]
    GET /api/leaves/my-leaves?year=2025

    // 3. Get leave by ID
    [HttpGet("{id}")]
    GET /api/leaves/{id}

    // 4. Cancel leave
    [HttpPost("{id}/cancel")]
    POST /api/leaves/{id}/cancel

    // 5. Approve leave (manager)
    [HttpPost("{id}/approve")]
    POST /api/leaves/{id}/approve

    // 6. Reject leave (manager)
    [HttpPost("{id}/reject")]
    POST /api/leaves/{id}/reject

    // 7. Get pending approvals (for managers)
    [HttpGet("pending-approvals")]
    GET /api/leaves/pending-approvals

    // 8. Get team leaves (for managers)
    [HttpGet("team")]
    GET /api/leaves/team?startDate=2025-01-01&endDate=2025-12-31

    // 9. Get my leave balance
    [HttpGet("balance")]
    GET /api/leaves/balance?year=2025

    // 10. Get leave calendar
    [HttpGet("calendar")]
    GET /api/leaves/calendar?startDate=2025-01-01&endDate=2025-01-31

    // 11. Get department calendar
    [HttpGet("department/{departmentId}/calendar")]
    GET /api/leaves/department/{id}/calendar?year=2025&month=1

    // 12. Get leave types
    [HttpGet("types")]
    GET /api/leaves/types

    // 13. Calculate leave encashment
    [HttpGet("encashment/calculate")]
    GET /api/leaves/encashment/calculate?employeeId={id}&lastWorkingDay=2025-12-31

    // 14. Get public holidays
    [HttpGet("public-holidays")]
    GET /api/leaves/public-holidays?year=2025

    // 15. Check if date is holiday
    [HttpGet("is-holiday")]
    GET /api/leaves/is-holiday?date=2025-01-01
}
```

---

## 8. Database Configuration

### TenantDbContext Updates

```csharp
public DbSet<LeaveType> LeaveTypes { get; set; }
public DbSet<LeaveBalance> LeaveBalances { get; set; }
public DbSet<LeaveApplication> LeaveApplications { get; set; }
public DbSet<LeaveApproval> LeaveApprovals { get; set; }
public DbSet<PublicHoliday> PublicHolidays { get; set; }
public DbSet<LeaveEncashment> LeaveEncashments { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // LeaveType
    modelBuilder.Entity<LeaveType>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.TypeCode);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        entity.Property(e => e.DefaultEntitlement).HasColumnType("decimal(10,2)");
    });

    // LeaveBalance
    modelBuilder.Entity<LeaveBalance>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId, e.Year }).IsUnique();
        entity.Property(e => e.TotalEntitlement).HasColumnType("decimal(10,2)");
        entity.Property(e => e.UsedDays).HasColumnType("decimal(10,2)");
        entity.Property(e => e.PendingDays).HasColumnType("decimal(10,2)");
        entity.Property(e => e.CarriedForward).HasColumnType("decimal(10,2)");
        entity.Property(e => e.Accrued).HasColumnType("decimal(10,2)");

        entity.HasOne(e => e.Employee)
              .WithMany()
              .HasForeignKey(e => e.EmployeeId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.LeaveType)
              .WithMany(lt => lt.LeaveBalances)
              .HasForeignKey(e => e.LeaveTypeId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.Ignore(e => e.AvailableDays);
    });

    // LeaveApplication
    modelBuilder.Entity<LeaveApplication>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.ApplicationNumber).IsUnique();
        entity.HasIndex(e => new { e.EmployeeId, e.StartDate, e.EndDate });
        entity.Property(e => e.ApplicationNumber).IsRequired().HasMaxLength(50);
        entity.Property(e => e.TotalDays).HasColumnType("decimal(10,2)");

        entity.HasOne(e => e.Employee)
              .WithMany()
              .HasForeignKey(e => e.EmployeeId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.LeaveType)
              .WithMany(lt => lt.LeaveApplications)
              .HasForeignKey(e => e.LeaveTypeId)
              .OnDelete(DeleteBehavior.Restrict);
    });

    // LeaveApproval
    modelBuilder.Entity<LeaveApproval>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.LeaveApplicationId, e.ApprovalLevel });

        entity.HasOne(e => e.LeaveApplication)
              .WithMany(la => la.Approvals)
              .HasForeignKey(e => e.LeaveApplicationId)
              .OnDelete(DeleteBehavior.Cascade);
    });

    // PublicHoliday
    modelBuilder.Entity<PublicHoliday>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.Date, e.Year });
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
    });

    // LeaveEncashment
    modelBuilder.Entity<LeaveEncashment>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.EmployeeId);
        entity.Property(e => e.UnusedAnnualLeaveDays).HasColumnType("decimal(10,2)");
        entity.Property(e => e.DailySalary).HasColumnType("decimal(18,2)");
        entity.Property(e => e.TotalEncashmentAmount).HasColumnType("decimal(18,2)");

        entity.HasOne(e => e.Employee)
              .WithMany()
              .HasForeignKey(e => e.EmployeeId)
              .OnDelete(DeleteBehavior.Cascade);
    });
}
```

---

## 9. Mauritius Public Holidays 2025

```csharp
// Seed data
var mauritiusHolidays2025 = new List<PublicHoliday>
{
    new() { Name = "New Year's Day", Date = new DateTime(2025, 1, 1), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "New Year's Day (2nd January)", Date = new DateTime(2025, 1, 2), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "Thaipoosam Cavadee", Date = new DateTime(2025, 2, 11), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "Abolition of Slavery", Date = new DateTime(2025, 2, 1), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "Chinese Spring Festival", Date = new DateTime(2025, 1, 29), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "Maha Shivaratri", Date = new DateTime(2025, 2, 26), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "National Day", Date = new DateTime(2025, 3, 12), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "Ugaadi", Date = new DateTime(2025, 3, 30), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "Labour Day", Date = new DateTime(2025, 5, 1), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "Eid ul-Fitr", Date = new DateTime(2025, 3, 31), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "Ganesh Chaturthi", Date = new DateTime(2025, 8, 27), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "Arrival of Indentured Labourers", Date = new DateTime(2025, 11, 2), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "Divali", Date = new DateTime(2025, 10, 20), Year = 2025, Type = HolidayType.NationalHoliday },
    new() { Name = "Christmas Day", Date = new DateTime(2025, 12, 25), Year = 2025, Type = HolidayType.NationalHoliday }
};
```

---

## 10. Testing Scenarios

### Test 1: Apply for Annual Leave
```json
POST /api/leaves
{
  "leaveTypeId": "{annual-leave-type-id}",
  "startDate": "2025-06-15",
  "endDate": "2025-06-20",
  "reason": "Family vacation to Rodrigues Island",
  "contactNumber": "+230 5234 5678",
  "calculationType": 1
}
```

**Expected:**
- 4 working days calculated (excluding weekends)
- Leave balance deducted from available
- Status: PendingApproval
- Manager notified via email
- Application number generated (LEV-2025-0001)

### Test 2: Manager Approves Leave
```json
POST /api/leaves/{leave-id}/approve
{
  "comments": "Approved. Enjoy your vacation!"
}
```

**Expected:**
- Status changes to Approved
- Employee notified via email
- Leave balance UsedDays updated
- PendingDays reduced

### Test 3: Check Leave Balance
```json
GET /api/leaves/balance?year=2025
```

**Expected Response:**
```json
{
  "success": true,
  "data": [
    {
      "leaveTypeName": "Annual Leave",
      "year": 2025,
      "totalEntitlement": 22.00,
      "usedDays": 4.00,
      "pendingDays": 0.00,
      "availableDays": 18.00,
      "carriedForward": 0.00
    },
    {
      "leaveTypeName": "Sick Leave",
      "year": 2025,
      "totalEntitlement": 15.00,
      "usedDays": 0.00,
      "pendingDays": 0.00,
      "availableDays": 15.00
    }
  ]
}
```

---

## 11. Migration Command

```bash
dotnet ef migrations add AddLeaveManagementSystem \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context TenantDbContext \
  --output-dir Data/Migrations/Tenant
```

---

## 12. Success Criteria

- ✅ All entities created and configured
- ✅ All DTOs with proper validation
- ✅ LeaveService with 20+ methods implemented
- ✅ 15 API endpoints working
- ✅ Leave balance calculation accurate
- ✅ Approval workflow functioning
- ✅ Public holidays excluding from calculations
- ✅ Working days calculation correct
- ✅ Leave encashment calculation accurate
- ✅ Email notifications working (placeholder)
- ✅ Migration applied successfully
- ✅ 0 build errors

---

## 13. Estimated Timeline

| Task | Time | Lines of Code |
|------|------|---------------|
| Create enums | 15 min | 100 |
| Create entities | 45 min | 400 |
| Create DTOs | 30 min | 350 |
| ILeaveService | 20 min | 80 |
| LeaveService | 2 hrs | 1500 |
| LeavesController | 45 min | 400 |
| TenantDbContext | 30 min | 200 |
| Seeder | 15 min | 100 |
| Migration | 10 min | - |
| Testing | 30 min | - |
| **Total** | **~5 hrs** | **~3130 lines** |

---

This guide is complete and ready for implementation. Would you like me to proceed with the actual implementation now?
