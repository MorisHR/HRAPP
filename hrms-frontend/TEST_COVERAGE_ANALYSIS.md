# TEST COVERAGE ANALYSIS - HRMS Frontend
## Comprehensive Gap Analysis and Coverage Report

**Date:** November 17, 2025
**Analysis Type:** Line-by-line coverage gap identification
**Methodology:** Manual code analysis + test file inspection

---

## TABLE OF CONTENTS
1. [Coverage Summary](#coverage-summary)
2. [Component Coverage Deep Dive](#component-coverage-deep-dive)
3. [Service Coverage Analysis](#service-coverage-analysis)
4. [Critical Path Analysis](#critical-path-analysis)
5. [Edge Cases Missing](#edge-cases-missing)
6. [Test Quality Heatmap](#test-quality-heatmap)

---

## COVERAGE SUMMARY

### Overall Statistics

```
Total Files: 150+ TypeScript files
Tested Files: 6 files
Test Coverage: ~4%

Components: 31 UI components
Tested: 5 components (16%)
Untested: 26 components (84%)

Services: 45+ services
Tested: 0 services (0%)
Untested: 45+ services (100%)

Guards: 4 guards
Tested: 0 guards (0%)

Interceptors: 1 interceptor
Tested: 0 interceptors (0%)

Feature Components: 65+ components
Tested: 0 components (0%)

Test Cases Written: 229 tests
Test Files: 6 files
Lines of Test Code: ~1,500 lines
```

### Coverage by Module

| Module | Files | Tested | Coverage | Priority |
|--------|-------|--------|----------|----------|
| UI Components | 31 | 5 | 16% | HIGH |
| Core Services | 45 | 0 | 0% | CRITICAL |
| Auth Components | 8 | 0 | 0% | CRITICAL |
| Guards | 4 | 0 | 0% | CRITICAL |
| Interceptors | 1 | 0 | 0% | CRITICAL |
| Feature Components | 65 | 0 | 0% | HIGH |
| Pipes | 0 | 0 | N/A | MEDIUM |
| Directives | 1 | 0 | 0% | MEDIUM |
| Models | 20+ | 0 | 0% | LOW |

---

## COMPONENT COVERAGE DEEP DIVE

### ✅ FULLY TESTED COMPONENTS (100% Coverage)

#### 1. Datepicker Component
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/datepicker/datepicker.ts`
**Test File:** `datepicker.spec.ts` (448 lines, 65+ tests)

**Coverage:**
- ✅ Component initialization
- ✅ Date formatting (MM/DD/YYYY)
- ✅ Calendar toggle behavior
- ✅ Month/year navigation
- ✅ Date selection
- ✅ Min/max date validation
- ✅ Disabled state
- ✅ Keyboard navigation (arrow keys, enter, escape)
- ✅ Calendar positioning
- ✅ Overlay click-to-close
- ✅ Today button
- ✅ Clear button
- ✅ Value change events
- ✅ Input integration
- ✅ Accessibility (ARIA labels, roles, live regions)
- ✅ Edge cases (leap years, month boundaries, DST, invalid dates)
- ✅ Computed signals (days in month, month grid)
- ✅ Date utilities (isSameDay, isBetween, isToday)

**Test Quality:** A+ (Excellent)

#### 2. Pagination Component
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/pagination/pagination.ts`
**Test File:** `pagination.spec.ts` (442 lines, 62+ tests)

**Coverage:**
- ✅ Initialization with default values
- ✅ Total pages calculation
- ✅ Item range display (start/end)
- ✅ First/previous/next/last navigation
- ✅ Page size change
- ✅ Page size options
- ✅ Position maintenance on size change
- ✅ Boundary prevention (no negative pages, no exceeding max)
- ✅ Event emissions (pageChange)
- ✅ UI rendering (buttons, select, indicators)
- ✅ Button disabled states
- ✅ Accessibility (ARIA labels, roles, live regions)
- ✅ Edge cases (empty items, single item, large numbers, partial pages)
- ✅ Invalid input handling
- ✅ Two-way binding

**Test Quality:** A+ (Excellent)

#### 3. ExpansionPanel Component
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/expansion-panel/expansion-panel.ts`
**Test File:** `expansion-panel.spec.ts` (35+ tests)

**Coverage:**
- ✅ Panel rendering
- ✅ Collapsed/expanded states
- ✅ Toggle behavior
- ✅ Disabled state
- ✅ Header click handling
- ✅ Content visibility
- ✅ CSS class application
- ✅ Animations (with BrowserAnimationsModule)
- ✅ Icon rotation
- ✅ Accessibility (ARIA expanded, controls)
- ✅ ExpansionPanelGroup (accordion mode)
- ✅ Single expansion mode
- ✅ Multi-expansion mode

**Test Quality:** A (Very Good)

#### 4. List Component
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/list/list.ts`
**Test File:** `list.spec.ts` (38+ tests)

**Coverage:**
- ✅ List rendering
- ✅ Dense mode
- ✅ Bordered mode
- ✅ Elevated mode
- ✅ Combined modes
- ✅ Role attribute
- ✅ Input reactivity
- ✅ ListItem rendering
- ✅ ListItem disabled state
- ✅ ListItem click events
- ✅ ListItem accessibility

**Test Quality:** A (Very Good)

#### 5. Divider Component
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/divider/divider.ts`
**Test File:** `divider.spec.ts` (153 lines, 26+ tests)

**Coverage:**
- ✅ HR element rendering
- ✅ Horizontal/vertical orientation
- ✅ Inset mode
- ✅ Dense mode
- ✅ Combined modes
- ✅ Role attribute (separator)
- ✅ ARIA orientation
- ✅ Host element classes
- ✅ Input reactivity
- ✅ Computed classes

**Test Quality:** A (Very Good)

---

### ❌ UNTESTED COMPONENTS (0% Coverage)

#### CRITICAL Priority (Must Test)

##### 1. Input Component ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/input/input.ts`
**Usage:** 8+ authentication forms, employee forms, search fields
**Risk:** High - core form component used throughout app

**Missing Coverage:**
- ❌ Value binding
- ❌ Placeholder display
- ❌ Label floating behavior
- ❌ Error state display
- ❌ Disabled state
- ❌ Required validation
- ❌ Input types (text, email, password, number)
- ❌ Focus/blur events
- ❌ Error message display
- ❌ Hint text
- ❌ Prefix/suffix slots
- ❌ Character counter
- ❌ Accessibility (labels, describedby)

**Estimated Test Cases:** 40-50 tests

##### 2. Button Component ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/button/button.ts`
**Usage:** 100+ instances across application
**Risk:** High - primary interaction component

**Missing Coverage:**
- ❌ Button variants (primary, secondary, outline, text)
- ❌ Button sizes (small, medium, large)
- ❌ Disabled state
- ❌ Loading state
- ❌ Icon buttons
- ❌ Click events
- ❌ Ripple effect
- ❌ Focus states
- ❌ Keyboard activation (Enter, Space)
- ❌ Accessibility (role, aria-label, aria-disabled)

**Estimated Test Cases:** 35-40 tests

##### 3. Dialog Component ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/dialog/dialog.ts`
**Usage:** Confirmation dialogs, forms, alerts
**Risk:** High - critical user interaction flows

**Missing Coverage:**
- ❌ Dialog open/close
- ❌ Backdrop click
- ❌ Escape key close
- ❌ Focus trapping
- ❌ Initial focus
- ❌ Return focus on close
- ❌ Data passing
- ❌ Dialog result
- ❌ Animation
- ❌ Multiple dialogs
- ❌ Accessibility (role dialog, aria-modal, aria-labelledby)

**Estimated Test Cases:** 45-50 tests

##### 4. Table Component ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/table/table.ts`
**Usage:** Employee lists, reports, data grids
**Risk:** High - primary data display component

**Missing Coverage:**
- ❌ Column rendering
- ❌ Data binding
- ❌ Sorting
- ❌ Filtering
- ❌ Row selection
- ❌ Pagination integration
- ❌ Empty state
- ❌ Loading state
- ❌ Custom cell templates
- ❌ Sticky headers
- ❌ Responsive layout
- ❌ Accessibility (roles, headers, captions)

**Estimated Test Cases:** 60-70 tests

#### HIGH Priority

##### 5. Select Component
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/select/select.ts`
**Usage:** Dropdowns throughout forms
**Missing Coverage:**
- ❌ Option rendering
- ❌ Selection
- ❌ Multi-select
- ❌ Search/filter
- ❌ Keyboard navigation
- ❌ Option groups
- ❌ Disabled options
- ❌ Value changes
- ❌ Accessibility

**Estimated Test Cases:** 45-50 tests

##### 6. Checkbox Component
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/checkbox/checkbox.ts`
**Usage:** Forms, filters, multi-select
**Missing Coverage:**
- ❌ Checked/unchecked states
- ❌ Indeterminate state
- ❌ Disabled state
- ❌ Change events
- ❌ Label association
- ❌ Accessibility

**Estimated Test Cases:** 25-30 tests

##### 7. Radio/RadioGroup Components
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/radio/radio.ts`
**Usage:** Single-choice selections
**Missing Coverage:**
- ❌ Radio selection
- ❌ Group management
- ❌ Keyboard navigation (arrow keys)
- ❌ Disabled state
- ❌ Value changes
- ❌ Accessibility

**Estimated Test Cases:** 35-40 tests

##### 8. Tabs Component
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/tabs/tabs.ts`
**Usage:** Dashboard sections, settings
**Missing Coverage:**
- ❌ Tab rendering
- ❌ Tab selection
- ❌ Active tab state
- ❌ Keyboard navigation
- ❌ Tab content switching
- ❌ Lazy loading
- ❌ Accessibility (role tablist/tab/tabpanel)

**Estimated Test Cases:** 40-45 tests

##### 9. Sidenav Component
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/sidenav/sidenav.ts`
**Usage:** Main navigation
**Missing Coverage:**
- ❌ Open/close
- ❌ Modes (over, push, side)
- ❌ Backdrop
- ❌ Focus trapping
- ❌ Responsive behavior
- ❌ Accessibility

**Estimated Test Cases:** 35-40 tests

##### 10. Icon Component
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/icon/icon.ts`
**Usage:** Throughout UI
**Missing Coverage:**
- ❌ Icon rendering
- ❌ Size variants
- ❌ Color variants
- ❌ SVG loading
- ❌ Accessibility (aria-hidden, aria-label)

**Estimated Test Cases:** 20-25 tests

#### MEDIUM Priority

11. ❌ Autocomplete Component
12. ❌ Badge Component
13. ❌ Card Component (+ CardBody, CardTitle, CardActions)
14. ❌ Chip Component
15. ❌ Menu Component
16. ❌ Paginator Component (duplicate of Pagination?)
17. ❌ ProgressBar Component
18. ❌ ProgressSpinner Component
19. ❌ Stepper Component
20. ❌ Toast Component
21. ❌ Toggle Component
22. ❌ Toolbar Component
23. ❌ DialogContainer Component

**Total Estimated Test Cases for Untested Components:** 500-600 tests

---

## SERVICE COVERAGE ANALYSIS

### ❌ CRITICAL Services (0% Coverage)

#### 1. AuthService ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/services/auth.service.ts`
**Lines:** 80+ lines analyzed
**Complexity:** HIGH

**Missing Coverage:**
- ❌ `login(request: LoginRequest)` - authentication flow
- ❌ `superAdminLogin(request: LoginRequest)` - superadmin auth
- ❌ `logout()` - session cleanup
- ❌ `clearAuthStateSilently()` - silent token clear
- ❌ `decodeJwt(token: string)` - JWT parsing
- ❌ `loadAuthState()` - localStorage read
- ❌ `saveAuthState()` - localStorage write
- ❌ Token signal management
- ❌ User signal management
- ❌ isAuthenticated computed signal
- ❌ Role-based computed signals (isSuperAdmin, isTenantAdmin, etc.)
- ❌ Session timeout subscription
- ❌ Error handling

**Critical Scenarios:**
- ❌ Successful login with valid credentials
- ❌ Failed login with invalid credentials
- ❌ Token expiration handling
- ❌ Simultaneous login prevention
- ❌ Role-based access checks
- ❌ LocalStorage corruption handling
- ❌ Network error handling
- ❌ Logout cleanup verification

**Estimated Test Cases:** 40-50 tests

#### 2. TenantService ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/services/tenant.service.ts`
**Risk:** Multi-tenancy isolation failures

**Missing Coverage:**
- ❌ Tenant creation
- ❌ Tenant activation
- ❌ Tenant deactivation
- ❌ Tenant context switching
- ❌ Subdomain validation
- ❌ Tenant data isolation
- ❌ Cross-tenant data leak prevention

**Estimated Test Cases:** 35-40 tests

#### 3. EmployeeService ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/services/employee.service.ts`
**Risk:** Business logic errors

**Missing Coverage:**
- ❌ CRUD operations
- ❌ Search/filtering
- ❌ Pagination
- ❌ Bulk operations
- ❌ Data validation
- ❌ Error handling

**Estimated Test Cases:** 45-50 tests

#### 4. PayrollService ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/services/payroll.service.ts`
**Risk:** HIGH - financial calculations

**Missing Coverage:**
- ❌ Salary calculations
- ❌ Deduction calculations
- ❌ Tax calculations
- ❌ Payslip generation
- ❌ Rounding logic
- ❌ Currency handling

**Estimated Test Cases:** 50-60 tests

#### 5. SessionManagementService ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/services/session-management.service.ts`
**Risk:** Session hijacking, timeout failures

**Missing Coverage:**
- ❌ Session timeout detection
- ❌ Token refresh
- ❌ Activity tracking
- ❌ Logout event emission
- ❌ Warning dialog trigger
- ❌ Idle timeout

**Estimated Test Cases:** 30-35 tests

#### 6. SubdomainService ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/services/subdomain.service.ts`
**Risk:** Tenant routing failures

**Missing Coverage:**
- ❌ Subdomain extraction from URL
- ❌ Tenant lookup by subdomain
- ❌ Subdomain validation
- ❌ Redirect logic
- ❌ Local development handling

**Estimated Test Cases:** 25-30 tests

### HIGH Priority Services (0% Coverage)

7. ❌ AttendanceService - Time tracking logic
8. ❌ LeaveService - Leave calculations
9. ❌ BillingService - Payment processing
10. ❌ DashboardService - Dashboard data aggregation
11. ❌ TimesheetService - Timesheet validation
12. ❌ SalaryComponentsService - Salary structure
13. ❌ ReportsService - Report generation
14. ❌ AuditLogService - Audit logging
15. ❌ SecurityAlertService - Security monitoring
16. ❌ NotificationService - Notification delivery
17. ❌ ErrorHandlerService - Error handling
18. ❌ TenantContextService - Tenant context management

### MEDIUM Priority Services (0% Coverage)

19. ❌ ThemeService
20. ❌ PricingTierService
21. ❌ EmployeeDraftService
22. ❌ LocationService
23. ❌ DepartmentService
24. ❌ DeviceStatusService
25. ❌ SectorsService
26. ❌ SetupService
27. ❌ EmailTestService
28-45. [Additional 18 services]

**Total Estimated Service Test Cases:** 800-1000 tests

---

## GUARD & INTERCEPTOR COVERAGE

### ❌ Guards (0% Coverage) - CRITICAL

#### 1. auth.guard.ts ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/guards/auth.guard.ts`
**Purpose:** Protect authenticated routes

**Missing Coverage:**
- ❌ Authenticated user can access protected routes
- ❌ Unauthenticated user redirected to login
- ❌ Expired token handling
- ❌ Missing token handling
- ❌ Invalid token handling
- ❌ Return URL preservation

**Estimated Test Cases:** 12-15 tests

#### 2. role.guard.ts ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/guards/role.guard.ts`
**Purpose:** Role-based access control

**Missing Coverage:**
- ❌ Correct role can access route
- ❌ Incorrect role denied access
- ❌ Multiple roles handling
- ❌ No role handling
- ❌ Role hierarchy
- ❌ Unauthorized redirect

**Estimated Test Cases:** 15-18 tests

#### 3. subdomain.guard.ts ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/guards/subdomain.guard.ts`
**Purpose:** Tenant subdomain validation

**Missing Coverage:**
- ❌ Valid subdomain allows access
- ❌ Invalid subdomain redirects
- ❌ Missing subdomain handling
- ❌ Tenant context set correctly
- ❌ Cross-tenant access prevention

**Estimated Test Cases:** 12-15 tests

#### 4. already-logged-in.guard.ts
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/guards/already-logged-in.guard.ts`
**Purpose:** Prevent authenticated users from accessing login

**Missing Coverage:**
- ❌ Authenticated user redirected to dashboard
- ❌ Unauthenticated user can access login
- ❌ Token expiration check

**Estimated Test Cases:** 8-10 tests

### ❌ Interceptors (0% Coverage) - CRITICAL

#### 1. auth.interceptor.ts ⚠️ CRITICAL
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/interceptors/auth.interceptor.ts`
**Purpose:** Add auth headers, handle 401s

**Missing Coverage:**
- ❌ Auth header added to requests
- ❌ Token attached correctly
- ❌ 401 response handling
- ❌ Token refresh flow
- ❌ Logout on auth failure
- ❌ Public endpoint bypass
- ❌ Error response handling

**Estimated Test Cases:** 20-25 tests

**Total Guard/Interceptor Test Cases:** 70-90 tests

---

## CRITICAL PATH ANALYSIS

### User Authentication Flow
**Status:** ❌ 0% Tested

**Critical Path Steps:**
1. User enters subdomain → SubdomainComponent
2. Subdomain validated → subdomain.guard.ts
3. User enters credentials → TenantLoginComponent
4. Credentials sent → AuthService.login()
5. Token received → AuthService.saveAuthState()
6. Auth interceptor adds token → auth.interceptor.ts
7. User redirected → auth.guard.ts
8. Role checked → role.guard.ts

**Coverage:**
- ❌ SubdomainComponent - 0%
- ❌ subdomain.guard.ts - 0%
- ❌ TenantLoginComponent - 0%
- ❌ AuthService - 0%
- ❌ auth.interceptor.ts - 0%
- ❌ auth.guard.ts - 0%
- ❌ role.guard.ts - 0%

**Path Test Coverage: 0%** ⚠️ CRITICAL

### Employee Management Flow
**Status:** ❌ 0% Tested

**Critical Path Steps:**
1. List employees → EmployeeListComponent
2. Fetch data → EmployeeService.getEmployees()
3. Display table → TableComponent
4. Edit employee → EmployeeFormComponent
5. Validate → Input components
6. Save → EmployeeService.update()

**Coverage:**
- ❌ EmployeeListComponent - 0%
- ❌ EmployeeService - 0%
- ❌ TableComponent - 0%
- ❌ EmployeeFormComponent - 0%
- ❌ Input components - 0%

**Path Test Coverage: 0%** ⚠️ CRITICAL

### Payroll Processing Flow
**Status:** ❌ 0% Tested

**Critical Path Steps:**
1. View payroll → PayrollDashboardComponent
2. Calculate salaries → PayrollService.calculatePayroll()
3. Generate payslips → PayrollService.generatePayslips()
4. Display → PayslipListComponent

**Coverage:**
- ❌ PayrollDashboardComponent - 0%
- ❌ PayrollService - 0%
- ❌ PayslipListComponent - 0%

**Path Test Coverage: 0%** ⚠️ CRITICAL

### Attendance Tracking Flow
**Status:** ❌ 0% Tested

**Critical Path Steps:**
1. Device punch → DeviceWebhook (backend)
2. Real-time update → AttendanceRealtimeService
3. Display dashboard → AttendanceDashboardComponent
4. Calculate hours → AttendanceService

**Coverage:**
- ❌ AttendanceRealtimeService - 0%
- ❌ AttendanceDashboardComponent - 0%
- ❌ AttendanceService - 0%

**Path Test Coverage: 0%** ⚠️ CRITICAL

---

## EDGE CASES MISSING

### Input Validation Edge Cases

#### AuthService
- ❌ Empty email/password
- ❌ Malformed email
- ❌ SQL injection attempts
- ❌ XSS in credentials
- ❌ Very long passwords (>1000 chars)
- ❌ Unicode/emoji in passwords
- ❌ Null/undefined inputs

#### PayrollService
- ❌ Negative salary values
- ❌ Zero salary
- ❌ Extremely large salaries
- ❌ Floating point precision issues
- ❌ Currency overflow
- ❌ Division by zero
- ❌ Null employee records

#### DatePicker (Tested ✅)
- ✅ Leap years
- ✅ DST transitions
- ✅ Month boundaries
- ✅ Invalid date strings
- ✅ Future date restrictions
- ✅ Past date restrictions

#### Pagination (Tested ✅)
- ✅ Zero items
- ✅ Single item
- ✅ Negative page numbers
- ✅ Page exceeding total
- ✅ Very large datasets
- ✅ Invalid page size

### Network/API Edge Cases
- ❌ Network timeout
- ❌ Slow connection (3G)
- ❌ Connection drop mid-request
- ❌ 500 server errors
- ❌ 502/503 errors
- ❌ Invalid JSON responses
- ❌ Large response payloads
- ❌ Concurrent API calls
- ❌ Race conditions

### Browser/Environment Edge Cases
- ❌ LocalStorage full
- ❌ LocalStorage disabled
- ❌ Cookies disabled
- ❌ Private browsing mode
- ❌ Multiple tabs/windows
- ❌ Browser back/forward
- ❌ Page refresh during operation
- ❌ Session timeout during operation

---

## TEST QUALITY HEATMAP

### Coverage Heatmap by Module

```
                          Coverage    Quality    Priority
┌──────────────────────┬───────────┬──────────┬──────────┐
│ UI Components        │           │          │          │
│   Datepicker         │ ████████  │ A+       │ Done ✅  │
│   Pagination         │ ████████  │ A+       │ Done ✅  │
│   ExpansionPanel     │ ████████  │ A        │ Done ✅  │
│   List               │ ████████  │ A        │ Done ✅  │
│   Divider            │ ████████  │ A        │ Done ✅  │
│   Input              │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   Button             │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   Dialog             │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   Table              │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   Other (22)         │ ▁▁▁▁▁▁▁▁  │ N/A      │ HIGH     │
│                      │           │          │          │
│ Core Services        │           │          │          │
│   AuthService        │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   TenantService      │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   EmployeeService    │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   PayrollService     │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   SessionMgmt        │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   Other (40)         │ ▁▁▁▁▁▁▁▁  │ N/A      │ HIGH     │
│                      │           │          │          │
│ Guards               │           │          │          │
│   auth.guard         │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   role.guard         │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   subdomain.guard    │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   Other (1)          │ ▁▁▁▁▁▁▁▁  │ N/A      │ HIGH     │
│                      │           │          │          │
│ Interceptors         │           │          │          │
│   auth.interceptor   │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│                      │           │          │          │
│ Feature Components   │           │          │          │
│   Auth (8)           │ ▁▁▁▁▁▁▁▁  │ N/A      │ CRITICAL │
│   Admin (20+)        │ ▁▁▁▁▁▁▁▁  │ N/A      │ HIGH     │
│   Tenant (25+)       │ ▁▁▁▁▁▁▁▁  │ N/A      │ HIGH     │
│   Employee (10+)     │ ▁▁▁▁▁▁▁▁  │ N/A      │ HIGH     │
└──────────────────────┴───────────┴──────────┴──────────┘

Legend: ████████ = 100% | ████▁▁▁▁ = 50% | ▁▁▁▁▁▁▁▁ = 0%
```

---

## RECOMMENDATIONS BY PRIORITY

### Sprint 1 (Week 1-2) - CRITICAL Coverage

**Goal:** Cover authentication and authorization critical paths

1. **AuthService Tests** (40-50 tests)
   - Login flow
   - Logout flow
   - Token management
   - Role checks

2. **Guard Tests** (50 tests total)
   - auth.guard.ts (15 tests)
   - role.guard.ts (18 tests)
   - subdomain.guard.ts (15 tests)

3. **auth.interceptor Tests** (20-25 tests)
   - Header injection
   - 401 handling
   - Token refresh

4. **Input Component Tests** (40-50 tests)
   - Used in all forms

**Total Sprint 1 Tests:** ~160-190 tests

### Sprint 2 (Week 3-4) - Core Services

**Goal:** Cover business logic services

1. **TenantService** (35-40 tests)
2. **EmployeeService** (45-50 tests)
3. **SessionManagementService** (30-35 tests)
4. **SubdomainService** (25-30 tests)
5. **PayrollService** (50-60 tests)

**Total Sprint 2 Tests:** ~185-215 tests

### Sprint 3 (Week 5-6) - UI Components

**Goal:** Cover critical UI components

1. **Button Component** (35-40 tests)
2. **Dialog Component** (45-50 tests)
3. **Table Component** (60-70 tests)
4. **Select Component** (45-50 tests)
5. **Checkbox Component** (25-30 tests)

**Total Sprint 3 Tests:** ~210-240 tests

### Sprint 4 (Week 7-8) - Integration & E2E

**Goal:** Cover user workflows end-to-end

1. **Auth Flow E2E** (10 scenarios)
2. **Employee Management E2E** (15 scenarios)
3. **Payroll E2E** (12 scenarios)
4. **Attendance E2E** (10 scenarios)
5. **Integration Tests** (30-40 tests)

**Total Sprint 4 Tests:** ~50-70 E2E + 30-40 integration

---

## APPENDIX: Untested Files List

### Components (31 total, 26 untested)

```
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/autocomplete/autocomplete.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/badge/badge.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/button/button.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/card/card.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/checkbox/checkbox.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/chip/chip.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/dialog/dialog.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/dialog-container/dialog-container.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/icon/icon.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/input/input.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/menu/menu.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/paginator/paginator.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/progress-bar/progress-bar.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/progress-spinner/progress-spinner.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/radio/radio.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/radio-group/radio-group.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/select/select.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/sidenav/sidenav.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/stepper/stepper.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/table/table.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/tabs/tabs.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/toast-container/toast-container.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/toggle/toggle.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/toolbar/toolbar.ts
```

### Services (45 untested)

```
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/services/auth.service.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/services/tenant.service.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/services/employee.service.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/services/payroll.service.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/services/session-management.service.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/services/subdomain.service.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/services/attendance.service.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/services/leave.service.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/services/billing.service.ts
[... 36 more services ...]
```

### Guards (4 untested)

```
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/guards/auth.guard.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/guards/role.guard.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/guards/subdomain.guard.ts
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/guards/already-logged-in.guard.ts
```

### Interceptors (1 untested)

```
❌ /workspaces/HRAPP/hrms-frontend/src/app/core/interceptors/auth.interceptor.ts
```

---

**Total Estimated Test Effort:** 1,500-2,000 test cases | 8,000-12,000 lines of test code | 6-8 weeks with 3 QA engineers

**End of Coverage Analysis**
