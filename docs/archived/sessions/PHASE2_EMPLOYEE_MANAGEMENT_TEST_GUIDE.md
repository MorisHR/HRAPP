# Phase 2 - Employee Management Testing Guide

## Overview
This guide provides step-by-step instructions for testing the Employee Management system with full expatriate worker support.

## Prerequisites
1. PostgreSQL database running
2. .NET 8 SDK installed
3. API running: `dotnet run --project src/HRMS.API`
4. Swagger UI: `http://localhost:5000/swagger`

---

## Step 1: Authentication

### 1.1 Login as Super Admin
```http
POST /api/admin/auth/login
Content-Type: application/json

{
  "email": "admin@hrms.com",
  "password": "Admin@123"
}
```

**Expected Response:**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "...",
    "email": "admin@hrms.com",
    "role": "SuperAdmin"
  }
}
```

### 1.2 Authorize in Swagger
1. Click "Authorize" button in Swagger
2. Enter: `Bearer {your-token-here}`
3. Click "Authorize" then "Close"

---

## Step 2: Create a Tenant

```http
POST /api/tenants
Content-Type: application/json
Authorization: Bearer {token}

{
  "companyName": "Test Company Ltd",
  "subdomain": "testco",
  "contactEmail": "contact@testco.com",
  "contactPhone": "+230 5123 4567",
  "address": "Port Louis, Mauritius",
  "subscriptionType": "Enterprise",
  "maxUsers": 100
}
```

**Note:** Copy the `tenantId` from the response for the next steps.

---

## Step 3: Create a Department

```http
POST /api/employees
Content-Type: application/json
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}

{
  "departmentCode": "IT",
  "departmentName": "Information Technology",
  "description": "IT Department"
}
```

**Note:** If department endpoint doesn't exist yet, employees can be created without departmentId initially.

---

## Step 4: Create a Local Employee

### 4.1 Create Local Mauritian Employee
```http
POST /api/employees
Content-Type: application/json
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}

{
  "employeeCode": "EMP001",
  "employeeType": 1,
  "firstName": "Jean",
  "lastName": "Rousseau",
  "email": "jean.rousseau@testco.com",
  "phoneNumber": "+230 5234 5678",
  "dateOfBirth": "1990-05-15",
  "gender": 1,
  "maritalStatus": 1,
  "nationality": "Mauritius",
  "nationalIdCard": "M150590234567H",
  "address": "123 Royal Road, Quatre Bornes",
  "city": "Quatre Bornes",
  "postalCode": "72301",
  "joiningDate": "2024-01-15",
  "jobTitle": "Software Developer",
  "basicSalary": 45000.00,
  "salaryCurrency": "MUR",
  "isNPFEligible": true,
  "isNSFEligible": true,
  "npfNumber": "NPF123456",
  "taxIdNumber": "TAN123456",
  "bankName": "MCB",
  "bankAccountNumber": "000123456789",
  "emergencyContacts": [
    {
      "contactName": "Marie Rousseau",
      "phoneNumber": "+230 5789 0123",
      "email": "marie.r@email.com",
      "relationship": "Spouse",
      "contactType": "Local",
      "isPrimary": true
    }
  ]
}
```

**Expected Result:**
- Status: `201 Created`
- Employee created with `AnnualLeaveBalance` calculated (pro-rated 22 days)
- No validation errors

---

## Step 5: Create an Expatriate Employee

### 5.1 Create Expatriate Employee (Valid)
```http
POST /api/employees
Content-Type: application/json
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}

{
  "employeeCode": "EXP001",
  "employeeType": 2,
  "firstName": "John",
  "lastName": "Smith",
  "middleName": "Michael",
  "email": "john.smith@testco.com",
  "phoneNumber": "+230 5345 6789",
  "dateOfBirth": "1985-08-20",
  "gender": 1,
  "maritalStatus": 2,
  "nationality": "British",
  "countryOfOrigin": "United Kingdom",
  "passportNumber": "GB123456789",
  "passportIssueDate": "2022-01-15",
  "passportExpiryDate": "2027-01-14",
  "visaType": 2,
  "visaNumber": "MU-OP-2024-001",
  "visaIssueDate": "2024-01-01",
  "visaExpiryDate": "2026-12-31",
  "workPermitNumber": "WP-2024-001",
  "workPermitExpiryDate": "2026-12-31",
  "address": "456 Coastal Road, Grand Baie",
  "city": "Grand Baie",
  "postalCode": "30501",
  "joiningDate": "2024-03-01",
  "contractEndDate": "2026-12-31",
  "jobTitle": "Senior Software Architect",
  "basicSalary": 150000.00,
  "salaryCurrency": "MUR",
  "taxResidentStatus": 2,
  "isNPFEligible": false,
  "isNSFEligible": false,
  "bankName": "MCB",
  "bankAccountNumber": "000987654321",
  "bankSwiftCode": "MCBMMU2M",
  "emergencyContacts": [
    {
      "contactName": "Sarah Smith",
      "phoneNumber": "+44 7700 900123",
      "email": "sarah.smith@email.com",
      "relationship": "Spouse",
      "contactType": "HomeCountry",
      "address": "London, UK",
      "country": "United Kingdom",
      "isPrimary": true
    },
    {
      "contactName": "Jane Doe",
      "phoneNumber": "+230 5456 7890",
      "email": "jane.doe@email.com",
      "relationship": "Friend",
      "contactType": "Local",
      "address": "Grand Baie, Mauritius",
      "country": "Mauritius",
      "isPrimary": false
    }
  ]
}
```

**Expected Result:**
- Status: `201 Created`
- All mandatory expatriate fields validated
- Leave balance calculated
- Emergency contacts created

---

## Step 6: Test Validation Errors

### 6.1 Missing Mandatory Fields for Expatriate
```http
POST /api/employees
Content-Type: application/json
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}

{
  "employeeCode": "EXP002",
  "employeeType": 2,
  "firstName": "Invalid",
  "lastName": "Expat",
  "email": "invalid@testco.com",
  "joiningDate": "2024-01-01",
  "jobTitle": "Test",
  "basicSalary": 50000
}
```

**Expected Result:**
- Status: `400 Bad Request`
- Error message listing missing fields:
  - Country of Origin is required
  - Passport Number is required
  - Passport Expiry Date is required
  - Visa Type is required
  - Visa Expiry Date is required

### 6.2 Expired Passport
```http
POST /api/employees
Content-Type: application/json
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}

{
  "employeeCode": "EXP003",
  "employeeType": 2,
  "firstName": "Expired",
  "lastName": "Passport",
  "email": "expired@testco.com",
  "nationality": "Indian",
  "countryOfOrigin": "India",
  "passportNumber": "IN999999",
  "passportExpiryDate": "2023-01-01",
  "visaType": 1,
  "visaExpiryDate": "2025-12-31",
  "joiningDate": "2024-01-01",
  "jobTitle": "Test",
  "basicSalary": 50000
}
```

**Expected Result:**
- Status: `400 Bad Request`
- Error: "Passport Expiry Date must be in the future"

### 6.3 Work Permit Expires After Passport
```http
POST /api/employees
Content-Type: application/json
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}

{
  "employeeCode": "EXP004",
  "employeeType": 2,
  "firstName": "Invalid",
  "lastName": "Logic",
  "email": "invalid2@testco.com",
  "nationality": "French",
  "countryOfOrigin": "France",
  "passportNumber": "FR123456",
  "passportExpiryDate": "2025-06-30",
  "visaType": 1,
  "visaExpiryDate": "2026-12-31",
  "workPermitExpiryDate": "2027-12-31",
  "joiningDate": "2024-01-01",
  "jobTitle": "Test",
  "basicSalary": 50000
}
```

**Expected Result:**
- Status: `400 Bad Request`
- Error: "Work Permit cannot expire after Passport expiry"

---

## Step 7: Retrieve Employees

### 7.1 Get All Employees
```http
GET /api/employees?includeInactive=false
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}
```

**Expected Result:**
- List of all active employees
- Count field showing total
- Both local and expatriate employees

### 7.2 Get Employee by ID
```http
GET /api/employees/{employee-id}
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}
```

**Expected Result:**
- Full employee details
- Emergency contacts included
- Calculated properties (e.g., YearsOfService, Age)

### 7.3 Get Employee by Code
```http
GET /api/employees/code/EXP001
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}
```

**Expected Result:**
- Employee with code "EXP001"

### 7.4 Get Only Expatriates
```http
GET /api/employees/expatriates
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}
```

**Expected Result:**
- Only employees with `EmployeeType = 2`

---

## Step 8: Document Expiry Tracking

### 8.1 Get Expiring Documents (90 days)
```http
GET /api/employees/expiring-documents?daysAhead=90
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}
```

**Expected Result:**
```json
{
  "success": true,
  "data": [
    {
      "employeeId": "...",
      "employeeCode": "EXP001",
      "employeeName": "John Michael Smith",
      "documentType": "Passport",
      "documentNumber": "GB123456789",
      "expiryDate": "2027-01-14",
      "daysUntilExpiry": 1095,
      "expiryStatus": 1,
      "requiresUrgentAction": false
    }
  ],
  "totalCount": 1,
  "criticalCount": 0,
  "daysAhead": 90
}
```

### 8.2 Get Document Status for Specific Employee
```http
GET /api/employees/{employee-id}/document-status
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}
```

**Expected Result:**
- Document expiry information
- Status for passport, visa, work permit
- Days until expiry
- Urgent action flag

---

## Step 9: Visa Renewal

### 9.1 Renew Visa
```http
POST /api/employees/{employee-id}/renew-visa
Content-Type: application/json
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}

{
  "newExpiryDate": "2028-12-31",
  "newVisaNumber": "MU-OP-2028-001"
}
```

**Expected Result:**
- Status: `200 OK`
- Visa details updated
- New expiry status calculated

---

## Step 10: Search and Filter

### 10.1 Search Employees
```http
GET /api/employees/search?q=John
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}
```

**Expected Result:**
- Employees matching "John" in name, email, or code

### 10.2 Get Employees by Country
```http
GET /api/employees/by-country
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}
```

**Expected Result:**
```json
{
  "success": true,
  "data": {
    "United Kingdom": 1,
    "Mauritius": 1
  },
  "totalCountries": 2
}
```

---

## Step 11: Update Employee

### 11.1 Update Employee Information
```http
PUT /api/employees/{employee-id}
Content-Type: application/json
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}

{
  "firstName": "John",
  "lastName": "Smith-Updated",
  "email": "john.smith.updated@testco.com",
  "phoneNumber": "+230 5999 9999",
  "basicSalary": 160000.00
}
```

**Expected Result:**
- Status: `200 OK`
- Updated fields reflected
- Validation still applies

---

## Step 12: Soft Delete Employee

### 12.1 Delete Employee
```http
DELETE /api/employees/{employee-id}
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}
```

**Expected Result:**
- Status: `200 OK`
- Employee marked as deleted
- Will not appear in default listings

### 12.2 Verify Soft Delete
```http
GET /api/employees?includeInactive=false
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}
```

**Expected Result:**
- Deleted employee NOT in list

```http
GET /api/employees?includeInactive=true
Authorization: Bearer {token}
X-Tenant-Id: {your-tenant-id}
```

**Expected Result:**
- Deleted employee IS in list

---

## Validation Test Matrix

| Test Case | Employee Type | Expected Result | Status |
|-----------|---------------|-----------------|---------|
| Local employee without NIC | Local | Validation Error | ❌ |
| Local employee with NIC | Local | Success | ✅ |
| Expatriate without passport | Expatriate | Validation Error | ❌ |
| Expatriate without visa | Expatriate | Validation Error | ❌ |
| Expatriate with expired passport | Expatriate | Validation Error | ❌ |
| Expatriate with expired visa | Expatriate | Validation Error | ❌ |
| Work permit > passport expiry | Expatriate | Validation Error | ❌ |
| Duplicate employee code | Any | Validation Error | ❌ |
| Duplicate email | Any | Validation Error | ❌ |
| Valid expatriate (all fields) | Expatriate | Success | ✅ |

---

## Pro-Rated Leave Calculation Test

### Test Scenarios
1. **Joined January 1st** → Full 22 days
2. **Joined July 1st** → ~11 days (6 months remaining)
3. **Joined October 1st** → ~5.5 days (3 months remaining)
4. **Joined December 1st** → ~1.83 days (1 month remaining)

### Verification
```http
POST /api/employees
(Create employee with joiningDate = "2024-07-01")
```

Check response: `annualLeaveBalance` should be approximately **11.00 days**

---

## Document Expiry Status Logic

| Days Until Expiry | Status | Color Code | Urgent |
|-------------------|--------|------------|--------|
| > 90 days | Valid | Green | No |
| 30-90 days | ExpiringSoon | Yellow | No |
| 15-30 days | ExpiringVerySoon | Orange | No |
| < 15 days | Critical | Red | Yes |
| Already expired | Expired | Red | Yes |

---

## Common Issues and Solutions

### Issue 1: "Employee code already exists"
**Solution:** Use a unique employee code for each employee

### Issue 2: "Email already exists"
**Solution:** Ensure each employee has a unique email address

### Issue 3: "Cannot resolve TenantDbContext"
**Solution:** Ensure X-Tenant-Id header is provided and tenant exists

### Issue 4: "Migration failed"
**Solution:** Ensure PostgreSQL is running and connection string is correct

---

## Database Verification Queries

```sql
-- View all employees in tenant schema
SELECT employee_code, first_name, last_name, employee_type, nationality
FROM tenant_testco.employees
WHERE is_deleted = false;

-- View expatriate employees with document expiry
SELECT employee_code, first_name, last_name,
       passport_expiry_date, visa_expiry_date, work_permit_expiry_date
FROM tenant_testco.employees
WHERE employee_type = 2 AND is_deleted = false;

-- View emergency contacts
SELECT e.employee_code, ec.contact_name, ec.relationship, ec.contact_type
FROM tenant_testco.emergency_contacts ec
JOIN tenant_testco.employees e ON ec.employee_id = e.id
WHERE e.is_deleted = false;
```

---

## Success Criteria

- ✅ All API endpoints return expected responses
- ✅ Validation rules enforce data integrity
- ✅ Local and expatriate employees can be created
- ✅ Document expiry tracking works correctly
- ✅ Pro-rated leave calculation is accurate
- ✅ Emergency contacts are properly linked
- ✅ Search and filter functions work
- ✅ Soft delete preserves data

---

## Next Steps (Phase 3)

1. **Department Management** - CRUD for departments
2. **Document Upload** - File storage for passport/visa scans
3. **Onboarding Workflow** - Multi-step onboarding process
4. **Automated Alerts** - Hangfire jobs for expiry notifications
5. **Leave Management** - Apply, approve, track leave
6. **Payroll Integration** - Salary calculations with statutory deductions
