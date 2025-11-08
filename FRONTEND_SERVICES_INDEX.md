# Frontend Services & Models - Complete Index

## Generated: November 7, 2024

This document index provides navigation to comprehensive analysis of HRMS frontend services and models used by the Employee module.

---

## Available Documents

### 1. FRONTEND_SERVICES_ANALYSIS.md (42 KB, 1535 lines)
**Complete technical reference with full code listings**

Covers 15 major sections:
1. Employee Service - CRUD operations with signals
2. Employee Model - Interface definitions
3. Employee Draft Service - Draft management with offline support
4. Address Service - Geographic lookups
5. Address Models - DTOs for districts, villages, postal codes
6. Department Service - Organization hierarchy management
7. Location Service - Physical work locations
8. Payroll Service - Payroll cycles and timesheet integration
9. Payroll Model - Mauritius tax deductions and calculations
10. Timesheet Service - Complete lifecycle management
11. Timesheet Model - Statuses, periods, and enums
12. Biometric Device Service - Device management and sync
13. Dashboard Service - Statistics and alerts
14. Auth Service - Authentication and role-based access
15. Tenant Service - Multi-tenancy management

Each section includes:
- Complete source code
- API endpoints table
- Reactive state signals
- Key interfaces and models
- Helper functions
- Usage patterns

### 2. FRONTEND_SERVICES_QUICK_REFERENCE.md (14 KB, 439 lines)
**Fast lookup guide for developers**

Quick-access sections:
- Service file locations
- API endpoint summary (organized by module)
- Reactive state (signals) reference
- Key models and enums
- LocalStorage usage patterns
- Error handling patterns
- Usage examples with code snippets
- Integration points
- Summary statistics

---

## Service Architecture Overview

### Core Services (in `/src/app/core/services/`)
```
employee.service.ts (120 lines)
  - getEmployees(): Get all
  - getEmployeeById(id): Get single
  - createEmployee(request): Create
  - updateEmployee(id, employee): Update
  - deleteEmployee(id): Delete
  - Signals: employees, loading, error

employee-draft.service.ts (130 lines)
  - getAllDrafts(): Get all drafts
  - getDraftById(id): Get by ID
  - saveDraft(request): Save/update
  - finalizeDraft(id): Finalize to employee
  - deleteDraft(id): Delete
  - LocalStorage helpers: save, load, clear, list

payroll.service.ts (115 lines)
  - getPayrollCycles(): Get cycles
  - getPayrollByCycle(cycleId): Get by cycle
  - getEmployeePayslips(employeeId): Get payslips
  - getPayslip(payslipId): Get single
  - processPayroll(cycleId): Process
  - downloadPayslip(payslipId): Download PDF
  - calculatePayrollFromTimesheets(request): Calculate
  - previewPayrollFromTimesheets(request): Preview
  - processBatchFromTimesheets(request): Batch
  - Signals: payrolls, cycles, loading, currentPayrollResult

timesheet.service.ts (337 lines)
  - Employee methods: getMyTimesheets, submitTimesheet, updateTimesheetEntry
  - Manager methods: getPendingApprovals, approveTimesheet, rejectTimesheet
  - Admin methods: generateTimesheet, lockTimesheet, deleteTimesheet
  - Adjustment methods: createAdjustment, approveAdjustment, rejectAdjustment
  - Stats methods: getTimesheetStats, getPeriodDates, formatHours
  - Signals: timesheets, currentTimesheet, pendingApprovals, stats, loading

dashboard.service.ts (247 lines)
  - getStats(): Get dashboard statistics
  - getAlerts(): Get urgent alerts
  - getRecentActivity(limit): Get activity feed
  - getDepartmentHeadcountChart(): Chart data
  - getEmployeeGrowthChart(): Chart data
  - getEmployeeTypeDistribution(): Chart data
  - getUpcomingBirthdays(days): Birthday list
  - Signals: stats, loading, error

auth.service.ts (100+ lines)
  - login(credentials): Authenticate user
  - Signals: user, token, loading
  - Computed signals: isAuthenticated, isSuperAdmin, isTenantAdmin, isHR, isManager, isEmployee
  - localStorage management for token and user

tenant.service.ts (68 lines)
  - getTenants(): Get all
  - getTenantById(id): Get by ID
  - createTenant(request): Create
  - updateTenant(id, tenant): Update
  - suspendTenant(id): Suspend
  - deleteTenant(id): Delete
  - Signals: tenants, loading
```

### Feature Services
```
address.service.ts (57 lines, in /src/app/services/)
  - getDistricts(): All districts
  - getVillagesByDistrict(districtId): Cascade lookup
  - getVillages(): All villages
  - searchPostalCodes(code): Autocomplete search
  - getPostalCodes(): All postal codes
  - lookupPostalCode(code): Postal code lookup

department.service.ts (123 lines, /features/tenant/organization/departments/)
  - getAll(): All departments
  - getById(id): By ID
  - create(department): Create
  - update(id, department): Update
  - delete(id): Delete
  - getHierarchy(): Hierarchy tree
  - getDropdown(): Dropdown options

location.service.ts (253 lines, /features/tenant/organization/locations/)
  - getAll(): All locations
  - getById(id): By ID
  - create(location): Create
  - update(id, location): Update
  - delete(id): Delete (soft)
  - getSummaries(): Summaries
  - getDropdown(): Dropdown
  - getByType(locationType): Filter
  - activate(id): Activate
  - deactivate(id): Deactivate
  - getStatistics(id): Statistics

biometric-device.service.ts (387 lines, /features/tenant/organization/devices/)
  - 24 API operations:
    - CRUD: getDevices, getDevice, createDevice, updateDevice, deleteDevice
    - Sync: testConnection, syncDevice, getSyncStatus, getAllSyncStatuses
    - Organization: getDevicesByLocation, getDevicesByType
    - Control: activateDevice, deactivateDevice, enableSync, disableSync
    - Maintenance: getDeviceStatistics, getAnomalies, updateDeviceConfig, restartDevice, clearDeviceData
    - Employee Auth: getAuthorizedEmployees, authorizeEmployee, revokeEmployeeAuthorization
    - Diagnostics: getSyncLogs, getAnomalies
```

### Core Models (in `/src/app/core/models/`)
```
employee.model.ts
  - Employee interface (11 fields)
  - EmployeeStatus enum: Active, OnLeave, Suspended, Terminated
  - CreateEmployeeRequest interface

payroll.model.ts (185 lines)
  - Payroll interface
  - Payslip interface
  - PayslipItem interface
  - PayrollCycle interface
  - PayrollStatus enum: Draft, Processed, Paid, Cancelled
  - PayrollResult interface (comprehensive calculation breakdown)
  - MauritiusDeductions interface (9 deduction types)
  - CalculateFromTimesheetsRequest interface
  - BatchCalculateFromTimesheetsRequest interface
  - BatchPayrollResult interface
  - Helper functions: formatCurrency, formatHours, formatPercentage, calculateTaxBracketLabel

timesheet.model.ts (216 lines)
  - Timesheet interface
  - TimesheetEntry interface
  - TimesheetAdjustment interface
  - TimesheetComment interface
  - TimesheetStats interface
  - TimesheetStatus enum: Draft, Submitted, Approved, Rejected, Locked
  - PeriodType enum: Weekly, BiWeekly, Monthly
  - DayType enum: 8 day types (Regular, Weekend, Holiday, etc.)
  - AdjustmentType enum: 4 adjustment types
  - AdjustmentStatus enum: 3 statuses
  - Request/Response DTOs: GenerateTimesheetRequest, UpdateTimesheetEntryRequest, etc.
  - Helper functions: getStatusLabel, getStatusColor, canEditTimesheet, etc.

tenant.model.ts
  - Tenant interface
  - CreateTenantRequest interface

user.model.ts
  - User interface
  - LoginRequest interface
  - LoginResponse interface
  - UserRole enum: SuperAdmin, TenantAdmin, HR, Manager, Employee

attendance.model.ts
  - Attendance interface

leave.model.ts
  - Leave interface
  - Leave status enums
```

### Feature Models
```
address.models.ts (34 lines, /src/app/models/)
  - DistrictDto interface (bilingual support)
  - VillageDto interface
  - PostalCodeDto interface

department.service.ts DTOs
  - DepartmentDto interface
  - CreateDepartmentDto interface
  - UpdateDepartmentDto interface
  - DepartmentDropdownDto interface
  - DepartmentHierarchyDto interface (tree structure)

location.service.ts DTOs
  - LocationDto interface (20+ fields)
  - CreateLocationDto interface
  - UpdateLocationDto interface
  - LocationDropdownDto interface
  - LocationSummaryDto interface

biometric-device.service.ts DTOs
  - BiometricDeviceDto interface (45 fields)
  - CreateBiometricDeviceDto interface
  - UpdateBiometricDeviceDto interface
  - TestConnectionDto interface
  - ConnectionTestResult interface
  - SyncResult interface
  - DeviceSyncStatusDto interface (10+ sync metrics)
```

---

## API Endpoints Summary

### Total: 100+ Endpoints

**Breakdown:**
- Employee CRUD: 5 endpoints
- Employee Drafts: 5 endpoints
- Address Lookups: 5 endpoints
- Departments: 7 endpoints
- Locations: 10 endpoints
- Biometric Devices: 24 endpoints
- Payroll: 9 endpoints
- Timesheet: 20 endpoints
- Dashboard: 8 endpoints
- Auth: 2 endpoints
- Tenants: 6 endpoints

---

## Reactive State Management

### Total: 40+ Signals

**By Service:**
- EmployeeService: 3 signals
- PayrollService: 4 signals
- TimesheetService: 5 signals
- DashboardService: 3 signals
- AuthService: 8 signals (3 private + 5 computed)
- TenantService: 2 signals

**State Categories:**
- Data states (employees, payrolls, timesheets, tenants)
- Loading states (boolean flags)
- Error states (string | null)
- Auth states (user, token)
- Computed role states (isSuperAdmin, isManager, etc.)

---

## Key Features & Integrations

### 1. Employee Management
- Complete CRUD operations
- Draft support with offline capability
- Address lookups (districts, villages, postal codes)
- Department and location associations
- Manager tracking

### 2. Timesheet Module
- Employee, manager, and admin views
- Draft, submitted, approved, rejected, locked statuses
- Multiple period types (weekly, bi-weekly, monthly)
- Adjustments with approval workflow
- Comment system for collaboration
- Bulk approval for managers

### 3. Payroll Processing
- Payroll cycle management
- Timesheet-based calculation
- Comprehensive Mauritius statutory deductions:
  - NPF (National Pension Fund)
  - NSF (National Savings Fund)
  - CSG (Corporate Social Guarantee)
  - PAYE Tax
  - PRGF Contribution
  - Training Levy
- Allowances (housing, transport, meal, mobile, other)
- Batch processing capabilities
- Payslip generation and download

### 4. Organization Management
- Department hierarchies
- Physical locations with working hours
- Biometric device network
- Device sync management
- Employee authorization per device

### 5. Attendance & Biometric
- Multi-device support
- Real-time sync status monitoring
- Offline alert capabilities
- Sync success rate tracking
- Device diagnostics (logs, anomalies)
- Employee-device authorization

### 6. Dashboard & Analytics
- Employee statistics
- Department headcount charts
- Employee growth trends
- Type distribution analysis
- Alerts and notifications
- Activity feed
- Upcoming birthdays

### 7. Authentication & Authorization
- SuperAdmin and Tenant logins
- 5 role-based access levels
- Computed signals for role checking
- Persistent authentication (localStorage)
- Subdomain support for multi-tenancy

---

## Error Handling Strategy

All services implement consistent error handling:

```typescript
// Standard pattern
.pipe(
  map(response => {
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Operation failed');
    }
    return response.data;
  }),
  tap(data => updateSignal(data)),  // Update state on success
  catchError(error => {
    updateErrorSignal(error.message);  // Store error in signal
    return throwError(() => error);    // Propagate to caller
  })
)
```

---

## Data Persistence

### LocalStorage
- **Auth:** access_token, user, tenant_subdomain
- **Drafts:** employee_draft_* (keyed by draft ID)
- **Format:** JSON serialized with timestamps

### Backend
- Permanent storage for all entities
- Soft deletes for non-critical items
- Audit trails (createdAt, updatedAt, createdBy, updatedBy)

---

## Component Integration Patterns

### Using Signals
```typescript
// In component
employees = this.employeeService.employees;
loading = this.employeeService.loading;

// In template
<div *ngIf="loading()">Loading...</div>
<div *ngFor="let emp of employees()">{{ emp.firstName }}</div>
```

### Using Observables
```typescript
// In component
ngOnInit() {
  this.employees$ = this.employeeService.getEmployees();
}

// In template
<div *ngIf="employees$ | async as employees">
  <div *ngFor="let emp of employees">{{ emp.firstName }}</div>
</div>
```

### Combining Both
```typescript
// Get async data and update signals
this.timesheetService.getMyTimesheets().subscribe(ts => {
  // Signal automatically updated by service
});

// Access current state via signal
const currentTS = this.timesheetService.currentTimesheet();
```

---

## Testing Recommendations

### Service Unit Tests
- Mock HttpClient
- Test signal updates on success/error
- Verify API endpoint URLs
- Test error handling and propagation
- Validate LocalStorage operations

### Component Integration Tests
- Mock services with test data
- Test signal consumption in templates
- Verify error state display
- Test form submissions with draft service

### E2E Tests
- Test complete workflows (employee creation → timesheet → payroll)
- Verify multi-role access control
- Test device sync scenarios
- Validate dashboard statistics

---

## Performance Considerations

### 1. Reactive Updates
- Signals provide fine-grained reactivity
- Only affected components re-render
- Computed signals cache results

### 2. Data Loading
- Lazy load large datasets (pagination recommended)
- Cache dropdown options (departments, locations)
- Batch operations for bulk updates

### 3. Network Optimization
- Use GET parameters for filters
- Implement request cancellation
- Consider pagination for list endpoints

---

## Security Notes

### 1. Authentication
- JWT tokens stored in localStorage
- Role-based access control via computed signals
- Subdomain-based multi-tenancy

### 2. Authorization
- Services check auth state before operations
- Components protected by route guards
- Role-based endpoint access

### 3. Data Validation
- DTOs enforce type safety
- API responses validated for success flag
- Error messages sanitized

---

## Migration Guide

### From Old Services to New
If updating existing components:

1. **Replace service calls:**
   ```typescript
   // Old: Direct HTTP calls
   // New: Use typed service methods
   this.employeeService.getEmployees()
   ```

2. **Update state access:**
   ```typescript
   // Old: Component state
   // New: Service signals
   employees = this.employeeService.employees;
   ```

3. **Update error handling:**
   ```typescript
   // Old: Subscribe error callback
   // New: Check error signal
   if (this.employeeService.error()) { ... }
   ```

---

## Summary

| Aspect | Count | Notes |
|--------|-------|-------|
| Core Services | 7 | Employee, Draft, Payroll, Timesheet, Dashboard, Auth, Tenant |
| Feature Services | 4 | Address, Department, Location, Biometric |
| Total Services | 11 | Core + Feature |
| API Endpoints | 100+ | RESTful operations |
| Signals | 40+ | Reactive state |
| Models/DTOs | 40+ | Type definitions |
| Enums | 10+ | Status and type values |
| Lines of Code | 2000+ | Services + models |

---

## Document Navigation

**For Specific Information:**
- Employee CRUD details → FRONTEND_SERVICES_ANALYSIS.md Section 1-2
- Payroll calculations → FRONTEND_SERVICES_ANALYSIS.md Section 8-9
- Timesheet workflow → FRONTEND_SERVICES_ANALYSIS.md Section 10-11
- Biometric device sync → FRONTEND_SERVICES_ANALYSIS.md Section 12
- Quick API reference → FRONTEND_SERVICES_QUICK_REFERENCE.md

**For Code Examples:**
- Cascading dropdowns → FRONTEND_SERVICES_QUICK_REFERENCE.md (Address section)
- Payroll from timesheets → FRONTEND_SERVICES_QUICK_REFERENCE.md (Examples section)
- Device management → FRONTEND_SERVICES_QUICK_REFERENCE.md (Examples section)

**For Integration:**
- Service dependencies → FRONTEND_SERVICES_QUICK_REFERENCE.md (Integration Points section)
- Reactive patterns → This document (Component Integration Patterns section)
- Error handling → FRONTEND_SERVICES_QUICK_REFERENCE.md (Error Handling Pattern section)

---

Generated: November 7, 2024
File Locations:
- Analysis: /workspaces/HRAPP/FRONTEND_SERVICES_ANALYSIS.md
- Quick Reference: /workspaces/HRAPP/FRONTEND_SERVICES_QUICK_REFERENCE.md
- Index: /workspaces/HRAPP/FRONTEND_SERVICES_INDEX.md

