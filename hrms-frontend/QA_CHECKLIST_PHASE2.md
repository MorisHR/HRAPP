# QA CHECKLIST - PHASE 2 MIGRATION
## Comprehensive Testing Roadmap for HRMS Frontend

**Project:** HRMS Frontend Material → Custom UI Migration
**Phase:** Phase 2 (Remaining 26 Components + Services)
**Timeline:** 8 weeks (4 sprints)
**Target Coverage:** 80% overall, 100% critical paths

---

## TABLE OF CONTENTS
1. [Sprint Planning Overview](#sprint-planning-overview)
2. [Unit Testing Checklist](#unit-testing-checklist)
3. [Integration Testing Checklist](#integration-testing-checklist)
4. [E2E Testing Checklist](#e2e-testing-checklist)
5. [Security Testing Checklist](#security-testing-checklist)
6. [Accessibility Testing Checklist](#accessibility-testing-checklist)
7. [Performance Testing Checklist](#performance-testing-checklist)
8. [Browser Compatibility Checklist](#browser-compatibility-checklist)

---

## SPRINT PLANNING OVERVIEW

### Sprint 1: Critical Infrastructure (Weeks 1-2)
**Focus:** Authentication, authorization, core services
**Test Count:** ~160-190 tests
**Story Points:** 21 points

### Sprint 2: Core Business Logic (Weeks 3-4)
**Focus:** Services, business logic, calculations
**Test Count:** ~185-215 tests
**Story Points:** 21 points

### Sprint 3: UI Components (Weeks 5-6)
**Focus:** Remaining custom components
**Test Count:** ~210-240 tests
**Story Points:** 21 points

### Sprint 4: Integration & E2E (Weeks 7-8)
**Focus:** User workflows, performance, security
**Test Count:** ~80-110 tests
**Story Points:** 21 points

**Total Estimated Tests:** 635-755 new tests

---

## UNIT TESTING CHECKLIST

### Sprint 1: Critical Infrastructure Tests

#### ✅ AuthService Tests (Priority: P0 - CRITICAL)
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/services/auth.service.ts`
**Test File:** `auth.service.spec.ts`
**Estimated:** 45 tests | 3 days

- [ ] **Initialization**
  - [ ] Should create service
  - [ ] Should load auth state from localStorage on init
  - [ ] Should initialize with null user when no stored state
  - [ ] Should subscribe to session timeout events

- [ ] **Login Functionality**
  - [ ] Should successfully login with valid credentials
  - [ ] Should set user signal on successful login
  - [ ] Should set token signal on successful login
  - [ ] Should save auth state to localStorage
  - [ ] Should decode JWT and extract user info
  - [ ] Should fail login with invalid credentials
  - [ ] Should handle network errors during login
  - [ ] Should handle 401 unauthorized response
  - [ ] Should handle 500 server error
  - [ ] Should set loading state during login
  - [ ] Should clear loading state after login completes
  - [ ] Should emit error message on failed login

- [ ] **SuperAdmin Login**
  - [ ] Should login superadmin with valid credentials
  - [ ] Should set isSuperAdmin computed signal
  - [ ] Should use correct API endpoint
  - [ ] Should handle superadmin-specific errors

- [ ] **Logout Functionality**
  - [ ] Should clear user signal on logout
  - [ ] Should clear token signal on logout
  - [ ] Should remove localStorage items on logout
  - [ ] Should navigate to login page on logout
  - [ ] Should handle logout errors gracefully

- [ ] **Silent Token Clear**
  - [ ] Should clear auth state without navigation
  - [ ] Should not navigate when clearing silently
  - [ ] Should emit appropriate events

- [ ] **JWT Decoding**
  - [ ] Should decode valid JWT token
  - [ ] Should extract user claims correctly
  - [ ] Should handle invalid JWT format
  - [ ] Should handle malformed base64
  - [ ] Should throw error for non-JWT tokens
  - [ ] Should decode tokens with special characters

- [ ] **Auth State Management**
  - [ ] Should save auth state to localStorage
  - [ ] Should load auth state from localStorage
  - [ ] Should handle corrupted localStorage data
  - [ ] Should handle missing localStorage items
  - [ ] Should handle localStorage quota exceeded

- [ ] **Computed Signals**
  - [ ] isAuthenticated should return true when logged in
  - [ ] isAuthenticated should return false when logged out
  - [ ] isSuperAdmin should return true for superadmin role
  - [ ] isTenantAdmin should return true for tenant admin role
  - [ ] isHR should return true for HR role
  - [ ] isManager should return true for manager role
  - [ ] isEmployee should return true for employee role
  - [ ] All role checks should return false when logged out

- [ ] **Session Timeout Integration**
  - [ ] Should logout when session timeout event received
  - [ ] Should subscribe to logout events on init

- [ ] **Edge Cases**
  - [ ] Should handle null token
  - [ ] Should handle undefined user
  - [ ] Should handle empty token string
  - [ ] Should handle very long tokens

**Acceptance Criteria:**
- ✅ All tests passing
- ✅ 90%+ code coverage
- ✅ All edge cases covered
- ✅ Mock HttpClient properly

---

#### ✅ Guard Tests (Priority: P0 - CRITICAL)

##### auth.guard.ts Tests
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/guards/auth.guard.ts`
**Test File:** `auth.guard.spec.ts`
**Estimated:** 15 tests | 1 day

- [ ] **Access Control**
  - [ ] Should allow access when user is authenticated
  - [ ] Should deny access when user is not authenticated
  - [ ] Should redirect to login when not authenticated
  - [ ] Should preserve return URL in query params
  - [ ] Should handle expired tokens
  - [ ] Should handle invalid tokens
  - [ ] Should handle missing tokens

- [ ] **Navigation**
  - [ ] Should return UrlTree for redirect
  - [ ] Should navigate to correct login page
  - [ ] Should handle subdomain in redirect

- [ ] **Edge Cases**
  - [ ] Should handle null auth service
  - [ ] Should handle undefined router
  - [ ] Should work with child routes
  - [ ] Should work with lazy-loaded modules

**Acceptance Criteria:**
- ✅ 100% code coverage
- ✅ All navigation scenarios tested
- ✅ Mock AuthService properly

---

##### role.guard.ts Tests
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/guards/role.guard.ts`
**Test File:** `role.guard.spec.ts`
**Estimated:** 18 tests | 1 day

- [ ] **Role-Based Access**
  - [ ] Should allow access for correct role
  - [ ] Should deny access for incorrect role
  - [ ] Should allow access for multiple allowed roles
  - [ ] Should handle SuperAdmin role
  - [ ] Should handle TenantAdmin role
  - [ ] Should handle HR role
  - [ ] Should handle Manager role
  - [ ] Should handle Employee role

- [ ] **Redirect Behavior**
  - [ ] Should redirect to unauthorized page
  - [ ] Should redirect to appropriate dashboard per role
  - [ ] Should show error message on denial

- [ ] **Edge Cases**
  - [ ] Should handle no role specified
  - [ ] Should handle empty roles array
  - [ ] Should handle null user
  - [ ] Should handle undefined role
  - [ ] Should handle role case sensitivity
  - [ ] Should handle role hierarchy

**Acceptance Criteria:**
- ✅ 100% code coverage
- ✅ All roles tested
- ✅ Authorization logic verified

---

##### subdomain.guard.ts Tests
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/guards/subdomain.guard.ts`
**Test File:** `subdomain.guard.spec.ts`
**Estimated:** 15 tests | 1 day

- [ ] **Subdomain Validation**
  - [ ] Should allow access with valid subdomain
  - [ ] Should deny access with invalid subdomain
  - [ ] Should deny access with missing subdomain
  - [ ] Should set tenant context on valid subdomain
  - [ ] Should handle localhost development
  - [ ] Should handle custom ports

- [ ] **Tenant Context**
  - [ ] Should load tenant by subdomain
  - [ ] Should handle non-existent tenant
  - [ ] Should handle deactivated tenant
  - [ ] Should cache tenant data

- [ ] **Edge Cases**
  - [ ] Should handle very long subdomains
  - [ ] Should handle special characters in subdomain
  - [ ] Should handle multiple subdomains (a.b.example.com)
  - [ ] Should handle IP addresses

**Acceptance Criteria:**
- ✅ 100% code coverage
- ✅ Tenant isolation verified
- ✅ All subdomain formats tested

---

##### already-logged-in.guard.ts Tests
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/guards/already-logged-in.guard.ts`
**Test File:** `already-logged-in.guard.spec.ts`
**Estimated:** 10 tests | 0.5 days

- [ ] **Redirect Logic**
  - [ ] Should redirect authenticated user to dashboard
  - [ ] Should allow unauthenticated user to login page
  - [ ] Should check token expiration
  - [ ] Should redirect based on user role

- [ ] **Edge Cases**
  - [ ] Should handle expired token as unauthenticated
  - [ ] Should handle missing token
  - [ ] Should handle invalid token

**Acceptance Criteria:**
- ✅ 100% code coverage
- ✅ Redirect logic verified

---

#### ✅ Interceptor Tests (Priority: P0 - CRITICAL)

##### auth.interceptor.ts Tests
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/core/interceptors/auth.interceptor.ts`
**Test File:** `auth.interceptor.spec.ts`
**Estimated:** 25 tests | 1.5 days

- [ ] **Header Injection**
  - [ ] Should add Authorization header with Bearer token
  - [ ] Should not add header for public endpoints
  - [ ] Should use current token from AuthService
  - [ ] Should handle missing token gracefully

- [ ] **Response Handling**
  - [ ] Should pass through successful responses
  - [ ] Should handle 401 Unauthorized
  - [ ] Should logout on 401
  - [ ] Should redirect to login on 401
  - [ ] Should handle 403 Forbidden
  - [ ] Should handle 500 errors
  - [ ] Should handle network errors

- [ ] **Token Refresh**
  - [ ] Should refresh token on 401 (if implemented)
  - [ ] Should retry request after refresh
  - [ ] Should logout if refresh fails

- [ ] **Request Cloning**
  - [ ] Should clone request with headers
  - [ ] Should not modify original request
  - [ ] Should handle requests with existing headers

- [ ] **Edge Cases**
  - [ ] Should handle null responses
  - [ ] Should handle empty responses
  - [ ] Should handle timeout errors
  - [ ] Should handle CORS errors
  - [ ] Should handle concurrent requests
  - [ ] Should not add header twice

**Acceptance Criteria:**
- ✅ 100% code coverage
- ✅ All HTTP status codes tested
- ✅ Mock HttpHandler properly

---

#### ✅ Critical Component Tests (Priority: P0)

##### Input Component Tests
**File:** `/workspaces/HRAPP/hrms-frontend/src/app/shared/ui/components/input/input.ts`
**Test File:** `input.spec.ts`
**Estimated:** 45 tests | 2 days

- [ ] **Basic Rendering**
  - [ ] Should create component
  - [ ] Should render input element
  - [ ] Should render label when provided
  - [ ] Should apply correct input type

- [ ] **Value Binding**
  - [ ] Should bind value to input
  - [ ] Should update on input change
  - [ ] Should support two-way binding
  - [ ] Should emit valueChange event

- [ ] **Label Behavior**
  - [ ] Should float label when focused
  - [ ] Should float label when has value
  - [ ] Should not float when empty and not focused
  - [ ] Should animate label transition

- [ ] **Input Types**
  - [ ] Should support type="text"
  - [ ] Should support type="email"
  - [ ] Should support type="password"
  - [ ] Should support type="number"
  - [ ] Should support type="tel"
  - [ ] Should support type="url"

- [ ] **States**
  - [ ] Should show disabled state
  - [ ] Should show readonly state
  - [ ] Should show error state
  - [ ] Should show success state
  - [ ] Should show focused state

- [ ] **Error Handling**
  - [ ] Should display error message
  - [ ] Should show error styling
  - [ ] Should clear error on input
  - [ ] Should support multiple errors

- [ ] **Validation**
  - [ ] Should mark as required
  - [ ] Should validate on blur
  - [ ] Should validate on input (if configured)
  - [ ] Should show validation errors

- [ ] **Placeholder**
  - [ ] Should display placeholder
  - [ ] Should hide placeholder when has value
  - [ ] Should show placeholder when empty

- [ ] **Hints**
  - [ ] Should display hint text
  - [ ] Should align hint text
  - [ ] Should hide hint when error shown

- [ ] **Prefix/Suffix**
  - [ ] Should render prefix content
  - [ ] Should render suffix content
  - [ ] Should support icon prefix
  - [ ] Should support icon suffix

- [ ] **Character Counter**
  - [ ] Should show character count
  - [ ] Should update count on input
  - [ ] Should warn when approaching limit
  - [ ] Should error when exceeding limit

- [ ] **Accessibility**
  - [ ] Should have proper label association
  - [ ] Should have aria-describedby for errors
  - [ ] Should have aria-describedby for hints
  - [ ] Should have aria-required when required
  - [ ] Should have aria-invalid when error

- [ ] **Focus Management**
  - [ ] Should focus on programmatic focus()
  - [ ] Should emit focus event
  - [ ] Should emit blur event
  - [ ] Should remove focus styling on blur

**Acceptance Criteria:**
- ✅ 100% code coverage
- ✅ All input types tested
- ✅ All states tested
- ✅ WCAG 2.1 AA compliance

---

### Sprint 2: Core Service Tests

#### ✅ SessionManagementService Tests
**File:** `session-management.service.ts`
**Estimated:** 32 tests | 2 days

- [ ] Session timeout detection
- [ ] Activity tracking
- [ ] Warning dialog trigger
- [ ] Auto-logout on timeout
- [ ] Token refresh logic
- [ ] Idle state detection
- [ ] Multiple tab synchronization
- [ ] Event emission

---

#### ✅ TenantService Tests
**File:** `tenant.service.ts`
**Estimated:** 38 tests | 2 days

- [ ] CRUD operations
- [ ] Activation/deactivation
- [ ] Subdomain validation
- [ ] Tenant context management
- [ ] Data isolation verification
- [ ] Caching logic

---

#### ✅ EmployeeService Tests
**File:** `employee.service.ts`
**Estimated:** 48 tests | 2.5 days

- [ ] CRUD operations
- [ ] Search and filtering
- [ ] Pagination
- [ ] Bulk operations
- [ ] Data validation
- [ ] Error handling
- [ ] Caching

---

#### ✅ PayrollService Tests
**File:** `payroll.service.ts`
**Estimated:** 55 tests | 3 days

- [ ] Salary calculations
- [ ] Tax calculations
- [ ] Deduction handling
- [ ] Bonus calculations
- [ ] Overtime calculations
- [ ] Payslip generation
- [ ] Rounding logic
- [ ] Currency precision
- [ ] Edge cases (negative, zero, overflow)

---

#### ✅ SubdomainService Tests
**File:** `subdomain.service.ts`
**Estimated:** 28 tests | 1.5 days

- [ ] Subdomain extraction from URL
- [ ] Tenant lookup by subdomain
- [ ] Validation logic
- [ ] Local development handling
- [ ] Caching
- [ ] Error handling

---

### Sprint 3: UI Component Tests

#### ✅ Button Component Tests
**File:** `button.ts`
**Estimated:** 38 tests | 2 days

- [ ] **Variants**
  - [ ] Primary button rendering
  - [ ] Secondary button rendering
  - [ ] Outline button rendering
  - [ ] Text button rendering
  - [ ] Icon button rendering

- [ ] **Sizes**
  - [ ] Small size
  - [ ] Medium size (default)
  - [ ] Large size

- [ ] **States**
  - [ ] Default state
  - [ ] Hover state
  - [ ] Active state
  - [ ] Focused state
  - [ ] Disabled state
  - [ ] Loading state

- [ ] **Functionality**
  - [ ] Click event emission
  - [ ] Keyboard activation (Enter)
  - [ ] Keyboard activation (Space)
  - [ ] No click when disabled
  - [ ] No click when loading

- [ ] **Accessibility**
  - [ ] Role="button"
  - [ ] aria-label support
  - [ ] aria-disabled when disabled
  - [ ] aria-busy when loading
  - [ ] Keyboard focus visible

**Acceptance Criteria:**
- ✅ 100% coverage
- ✅ All variants tested
- ✅ WCAG 2.1 AA compliance

---

#### ✅ Dialog Component Tests
**File:** `dialog.ts`
**Estimated:** 48 tests | 2.5 days

- [ ] Open/close functionality
- [ ] Data passing
- [ ] Result handling
- [ ] Backdrop behavior
- [ ] Escape key handling
- [ ] Focus trapping
- [ ] Initial focus
- [ ] Return focus on close
- [ ] Stacking multiple dialogs
- [ ] Animation
- [ ] Accessibility (role, aria-modal, aria-labelledby)

---

#### ✅ Table Component Tests
**File:** `table.ts`
**Estimated:** 65 tests | 3 days

- [ ] Column definition
- [ ] Data binding
- [ ] Row rendering
- [ ] Sorting (ascending/descending)
- [ ] Filtering
- [ ] Selection (single/multiple)
- [ ] Pagination integration
- [ ] Empty state
- [ ] Loading state
- [ ] Sticky headers
- [ ] Custom cell templates
- [ ] Row actions
- [ ] Responsive layout
- [ ] Accessibility

---

#### ✅ Select Component Tests
**File:** `select.ts`
**Estimated:** 48 tests | 2.5 days

- [ ] Single selection
- [ ] Multi-selection
- [ ] Option rendering
- [ ] Search/filter
- [ ] Keyboard navigation
- [ ] Option groups
- [ ] Disabled options
- [ ] Placeholder
- [ ] Clear button
- [ ] Value changes
- [ ] Accessibility

---

#### ✅ Checkbox Component Tests
**File:** `checkbox.ts`
**Estimated:** 28 tests | 1.5 days

- [ ] Checked state
- [ ] Unchecked state
- [ ] Indeterminate state
- [ ] Disabled state
- [ ] Label association
- [ ] Change events
- [ ] Value binding
- [ ] Accessibility

---

#### ✅ Radio Component Tests
**File:** `radio.ts` & `radio-group.ts`
**Estimated:** 38 tests | 2 days

- [ ] Radio selection
- [ ] Radio group management
- [ ] Keyboard navigation (arrow keys)
- [ ] Disabled radios
- [ ] Value changes
- [ ] Label association
- [ ] Accessibility

---

#### ✅ Remaining Components (15 components)
**Estimated:** 180 tests | 9 days total

- [ ] Tabs (40 tests)
- [ ] Sidenav (38 tests)
- [ ] Icon (22 tests)
- [ ] Card (25 tests)
- [ ] Autocomplete (35 tests)
- [ ] Badge (15 tests)
- [ ] Chip (20 tests)
- [ ] Menu (32 tests)
- [ ] ProgressBar (18 tests)
- [ ] ProgressSpinner (18 tests)
- [ ] Stepper (35 tests)
- [ ] Toast (28 tests)
- [ ] Toggle (25 tests)
- [ ] Toolbar (22 tests)
- [ ] Paginator (22 tests - may be duplicate)

---

## INTEGRATION TESTING CHECKLIST

### Sprint 4: Integration Tests

#### ✅ Component-Service Integration

- [ ] **Auth Flow Integration** (8 tests)
  - [ ] Login form submission → AuthService
  - [ ] Login success → redirect
  - [ ] Login failure → error display
  - [ ] Logout button → AuthService.logout()

- [ ] **Employee Form Integration** (10 tests)
  - [ ] Form submission → EmployeeService.create()
  - [ ] Validation errors → form display
  - [ ] Success → redirect
  - [ ] Cancel → navigation

- [ ] **Table + Pagination Integration** (8 tests)
  - [ ] Table data → Pagination component
  - [ ] Page change → table update
  - [ ] Page size change → table update
  - [ ] Sorting + pagination

- [ ] **Dialog Integration** (6 tests)
  - [ ] Open dialog → data passed
  - [ ] Close dialog → result returned
  - [ ] Dialog → service call
  - [ ] Confirmation dialog workflow

**Total Integration Tests:** 30-40 tests

---

## E2E TESTING CHECKLIST

### Sprint 4: E2E Test Scenarios

#### ✅ Authentication Workflows (Priority: CRITICAL)

**Tool:** Cypress or Playwright

- [ ] **User Login Flow** (E2E-001)
  - [ ] Navigate to subdomain page
  - [ ] Enter subdomain
  - [ ] Click continue
  - [ ] Enter email and password
  - [ ] Click login button
  - [ ] Verify redirect to dashboard
  - [ ] Verify user name displayed
  - [ ] Verify token in localStorage

- [ ] **Failed Login Flow** (E2E-002)
  - [ ] Navigate to login
  - [ ] Enter invalid credentials
  - [ ] Click login
  - [ ] Verify error message displayed
  - [ ] Verify no redirect
  - [ ] Verify no token stored

- [ ] **Logout Flow** (E2E-003)
  - [ ] Login successfully
  - [ ] Click logout button
  - [ ] Verify redirect to login
  - [ ] Verify token cleared
  - [ ] Verify cannot access protected routes

- [ ] **Session Timeout Flow** (E2E-004)
  - [ ] Login successfully
  - [ ] Wait for session timeout
  - [ ] Verify timeout warning appears
  - [ ] Verify auto-logout after countdown
  - [ ] Verify redirect to login

- [ ] **Remember Me Flow** (E2E-005)
  - [ ] Login with "Remember Me" checked
  - [ ] Close browser
  - [ ] Reopen browser
  - [ ] Verify still logged in

- [ ] **Forgot Password Flow** (E2E-006)
  - [ ] Click "Forgot Password"
  - [ ] Enter email
  - [ ] Submit form
  - [ ] Verify success message
  - [ ] Check email received (mock)

- [ ] **Reset Password Flow** (E2E-007)
  - [ ] Click reset link (with token)
  - [ ] Enter new password
  - [ ] Confirm password
  - [ ] Submit form
  - [ ] Verify success
  - [ ] Login with new password

- [ ] **Subdomain Validation** (E2E-008)
  - [ ] Enter invalid subdomain
  - [ ] Verify error message
  - [ ] Cannot proceed to login

---

#### ✅ Employee Management Workflows (Priority: HIGH)

- [ ] **Create Employee** (E2E-010)
  - [ ] Navigate to employees list
  - [ ] Click "Add Employee"
  - [ ] Fill employee form
  - [ ] Upload photo
  - [ ] Submit form
  - [ ] Verify success message
  - [ ] Verify employee in list

- [ ] **Edit Employee** (E2E-011)
  - [ ] Find employee in list
  - [ ] Click edit button
  - [ ] Modify fields
  - [ ] Submit form
  - [ ] Verify changes saved

- [ ] **Delete Employee** (E2E-012)
  - [ ] Find employee in list
  - [ ] Click delete button
  - [ ] Confirm deletion dialog
  - [ ] Verify employee removed from list

- [ ] **Search Employees** (E2E-013)
  - [ ] Enter search query
  - [ ] Verify filtered results
  - [ ] Clear search
  - [ ] Verify all employees shown

- [ ] **Paginate Employees** (E2E-014)
  - [ ] Load employee list
  - [ ] Click next page
  - [ ] Verify new employees loaded
  - [ ] Change page size
  - [ ] Verify items per page updated

---

#### ✅ Payroll Workflows (Priority: HIGH)

- [ ] **Generate Payroll** (E2E-020)
  - [ ] Navigate to payroll dashboard
  - [ ] Select pay period
  - [ ] Click "Generate Payroll"
  - [ ] Verify calculation in progress
  - [ ] Verify payslips generated

- [ ] **View Payslip** (E2E-021)
  - [ ] Navigate to payslips
  - [ ] Click on payslip
  - [ ] Verify details displayed
  - [ ] Download PDF
  - [ ] Verify PDF downloaded

- [ ] **Process Payments** (E2E-022)
  - [ ] Select payslips
  - [ ] Click "Process Payments"
  - [ ] Confirm dialog
  - [ ] Verify payment status updated

---

#### ✅ Attendance Workflows (Priority: HIGH)

- [ ] **Clock In** (E2E-030)
  - [ ] Navigate to attendance
  - [ ] Click "Clock In"
  - [ ] Verify timestamp recorded
  - [ ] Verify status "In"

- [ ] **Clock Out** (E2E-031)
  - [ ] Click "Clock Out"
  - [ ] Verify timestamp recorded
  - [ ] Verify status "Out"
  - [ ] Verify hours calculated

- [ ] **View Attendance Report** (E2E-032)
  - [ ] Navigate to reports
  - [ ] Select date range
  - [ ] Generate report
  - [ ] Verify data displayed
  - [ ] Export report

---

#### ✅ Leave Management Workflows (Priority: MEDIUM)

- [ ] **Request Leave** (E2E-040)
  - [ ] Navigate to leave section
  - [ ] Click "Request Leave"
  - [ ] Select dates
  - [ ] Select leave type
  - [ ] Add reason
  - [ ] Submit request
  - [ ] Verify pending status

- [ ] **Approve Leave** (E2E-041)
  - [ ] Login as manager
  - [ ] Navigate to leave approvals
  - [ ] Review request
  - [ ] Click approve
  - [ ] Verify approved status
  - [ ] Verify employee notified

- [ ] **Reject Leave** (E2E-042)
  - [ ] Review request
  - [ ] Click reject
  - [ ] Add rejection reason
  - [ ] Verify rejected status

---

**Total E2E Scenarios:** 25-30 workflows | 50-70 test cases

---

## SECURITY TESTING CHECKLIST

### Automated Security Scans

#### ✅ Dependency Vulnerability Scanning

- [ ] **npm audit** (Weekly)
  - [ ] Run `npm audit`
  - [ ] Review critical vulnerabilities
  - [ ] Update vulnerable packages
  - [ ] Document exceptions

- [ ] **Snyk Scanning** (CI/CD)
  - [ ] Integrate Snyk
  - [ ] Configure thresholds
  - [ ] Set up alerts
  - [ ] Monitor dashboard

---

#### ✅ XSS (Cross-Site Scripting) Testing

- [ ] **Input Sanitization**
  - [ ] Test all input fields with XSS payloads
  - [ ] Test `<script>alert('XSS')</script>`
  - [ ] Test `<img src=x onerror=alert('XSS')>`
  - [ ] Test `javascript:alert('XSS')`
  - [ ] Test `<svg onload=alert('XSS')>`
  - [ ] Verify Angular sanitization working

- [ ] **Output Encoding**
  - [ ] Test user-generated content display
  - [ ] Test HTML rendering
  - [ ] Test URL rendering
  - [ ] Verify no innerHTML usage without sanitization

- [ ] **DOM-based XSS**
  - [ ] Test URL parameters
  - [ ] Test hash fragments
  - [ ] Test postMessage
  - [ ] Test localStorage data display

**Test Locations:**
- [ ] Login form
- [ ] Employee name field
- [ ] Comments/notes fields
- [ ] Search inputs
- [ ] Report filters

---

#### ✅ CSRF (Cross-Site Request Forgery) Testing

- [ ] **Token Validation**
  - [ ] Verify CSRF token on all POST/PUT/DELETE
  - [ ] Test request without CSRF token
  - [ ] Test request with invalid token
  - [ ] Test request with expired token
  - [ ] Verify SameSite cookie attribute

- [ ] **Anti-CSRF Measures**
  - [ ] Verify custom headers (X-Requested-With)
  - [ ] Test Origin header validation
  - [ ] Test Referer header validation

**Test Endpoints:**
- [ ] Login endpoint
- [ ] Employee create/update/delete
- [ ] Payroll generation
- [ ] Password reset

---

#### ✅ Authentication & Authorization Testing

- [ ] **Authentication Bypass**
  - [ ] Test access without token
  - [ ] Test access with expired token
  - [ ] Test access with invalid token
  - [ ] Test access with another user's token
  - [ ] Test token replay attacks

- [ ] **Authorization Bypass**
  - [ ] Test horizontal privilege escalation (access other tenant's data)
  - [ ] Test vertical privilege escalation (employee accessing admin routes)
  - [ ] Test role manipulation in token
  - [ ] Test direct URL access to protected routes

- [ ] **Session Management**
  - [ ] Test concurrent login prevention
  - [ ] Test session fixation
  - [ ] Test session timeout enforcement
  - [ ] Test logout token invalidation
  - [ ] Test token in multiple tabs

---

#### ✅ Injection Testing

- [ ] **SQL Injection** (Backend, but test frontend prevents)
  - [ ] Test `' OR '1'='1` in login
  - [ ] Test `'; DROP TABLE users; --` in inputs
  - [ ] Test `1' UNION SELECT * FROM users--`

- [ ] **NoSQL Injection**
  - [ ] Test `{"$ne": null}` payloads
  - [ ] Test JSON injection in API calls

- [ ] **Command Injection**
  - [ ] Test `; ls -la` in inputs
  - [ ] Test `| cat /etc/passwd`

**Note:** Frontend should never allow these through validation

---

#### ✅ Sensitive Data Exposure

- [ ] **Data in Transit**
  - [ ] Verify HTTPS enforced
  - [ ] Verify TLS 1.2+ used
  - [ ] Verify no mixed content warnings
  - [ ] Test HTTP to HTTPS redirect

- [ ] **Data at Rest**
  - [ ] Verify passwords not stored in localStorage
  - [ ] Verify sensitive data encrypted in localStorage
  - [ ] Verify tokens have expiration
  - [ ] Verify session data cleared on logout

- [ ] **Data in Logs**
  - [ ] Verify no passwords in console.log
  - [ ] Verify no tokens in console.log
  - [ ] Verify no PII in error messages
  - [ ] Verify no sensitive data in stack traces

- [ ] **Browser Storage**
  - [ ] Inspect localStorage for sensitive data
  - [ ] Inspect sessionStorage for sensitive data
  - [ ] Inspect cookies for secure/httpOnly flags
  - [ ] Verify IndexedDB doesn't contain PII

---

#### ✅ Security Headers Testing

- [ ] **HTTP Headers** (Backend, but verify in browser)
  - [ ] Verify Content-Security-Policy header
  - [ ] Verify X-Frame-Options: DENY
  - [ ] Verify X-Content-Type-Options: nosniff
  - [ ] Verify Strict-Transport-Security
  - [ ] Verify Referrer-Policy
  - [ ] Verify Permissions-Policy

**Tool:** Use SecurityHeaders.com or OWASP ZAP

---

#### ✅ Client-Side Security

- [ ] **Source Code Exposure**
  - [ ] Verify production build minified
  - [ ] Verify no source maps in production
  - [ ] Verify no API keys in frontend code
  - [ ] Verify no hardcoded credentials

- [ ] **Third-Party Dependencies**
  - [ ] Audit all npm packages
  - [ ] Verify no malicious packages
  - [ ] Check package download counts
  - [ ] Review package maintainers

---

**Security Testing Tools:**
- [ ] OWASP ZAP (automated scanning)
- [ ] Burp Suite Community (manual testing)
- [ ] npm audit (dependency scan)
- [ ] Snyk (vulnerability database)
- [ ] SecurityHeaders.com (header check)

---

## ACCESSIBILITY TESTING CHECKLIST

### WCAG 2.1 Level AA Compliance

#### ✅ Automated Testing

- [ ] **axe-core Integration**
  - [ ] Install @axe-core/angular
  - [ ] Run axe in unit tests
  - [ ] Fix all violations
  - [ ] Set up CI/CD checks

- [ ] **Lighthouse Accessibility Audit**
  - [ ] Run Lighthouse on all pages
  - [ ] Target score: 95+
  - [ ] Fix critical issues
  - [ ] Document exceptions

---

#### ✅ Keyboard Navigation

- [ ] **Tab Order**
  - [ ] Tab through all interactive elements
  - [ ] Verify logical tab order
  - [ ] Verify no keyboard traps
  - [ ] Test Shift+Tab (reverse)

- [ ] **Keyboard Shortcuts**
  - [ ] Enter activates buttons
  - [ ] Space activates buttons
  - [ ] Arrow keys in menus/lists
  - [ ] Escape closes dialogs
  - [ ] Home/End in lists

- [ ] **Focus Management**
  - [ ] Focus visible on all elements
  - [ ] Focus moves to dialog on open
  - [ ] Focus returns on dialog close
  - [ ] Skip links present
  - [ ] Focus indicator contrast ratio 3:1+

**Test Pages:**
- [ ] Login page
- [ ] Employee list
- [ ] Employee form
- [ ] Payroll dashboard
- [ ] Settings page

---

#### ✅ Screen Reader Testing

**Tools:** NVDA (Windows), JAWS (Windows), VoiceOver (Mac)

- [ ] **Page Structure**
  - [ ] Headings properly nested (h1 → h2 → h3)
  - [ ] Landmarks (main, nav, aside, footer)
  - [ ] Regions labeled
  - [ ] Lists properly marked up

- [ ] **Forms**
  - [ ] Labels associated with inputs
  - [ ] Required fields announced
  - [ ] Error messages announced
  - [ ] Fieldsets for grouped inputs
  - [ ] Legends for fieldsets

- [ ] **Dynamic Content**
  - [ ] aria-live for status messages
  - [ ] aria-live for loading states
  - [ ] aria-busy during async operations
  - [ ] Dialog role announced
  - [ ] Alert role announced

- [ ] **Tables**
  - [ ] Table headers (th)
  - [ ] Caption describes table
  - [ ] Scope attributes on headers
  - [ ] Data cells associated with headers

**Test Scenarios:**
- [ ] Login with screen reader
- [ ] Navigate employee list with screen reader
- [ ] Fill form with screen reader
- [ ] Operate dialog with screen reader
- [ ] Use datepicker with screen reader

---

#### ✅ Color Contrast

- [ ] **Text Contrast**
  - [ ] Normal text: 4.5:1 minimum
  - [ ] Large text (18pt+): 3:1 minimum
  - [ ] UI components: 3:1 minimum
  - [ ] Focus indicators: 3:1 minimum

**Tool:** Use WebAIM Contrast Checker or browser DevTools

**Test Elements:**
- [ ] Body text
- [ ] Button text
- [ ] Link text
- [ ] Error messages
- [ ] Placeholder text
- [ ] Disabled states

---

#### ✅ ARIA Attributes

- [ ] **Roles**
  - [ ] button role on clickable divs
  - [ ] dialog role on modals
  - [ ] alert role on alerts
  - [ ] navigation role on nav
  - [ ] main role on main content

- [ ] **States**
  - [ ] aria-expanded on collapsible
  - [ ] aria-selected on tabs
  - [ ] aria-checked on checkboxes
  - [ ] aria-pressed on toggle buttons
  - [ ] aria-disabled on disabled elements

- [ ] **Properties**
  - [ ] aria-label on unlabeled elements
  - [ ] aria-labelledby for associations
  - [ ] aria-describedby for descriptions
  - [ ] aria-required on required fields
  - [ ] aria-invalid on error fields
  - [ ] aria-live on dynamic content

---

#### ✅ Responsive and Zoom

- [ ] **Zoom Testing**
  - [ ] Test at 200% zoom
  - [ ] Verify no horizontal scroll
  - [ ] Verify text readable
  - [ ] Verify buttons clickable

- [ ] **Text Resize**
  - [ ] Increase browser text size to 200%
  - [ ] Verify layout doesn't break
  - [ ] Verify content visible

- [ ] **Responsive**
  - [ ] Test mobile viewport (320px)
  - [ ] Test tablet viewport (768px)
  - [ ] Test desktop viewport (1920px)

---

**Accessibility Audit Score Target: 95+**

---

## PERFORMANCE TESTING CHECKLIST

### Sprint 4: Performance Benchmarks

#### ✅ Lighthouse Performance Audit

**Target Scores:**
- Performance: 90+
- Accessibility: 95+
- Best Practices: 95+
- SEO: 90+

**Test Pages:**
- [ ] Login page
- [ ] Dashboard
- [ ] Employee list (with 100 items)
- [ ] Employee form

**Metrics:**
- [ ] First Contentful Paint (FCP): < 1.8s
- [ ] Largest Contentful Paint (LCP): < 2.5s
- [ ] Time to Interactive (TTI): < 3.8s
- [ ] Total Blocking Time (TBT): < 200ms
- [ ] Cumulative Layout Shift (CLS): < 0.1

---

#### ✅ Bundle Size Analysis

- [ ] **Bundle Size Targets**
  - [ ] Initial bundle: < 200kB gzipped
  - [ ] Lazy-loaded chunks: < 100kB gzipped
  - [ ] Vendor bundle: < 500kB gzipped

- [ ] **Bundle Analysis**
  - [ ] Run `npm run build -- --stats-json`
  - [ ] Analyze with webpack-bundle-analyzer
  - [ ] Identify large dependencies
  - [ ] Implement code splitting

- [ ] **Tree Shaking**
  - [ ] Verify unused code removed
  - [ ] Check dead code elimination
  - [ ] Optimize imports (use specific imports)

---

#### ✅ Load Testing

**Tool:** Apache JMeter or k6

- [ ] **Concurrent Users**
  - [ ] Test with 10 concurrent users
  - [ ] Test with 50 concurrent users
  - [ ] Test with 100 concurrent users
  - [ ] Test with 500 concurrent users

- [ ] **Scenarios**
  - [ ] Login load test
  - [ ] Dashboard load test
  - [ ] Employee list load test
  - [ ] Report generation load test

**Acceptance Criteria:**
- [ ] 95th percentile response time < 2s
- [ ] Error rate < 1%
- [ ] Server CPU < 80%
- [ ] Server memory < 80%

---

#### ✅ Rendering Performance

- [ ] **Component Render Time**
  - [ ] Measure table render with 100 rows
  - [ ] Measure table render with 1000 rows
  - [ ] Measure form render
  - [ ] Measure datepicker render

**Target:** < 16ms per frame (60fps)

- [ ] **Change Detection**
  - [ ] Profile change detection cycles
  - [ ] Identify unnecessary re-renders
  - [ ] Optimize with OnPush strategy

- [ ] **Virtual Scrolling**
  - [ ] Implement for large lists
  - [ ] Test with 10,000 items
  - [ ] Verify smooth scrolling

---

#### ✅ Network Performance

- [ ] **API Response Times**
  - [ ] Login API: < 500ms
  - [ ] Employee list API: < 1s
  - [ ] Payroll calculation API: < 3s

- [ ] **HTTP Caching**
  - [ ] Verify Cache-Control headers
  - [ ] Verify ETag headers
  - [ ] Test offline mode (Service Worker)

- [ ] **Compression**
  - [ ] Verify gzip enabled
  - [ ] Verify brotli enabled (if available)
  - [ ] Test compression ratios

---

#### ✅ Memory Leak Detection

- [ ] **Memory Profiling**
  - [ ] Record heap snapshots
  - [ ] Navigate through app
  - [ ] Compare heap snapshots
  - [ ] Verify memory released

- [ ] **Subscription Leaks**
  - [ ] Audit all subscriptions
  - [ ] Verify unsubscribe in ngOnDestroy
  - [ ] Use takeUntil pattern
  - [ ] Test with Chrome DevTools memory profiler

**Target:** No memory growth after 5 minutes of use

---

## BROWSER COMPATIBILITY CHECKLIST

### Target Browsers

#### ✅ Desktop Browsers

- [ ] **Chrome**
  - [ ] Chrome 90+ (Windows)
  - [ ] Chrome 90+ (macOS)
  - [ ] Chrome 90+ (Linux)

- [ ] **Firefox**
  - [ ] Firefox 88+ (Windows)
  - [ ] Firefox 88+ (macOS)
  - [ ] Firefox 88+ (Linux)

- [ ] **Safari**
  - [ ] Safari 14+ (macOS)
  - [ ] Safari 15+ (macOS)

- [ ] **Edge**
  - [ ] Edge 90+ (Windows)
  - [ ] Edge 90+ (macOS)

---

#### ✅ Mobile Browsers

- [ ] **iOS Safari**
  - [ ] iOS Safari 14+ (iPhone)
  - [ ] iOS Safari 15+ (iPhone)
  - [ ] iOS Safari 14+ (iPad)

- [ ] **Chrome Mobile**
  - [ ] Chrome 90+ (Android)
  - [ ] Chrome Mobile (iOS)

- [ ] **Samsung Internet**
  - [ ] Samsung Internet 14+ (Android)

---

#### ✅ Feature Testing per Browser

**Login Page:**
- [ ] Form rendering
- [ ] Input validation
- [ ] Password toggle
- [ ] Remember me checkbox
- [ ] Form submission
- [ ] Error display

**Dashboard:**
- [ ] Charts rendering (Chart.js)
- [ ] Real-time updates (SignalR)
- [ ] Navigation
- [ ] Responsive layout

**Employee List:**
- [ ] Table rendering
- [ ] Sorting
- [ ] Filtering
- [ ] Pagination
- [ ] Selection

**Employee Form:**
- [ ] All input types
- [ ] Datepicker
- [ ] Select dropdown
- [ ] File upload
- [ ] Form validation
- [ ] Form submission

---

#### ✅ Responsive Testing

**Viewports:**
- [ ] Mobile Portrait (320px)
- [ ] Mobile Landscape (568px)
- [ ] Tablet Portrait (768px)
- [ ] Tablet Landscape (1024px)
- [ ] Desktop (1280px)
- [ ] Large Desktop (1920px)

**Test Elements:**
- [ ] Navigation menu (hamburger on mobile)
- [ ] Data tables (horizontal scroll)
- [ ] Forms (stacked on mobile)
- [ ] Dialogs (full-screen on mobile)
- [ ] Buttons (touch targets 44x44px minimum)

---

#### ✅ Cross-Browser Testing Tools

- [ ] **BrowserStack** (Recommended)
  - [ ] Set up account
  - [ ] Configure test suite
  - [ ] Run automated tests
  - [ ] Manual exploratory testing

- [ ] **Sauce Labs** (Alternative)
  - [ ] Cloud-based testing
  - [ ] Real devices

- [ ] **Playwright**
  - [ ] Chromium testing
  - [ ] Firefox testing
  - [ ] WebKit testing

---

## TESTING INFRASTRUCTURE

### CI/CD Integration

#### ✅ GitHub Actions / GitLab CI Setup

- [ ] **Unit Tests**
  - [ ] Run on every commit
  - [ ] Run on pull requests
  - [ ] Block merge if tests fail
  - [ ] Report coverage to SonarQube

- [ ] **E2E Tests**
  - [ ] Run on pull requests
  - [ ] Run nightly on main branch
  - [ ] Record videos of failures
  - [ ] Upload screenshots on failure

- [ ] **Security Scans**
  - [ ] npm audit on every build
  - [ ] Snyk scan on pull requests
  - [ ] OWASP ZAP weekly scan

- [ ] **Accessibility**
  - [ ] axe-core on every build
  - [ ] Lighthouse in CI
  - [ ] pa11y-ci integration

- [ ] **Performance**
  - [ ] Lighthouse CI
  - [ ] Bundle size tracking
  - [ ] Performance budget enforcement

---

### Test Reporting

- [ ] **Coverage Reports**
  - [ ] Generate HTML reports
  - [ ] Publish to SonarQube
  - [ ] Track coverage trends

- [ ] **Test Results**
  - [ ] JUnit XML format
  - [ ] Publish to test management tool
  - [ ] Track flaky tests

- [ ] **E2E Videos**
  - [ ] Record all E2E tests
  - [ ] Upload to cloud storage
  - [ ] Link in CI/CD logs

---

## DEFINITION OF DONE

A test is considered DONE when:

- [ ] ✅ Test code written
- [ ] ✅ Test passing locally
- [ ] ✅ Test passing in CI/CD
- [ ] ✅ Code review approved
- [ ] ✅ Coverage meets threshold
- [ ] ✅ Edge cases covered
- [ ] ✅ Documentation updated
- [ ] ✅ No flaky tests
- [ ] ✅ Performance acceptable

---

## SUCCESS METRICS

### Phase 2 Completion Criteria

- [ ] **Coverage Targets**
  - [ ] Overall: 80%+
  - [ ] Critical services: 90%+
  - [ ] Guards/interceptors: 100%
  - [ ] UI components: 85%+

- [ ] **Quality Targets**
  - [ ] All tests passing
  - [ ] 0 flaky tests
  - [ ] 0 critical bugs
  - [ ] < 5 medium bugs

- [ ] **Performance Targets**
  - [ ] Lighthouse score: 90+
  - [ ] Bundle size: < 200kB gzipped
  - [ ] FCP: < 1.8s
  - [ ] LCP: < 2.5s

- [ ] **Security Targets**
  - [ ] 0 critical vulnerabilities
  - [ ] 0 high vulnerabilities
  - [ ] < 5 medium vulnerabilities

- [ ] **Accessibility Targets**
  - [ ] WCAG 2.1 AA compliance
  - [ ] axe-core: 0 violations
  - [ ] Lighthouse accessibility: 95+

---

**END OF QA CHECKLIST**

Total Estimated Effort: 8 weeks | 3 QA engineers | 635-755 tests
