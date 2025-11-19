# Phase 3B - Leave Management System
## Implementation Status Report

**Date:** November 1, 2025
**Current Status:** PARTIAL - Core Foundation Complete
**Next Session:** Complete remaining components

---

## ✅ COMPLETED (This Session)

### 1. Enums (5 files) - 100% Complete
- ✅ `src/HRMS.Core/Enums/LeaveTypeEnum.cs`
- ✅ `src/HRMS.Core/Enums/LeaveStatus.cs`
- ✅ `src/HRMS.Core/Enums/ApprovalStatus.cs`
- ✅ `src/HRMS.Core/Enums/HolidayType.cs`
- ✅ `src/HRMS.Core/Enums/LeaveCalculationType.cs`

### 2. Entities (1 of 6) - 17% Complete
- ✅ `src/HRMS.Core/Entities/Tenant/PublicHoliday.cs`

### 3. Documentation
- ✅ **PHASE3B_LEAVE_MANAGEMENT_IMPLEMENTATION_GUIDE.md** - Complete 40-page implementation guide with all code examples

---

## 🔄 IN PROGRESS / TODO

### Entities (Remaining 5)
Use the implementation guide for complete code.

**Priority 1 - Core Entities:**
1. ❌ `LeaveType.cs` - Master data for leave types
2. ❌ `LeaveBalance.cs` - Track balance per employee/type/year
3. ❌ `LeaveApplication.cs` - Main leave application entity

**Priority 2 - Enhanced Entities:**
4. ❌ `LeaveApproval.cs` - Multi-level approval tracking
5. ❌ `LeaveEncashment.cs` - Final settlement calculations

### DTOs (12 files)
All code provided in implementation guide:
1. ❌ CreateLeaveApplicationRequest.cs
2. ❌ ApproveLeaveRequest.cs
3. ❌ RejectLeaveRequest.cs
4. ❌ CancelLeaveRequest.cs
5. ❌ LeaveApplicationDto.cs
6. ❌ LeaveApplicationListDto.cs
7. ❌ LeaveBalanceDto.cs
8. ❌ LeaveApprovalDto.cs
9. ❌ LeaveCalendarDto.cs
10. ❌ LeaveTypeDto.cs
11. ❌ PublicHolidayDto.cs
12. ❌ LeaveEncashmentDto.cs

### Service Layer
1. ❌ ILeaveService interface (20+ methods)
2. ❌ LeaveService implementation (~1500 lines)

### Controllers
1. ❌ LeavesController (15 endpoints)
2. ❌ PublicHolidaysController (optional)

### Database
1. ❌ TenantDbContext configuration for Leave entities
2. ❌ Mauritius 2025 holidays seeder
3. ❌ EF Core migration

### Registration
1. ❌ Register ILeaveService in Program.cs

---

## 📋 QUICK START GUIDE FOR NEXT SESSION

### Step 1: Create Remaining Entities (10 minutes)

**Copy from implementation guide sections 2.2, 2.3, 2.4:**

```bash
# Create these files:
src/HRMS.Core/Entities/Tenant/LeaveType.cs
src/HRMS.Core/Entities/Tenant/LeaveBalance.cs
src/HRMS.Core/Entities/Tenant/LeaveApplication.cs
```

### Step 2: Create DTOs (15 minutes)

**Copy from implementation guide section 3:**

```bash
# Create all 12 DTO files in:
src/HRMS.Application/DTOs/
```

### Step 3: Create ILeaveService (5 minutes)

**Copy from implementation guide section 4:**

```bash
src/HRMS.Application/Interfaces/ILeaveService.cs
```

### Step 4: Implement LeaveService (45 minutes)

**This is the big one - ~1500 lines**

Create `src/HRMS.Infrastructure/Services/LeaveService.cs`

**Key methods to implement:**
```csharp
public class LeaveService : ILeaveService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<LeaveService> _logger;

    // Apply for Leave
    public async Task<LeaveApplicationDto> ApplyForLeaveAsync(
        Guid employeeId,
        CreateLeaveApplicationRequest request)
    {
        // 1. Validate request
        var validationError = await ValidateLeaveApplicationAsync(employeeId, request);
        if (validationError != null)
            throw new InvalidOperationException(validationError);

        // 2. Calculate working days
        var workingDays = await CalculateWorkingDaysAsync(
            request.StartDate,
            request.EndDate
        );

        // 3. Generate application number
        var appNumber = await GenerateApplicationNumberAsync();

        // 4. Create application
        var application = new LeaveApplication
        {
            ApplicationNumber = appNumber,
            EmployeeId = employeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalDays = workingDays,
            Reason = request.Reason,
            Status = LeaveStatus.PendingApproval,
            AppliedDate = DateTime.UtcNow
        };

        _context.LeaveApplications.Add(application);

        // 5. Update balance (pending)
        await UpdatePendingBalanceAsync(
            employeeId,
            request.LeaveTypeId,
            workingDays,
            isAdd: true
        );

        await _context.SaveChangesAsync();

        // 6. Send notification to manager (placeholder)
        // await _notificationService.NotifyManagerAsync(application);

        return await MapToDto(application);
    }

    // Calculate Working Days
    public async Task<decimal> CalculateWorkingDaysAsync(
        DateTime startDate,
        DateTime endDate)
    {
        var holidays = await _context.PublicHolidays
            .Where(h => h.Date >= startDate && h.Date <= endDate && h.IsActive)
            .Select(h => h.Date.Date)
            .ToListAsync();

        decimal workingDays = 0;
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
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

    // Validate Leave Application
    public async Task<string?> ValidateLeaveApplicationAsync(
        Guid employeeId,
        CreateLeaveApplicationRequest request)
    {
        var errors = new List<string>();

        // Cannot apply for past dates
        if (request.StartDate.Date < DateTime.UtcNow.Date)
            errors.Add("Cannot apply for leave in the past");

        // End date >= start date
        if (request.EndDate < request.StartDate)
            errors.Add("End date cannot be before start date");

        // Check balance
        var balance = await GetOrCreateBalanceAsync(
            employeeId,
            request.LeaveTypeId,
            request.StartDate.Year
        );

        var workingDays = await CalculateWorkingDaysAsync(
            request.StartDate,
            request.EndDate
        );

        if (balance.AvailableDays < workingDays)
            errors.Add($"Insufficient balance. Available: {balance.AvailableDays}, Requested: {workingDays}");

        // Check overlapping
        var hasOverlap = await HasOverlappingLeaveAsync(
            employeeId,
            request.StartDate,
            request.EndDate
        );
        if (hasOverlap)
            errors.Add("Overlapping leave application exists");

        return errors.Any() ? string.Join("; ", errors) : null;
    }

    // ... implement remaining 17+ methods
}
```

**Full implementation available in:** `PHASE3B_LEAVE_MANAGEMENT_IMPLEMENTATION_GUIDE.md` section 6

### Step 5: Create LeavesController (20 minutes)

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _leaveService;
    private readonly ILogger<LeavesController> _logger;

    // 1. Apply for leave
    [HttpPost]
    public async Task<IActionResult> ApplyForLeave(
        [FromBody] CreateLeaveApplicationRequest request)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId(); // From JWT claims
            var leave = await _leaveService.ApplyForLeaveAsync(employeeId, request);

            return CreatedAtAction(
                nameof(GetLeaveById),
                new { id = leave.Id },
                new { success = true, data = leave }
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // 2. Get my leaves
    [HttpGet("my-leaves")]
    public async Task<IActionResult> GetMyLeaves([FromQuery] int? year = null)
    {
        var employeeId = GetCurrentEmployeeId();
        var leaves = await _leaveService.GetMyLeavesAsync(employeeId, year);
        return Ok(new { success = true, data = leaves });
    }

    // 3. Get leave balance
    [HttpGet("balance")]
    public async Task<IActionResult> GetLeaveBalance([FromQuery] int? year = null)
    {
        var employeeId = GetCurrentEmployeeId();
        var balances = await _leaveService.GetLeaveBalanceAsync(employeeId, year);
        return Ok(new { success = true, data = balances });
    }

    // 4. Approve leave (manager)
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveLeave(
        Guid id,
        [FromBody] ApproveLeaveRequest request)
    {
        var managerId = GetCurrentEmployeeId();
        var leave = await _leaveService.ApproveLeaveAsync(id, managerId, request);
        return Ok(new { success = true, data = leave });
    }

    // ... implement remaining 11 endpoints
}
```

### Step 6: Update TenantDbContext (15 minutes)

```csharp
// Add DbSets
public DbSet<LeaveType> LeaveTypes { get; set; }
public DbSet<LeaveBalance> LeaveBalances { get; set; }
public DbSet<LeaveApplication> LeaveApplications { get; set; }
public DbSet<PublicHoliday> PublicHolidays { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configurations

    // LeaveType configuration
    modelBuilder.Entity<LeaveType>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.TypeCode);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        entity.Property(e => e.DefaultEntitlement).HasColumnType("decimal(10,2)");
        entity.HasQueryFilter(e => !e.IsDeleted);
    });

    // LeaveBalance configuration
    modelBuilder.Entity<LeaveBalance>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId, e.Year }).IsUnique();

        entity.Property(e => e.TotalEntitlement).HasColumnType("decimal(10,2)");
        entity.Property(e => e.UsedDays).HasColumnType("decimal(10,2)");
        entity.Property(e => e.PendingDays).HasColumnType("decimal(10,2)");
        entity.Property(e => e.CarriedForward).HasColumnType("decimal(10,2)");

        entity.HasOne(e => e.Employee)
              .WithMany()
              .HasForeignKey(e => e.EmployeeId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.LeaveType)
              .WithMany(lt => lt.LeaveBalances)
              .HasForeignKey(e => e.LeaveTypeId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.Ignore(e => e.AvailableDays);
        entity.HasQueryFilter(e => !e.IsDeleted);
    });

    // LeaveApplication configuration
    modelBuilder.Entity<LeaveApplication>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.ApplicationNumber).IsUnique();
        entity.HasIndex(e => new { e.EmployeeId, e.StartDate, e.EndDate });

        entity.Property(e => e.ApplicationNumber).IsRequired().HasMaxLength(50);
        entity.Property(e => e.TotalDays).HasColumnType("decimal(10,2)");
        entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);

        entity.HasOne(e => e.Employee)
              .WithMany()
              .HasForeignKey(e => e.EmployeeId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.LeaveType)
              .WithMany(lt => lt.LeaveApplications)
              .HasForeignKey(e => e.LeaveTypeId)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasQueryFilter(e => !e.IsDeleted);
    });

    // PublicHoliday configuration
    modelBuilder.Entity<PublicHoliday>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.Date, e.Year });
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.HasQueryFilter(e => !e.IsDeleted);
    });
}
```

### Step 7: Register Services (2 minutes)

In `Program.cs`:
```csharp
builder.Services.AddScoped<ILeaveService, LeaveService>();
```

### Step 8: Create Migration (5 minutes)

```bash
dotnet ef migrations add AddLeaveManagementSystem \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context TenantDbContext \
  --output-dir Data/Migrations/Tenant
```

### Step 9: Build and Test (10 minutes)

```bash
dotnet build
dotnet run --project src/HRMS.API
```

**Test in Swagger:**
1. Login as admin
2. Create a tenant
3. Create an employee
4. Initialize leave balance for employee
5. Apply for leave
6. Check balance

---

## 📊 IMPLEMENTATION PROGRESS

| Component | Files | Status | %  |
|-----------|-------|--------|-----|
| Enums | 5/5 | ✅ Complete | 100% |
| Entities | 1/6 | 🔄 In Progress | 17% |
| DTOs | 0/12 | ❌ Pending | 0% |
| Services | 0/2 | ❌ Pending | 0% |
| Controllers | 0/2 | ❌ Pending | 0% |
| Database Config | 0/1 | ❌ Pending | 0% |
| Migration | 0/1 | ❌ Pending | 0% |
| **OVERALL** | **6/34** | **🔄 18%** | **18%** |

---

## 🎯 ESTIMATED TIME TO COMPLETION

| Task | Time |
|------|------|
| Remaining Entities | 20 min |
| All DTOs | 25 min |
| ILeaveService | 10 min |
| LeaveService | 60 min |
| LeavesController | 30 min |
| DbContext Config | 20 min |
| Migration | 5 min |
| Testing | 15 min |
| **TOTAL** | **~3 hours** |

---

## 📖 REFERENCE DOCUMENTS

1. **PHASE3B_LEAVE_MANAGEMENT_IMPLEMENTATION_GUIDE.md**
   - Complete 40-page guide
   - All code examples
   - Business logic explained
   - Testing scenarios
   - Mauritius holidays list

2. **PHASE2_EMPLOYEE_MANAGEMENT_COMPLETION_REPORT.md**
   - Reference for similar patterns
   - Employee entity structure
   - Service patterns

3. **PHASE2_EMPLOYEE_MANAGEMENT_TEST_GUIDE.md**
   - API testing examples
   - Swagger usage

---

## ✅ ACCEPTANCE CRITERIA

When complete, the system should:

- ✅ Allow employees to apply for leave
- ✅ Calculate working days correctly (exclude weekends & holidays)
- ✅ Track leave balance (entitled, used, pending, available)
- ✅ Validate leave applications (sufficient balance, no overlap, etc.)
- ✅ Support manager approval workflow
- ✅ Provide leave calendar view
- ✅ Handle Mauritius public holidays
- ✅ Calculate pro-rated entitlement for mid-year joiners
- ✅ Support 9 leave types (Annual, Sick, Casual, Maternity, etc.)
- ✅ Build with 0 errors
- ✅ All endpoints tested in Swagger

---

## 🔥 PRIORITY ORDER FOR NEXT SESSION

1. **HIGH PRIORITY** - Core functionality:
   - Create 3 core entities (LeaveType, LeaveBalance, LeaveApplication)
   - Create 6 core DTOs (Create, List, Balance)
   - Implement core LeaveService methods (Apply, Balance, Validate)
   - Create basic LeavesController (5 endpoints)
   - Configure DbContext
   - Create migration
   - **This gets you a WORKING system** ⭐

2. **MEDIUM PRIORITY** - Enhanced features:
   - Add LeaveApproval entity for multi-level approvals
   - Implement approval workflow
   - Add calendar endpoints
   - Seed Mauritius holidays

3. **LOW PRIORITY** - Advanced features:
   - LeaveEncashment entity
   - Email notifications
   - Advanced reporting
   - Leave carry-forward logic

---

## 💡 HELPFUL TIPS

1. **Use the implementation guide** - Don't rewrite from scratch, copy the provided code
2. **Build frequently** - After entities, after DTOs, after service, etc.
3. **Test as you go** - Use Swagger to test each endpoint
4. **Follow the pattern** - Look at EmployeeService for similar patterns
5. **DbContext last** - Do this after all entities are created
6. **Migration errors** - If migration fails, check TenantDbContextFactory exists

---

## 📞 NEXT STEPS

**To continue implementation:**

1. Start a new conversation with Claude Code
2. Share this document: `PHASE3B_IMPLEMENTATION_STATUS.md`
3. Share the guide: `PHASE3B_LEAVE_MANAGEMENT_IMPLEMENTATION_GUIDE.md`
4. Ask Claude to "Complete Phase 3B Leave Management using the implementation guide"

**Claude will:**
- Create all remaining entities
- Create all DTOs
- Implement LeaveService
- Create LeavesController
- Configure database
- Create migration
- Build and test

**Estimated:** 3-4 hours to completion

---

**Status:** Foundation laid, ready for next session
**Build Status:** Clean (0 errors, enums + 1 entity created)
**Documentation:** Complete and detailed
**Next AI:** Follow the implementation guide step-by-step
