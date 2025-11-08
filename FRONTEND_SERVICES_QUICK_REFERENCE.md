# Frontend Services - Quick Reference

## Service Locations & Files

### Core Services (in `/src/app/core/services/`)
1. **employee.service.ts** - CRUD operations for employees
2. **employee-draft.service.ts** - Draft management with localStorage support
3. **payroll.service.ts** - Payroll cycles, payslips, and timesheet calculations
4. **timesheet.service.ts** - Complete timesheet lifecycle management
5. **dashboard.service.ts** - Dashboard stats, alerts, and charts
6. **auth.service.ts** - Authentication with 5 role-based signals
7. **tenant.service.ts** - Tenant management (SuperAdmin)

### Feature Services
1. **address.service.ts** (`/src/app/services/`) - Address lookups
2. **department.service.ts** (`/features/tenant/organization/departments/services/`)
3. **location.service.ts** (`/features/tenant/organization/locations/`)
4. **biometric-device.service.ts** (`/features/tenant/organization/devices/`)

### Core Models (in `/src/app/core/models/`)
- employee.model.ts
- address.models.ts (in `/src/app/models/`)
- payroll.model.ts
- timesheet.model.ts
- tenant.model.ts
- user.model.ts
- attendance.model.ts
- leave.model.ts

---

## API Endpoint Summary

### Employee Management
```
GET    /employees                      - List all
GET    /employees/{id}                 - Get by ID
POST   /employees                      - Create
PUT    /employees/{id}                 - Update
DELETE /employees/{id}                 - Delete
```

### Employee Drafts
```
GET    /employee-drafts                - List all
GET    /employee-drafts/{id}           - Get by ID
POST   /employee-drafts                - Save/create
POST   /employee-drafts/{id}/finalize  - Finalize to employee
DELETE /employee-drafts/{id}           - Delete
```

### Address Lookups
```
GET    /address-lookup/districts                          - All districts
GET    /address-lookup/districts/{id}/villages            - Villages by district
GET    /address-lookup/villages                           - All villages
GET    /address-lookup/postal-codes                       - All postal codes
GET    /address-lookup/postal-codes/search?code={code}    - Search postal codes
```

### Departments
```
GET    /api/department                 - List all
GET    /api/department/{id}            - Get by ID
POST   /api/department                 - Create
PUT    /api/department/{id}            - Update
DELETE /api/department/{id}            - Delete
GET    /api/department/hierarchy       - Hierarchy tree
GET    /api/department/dropdown        - Dropdown options
```

### Locations
```
GET    /api/locations                          - List all
GET    /api/locations/{id}                     - Get by ID
POST   /api/locations                          - Create
PUT    /api/locations/{id}                     - Update
DELETE /api/locations/{id}                     - Delete
GET    /api/locations/summaries                - Summaries
GET    /api/locations/dropdown                 - Dropdown
GET    /api/locations/by-type/{type}           - By type
PATCH  /api/locations/{id}/activate            - Activate
PATCH  /api/locations/{id}/deactivate          - Deactivate
GET    /api/locations/{id}/statistics          - Statistics
```

### Biometric Devices (24 endpoints)
```
CRUD Operations:
GET    /api/biometric-devices                  - List
GET    /api/biometric-devices/{id}             - Get
POST   /api/biometric-devices                  - Create
PUT    /api/biometric-devices/{id}             - Update
DELETE /api/biometric-devices/{id}             - Delete

Sync & Status:
POST   /api/biometric-devices/test-connection               - Test connection
POST   /api/biometric-devices/{id}/sync                     - Manual sync
GET    /api/biometric-devices/{id}/sync-status              - Get sync status
GET    /api/biometric-devices/sync-statuses                 - All sync statuses
GET    /api/biometric-devices/{id}/sync-logs                - Sync logs

Organization:
GET    /api/biometric-devices/by-location/{id}             - By location
GET    /api/biometric-devices/by-type/{type}               - By type

Control:
PATCH  /api/biometric-devices/{id}/activate                - Activate
PATCH  /api/biometric-devices/{id}/deactivate              - Deactivate
PATCH  /api/biometric-devices/{id}/enable-sync             - Enable sync
PATCH  /api/biometric-devices/{id}/disable-sync            - Disable sync

Maintenance:
GET    /api/biometric-devices/{id}/statistics              - Statistics
GET    /api/biometric-devices/{id}/anomalies               - Anomalies
PATCH  /api/biometric-devices/{id}/config                  - Update config
POST   /api/biometric-devices/{id}/restart                 - Restart
POST   /api/biometric-devices/{id}/clear-data              - Clear data

Employee Authorization:
GET    /api/biometric-devices/{id}/authorized-employees    - List authorized
POST   /api/biometric-devices/{id}/authorize-employee/{empId}   - Authorize
DELETE /api/biometric-devices/{id}/revoke-employee/{empId}      - Revoke
```

### Payroll
```
GET    /payroll/cycles                          - Get cycles
GET    /payroll/cycles/{id}                     - By cycle
GET    /payroll/employee/{id}/payslips          - Employee payslips
GET    /payroll/payslips/{id}                   - Get payslip
POST   /payroll/cycles/{id}/process             - Process payroll
GET    /payroll/payslips/{id}/download          - Download PDF

Timesheet Integration:
POST   /payroll/calculate-from-timesheets       - Calculate single
POST   /payroll/preview-from-timesheets         - Preview calculation
POST   /payroll/process-batch-from-timesheets   - Batch process
```

### Timesheet (20 endpoints)
```
Employee:
GET    /timesheet/my-timesheets         - My timesheets
GET    /timesheet/{id}                  - Get by ID
POST   /timesheet/{id}/submit           - Submit
PUT    /timesheet/entries/{id}          - Update entry
POST   /timesheet/{id}/comments         - Add comment

Manager:
GET    /timesheet/pending-approvals     - Pending approvals
GET    /timesheet/employee/{id}         - Employee timesheets
POST   /timesheet/{id}/approve          - Approve
POST   /timesheet/{id}/reject           - Reject
POST   /timesheet/bulk-approve          - Bulk approve

Admin:
POST   /timesheet/generate              - Generate
POST   /timesheet/{id}/regenerate       - Regenerate
GET    /timesheet/all                   - All timesheets
POST   /timesheet/{id}/lock             - Lock
POST   /timesheet/{id}/unlock           - Unlock
DELETE /timesheet/{id}                  - Delete

Adjustments:
POST   /timesheet/entries/{id}/adjustments           - Create
POST   /timesheet/adjustments/{id}/approve           - Approve
POST   /timesheet/adjustments/{id}/reject            - Reject

Stats:
GET    /timesheet/stats                 - Statistics
```

### Dashboard
```
GET    /dashboard/stats                                   - Statistics
GET    /dashboard/departments                             - Departments
GET    /dashboard/alerts                                  - Alerts
GET    /dashboard/recent-activity                         - Activity feed
GET    /dashboard/charts/department-headcount             - Chart
GET    /dashboard/charts/employee-growth                  - Chart
GET    /dashboard/charts/employee-type-distribution       - Chart
GET    /dashboard/upcoming-birthdays                      - Birthdays
```

### Auth
```
POST   /auth/login                      - SuperAdmin login
POST   /auth/tenant/login               - Tenant employee login
```

---

## Reactive State (Signals)

### EmployeeService
```typescript
employees: Signal<Employee[]>
loading: Signal<boolean>
error: Signal<string | null>
```

### PayrollService
```typescript
payrolls: Signal<Payroll[]>
cycles: Signal<PayrollCycle[]>
loading: Signal<boolean>
currentPayrollResult: Signal<PayrollResult | null>
```

### TimesheetService
```typescript
timesheets: Signal<Timesheet[]>
currentTimesheet: Signal<Timesheet | null>
pendingApprovals: Signal<Timesheet[]>
stats: Signal<TimesheetStats | null>
loading: Signal<boolean>
```

### DashboardService
```typescript
stats: Signal<DashboardStats | null>
loading: Signal<boolean>
error: Signal<string | null>
```

### AuthService
```typescript
user: Signal<User | null>
token: Signal<string | null>
loading: Signal<boolean>
isAuthenticated: Signal<boolean> // computed
isSuperAdmin: Signal<boolean>    // computed
isTenantAdmin: Signal<boolean>   // computed
isHR: Signal<boolean>            // computed
isManager: Signal<boolean>       // computed
isEmployee: Signal<boolean>      // computed
```

### TenantService
```typescript
tenants: Signal<Tenant[]>
loading: Signal<boolean>
```

---

## Key Models & Enums

### Employee
- Statuses: Active, OnLeave, Suspended, Terminated

### Payroll
- Status: Draft, Processed, Paid, Cancelled
- Includes Mauritius tax deductions:
  - NPF (Employee/Employer)
  - NSF (Employee/Employer)
  - CSG (Employee/Employer)
  - PAYE Tax
  - PRGF Contribution
  - Training Levy

### Timesheet
- Status: Draft, Submitted, Approved, Rejected, Locked
- Period: Weekly, BiWeekly, Monthly
- DayType: Regular, Weekend, Holiday, SickLeave, AnnualLeave, CasualLeave, UnpaidLeave, Absent
- AdjustmentType: ManualCorrection, SystemCorrection, AttendanceUpdate, LeaveUpdate
- AdjustmentStatus: Pending, Approved, Rejected

### Address
- District, Village, PostalCode with bilingual support (French)

### Department
- Supports hierarchies with parent departments
- Includes employee count and department head tracking

### Location
- Location types, working hours (JSON), timezone
- Device and employee counts
- Geographic coordinates

### BiometricDevice
- Multiple device types and sync strategies
- Offline alert capabilities
- Sync success rate tracking
- Employee authorization management

---

## LocalStorage Usage

### EmployeeDraftService
```typescript
// Key format: employee_draft_{draftId}
// Value: JSON serialized form data with _savedAt timestamp
localStorage.setItem(key, JSON.stringify({...formData, _savedAt: ISO string}))
localStorage.getItem(key)
localStorage.removeItem(key)
```

### AuthService
```typescript
localStorage.setItem('access_token', token)
localStorage.setItem('user', JSON.stringify(user))
localStorage.setItem('tenant_subdomain', subdomain) // Optional for tenant employees
```

---

## Error Handling Pattern

All services follow consistent error handling:
```typescript
.pipe(
  map(response => {
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Default error');
    }
    return response.data;
  }),
  tap(data => updateSignal(data)),
  catchError(error => {
    updateErrorSignal(error.message);
    return throwError(() => error);
  })
)
```

---

## Usage Examples

### Get Employees
```typescript
constructor(private employeeService: EmployeeService) {}

employees$ = this.employeeService.getEmployees();
loading = this.employeeService.loading;
```

### Create Employee Draft
```typescript
this.draftService.saveDraft({
  formDataJson: JSON.stringify(formData),
  draftName: 'Employee Draft - John Doe',
  completionPercentage: 75
}).subscribe(response => {
  console.log('Draft saved:', response.data);
});
```

### Cascading Address Lookup
```typescript
// Get all districts
this.addressService.getDistricts().subscribe(districts => {});

// When district selected
this.addressService.getVillagesByDistrict(districtId).subscribe(villages => {});

// Search postal codes
this.addressService.searchPostalCodes('70000').subscribe(codes => {});
```

### Calculate Payroll from Timesheet
```typescript
this.payrollService.calculatePayrollFromTimesheets({
  employeeId: 'emp-123',
  periodStart: '2024-11-01',
  periodEnd: '2024-11-30'
}).subscribe(result => {
  console.log('Gross Salary:', result.totalGrossSalary);
  console.log('Deductions:', result.statutoryDeductions);
  console.log('Net Salary:', result.netSalary);
});
```

### Approve Multiple Timesheets
```typescript
this.timesheetService.bulkApproveTimesheets({
  timesheetIds: ['ts-1', 'ts-2', 'ts-3']
}).subscribe(() => {
  console.log('Approved successfully');
});
```

### Manage Biometric Device
```typescript
// Test connection
this.deviceService.testConnection({
  ipAddress: '192.168.1.100',
  port: 4370,
  connectionMethod: 'USB'
}).subscribe(result => {
  console.log('Connection status:', result.success);
});

// Manual sync
this.deviceService.syncDevice(deviceId).subscribe(result => {
  console.log('Synced', result.recordCount, 'records');
});
```

---

## Integration Points

1. **Employee Module** → Depends on Department, Location, Address services
2. **Payroll Module** → Depends on Timesheet, Employee services
3. **Timesheet Module** → Depends on Employee, Payroll services
4. **Dashboard** → Consumes Employee, Payroll, Timesheet stats
5. **Auth** → Protects all modules with role-based signals
6. **Biometric Devices** → Feeds attendance data for timesheets

---

## Summary Statistics

- **Services**: 13 (8 core + 5 feature)
- **API Endpoints**: 100+
- **Signals**: 40+ across services
- **Models/DTOs**: 40+
- **Enums**: 10+
- **HTTP Methods**: GET, POST, PUT, DELETE, PATCH

---

## File Size Reference

- `/core/services/employee.service.ts` - 120 lines
- `/core/services/employee-draft.service.ts` - 130 lines
- `/core/services/payroll.service.ts` - 115 lines
- `/core/services/timesheet.service.ts` - 337 lines
- `/core/services/dashboard.service.ts` - 247 lines
- `/core/models/payroll.model.ts` - 185 lines
- `/core/models/timesheet.model.ts` - 216 lines
- `/features/tenant/organization/devices/biometric-device.service.ts` - 387 lines

Total: ~2,000 lines of service code + models

