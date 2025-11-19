-- =========================================================================
-- DEMO EMPLOYEE SEED DATA
-- Fortune 500-grade sample data for HRMS demo
-- Run this script against a specific tenant schema (e.g., tenant_siraaj)
-- =========================================================================

-- Set the schema (replace 'tenant_siraaj' with your actual tenant schema)
SET search_path TO tenant_siraaj;

-- First, ensure we have demo departments
INSERT INTO "Departments" ("Id", "Name", "Code", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted")
VALUES
    ('11111111-1111-1111-1111-111111111111', 'Engineering', 'ENG', true, NOW(), NOW(), false),
    ('22222222-2222-2222-2222-222222222222', 'Sales', 'SAL', true, NOW(), NOW(), false),
    ('33333333-3333-3333-3333-333333333333', 'Human Resources', 'HR', true, NOW(), NOW(), false),
    ('44444444-4444-4444-4444-444444444444', 'Finance', 'FIN', true, NOW(), NOW(), false),
    ('55555555-5555-5555-5555-555555555555', 'Marketing', 'MKT', true, NOW(), NOW(), false),
    ('66666666-6666-6666-6666-666666666666', 'Customer Support', 'CS', true, NOW(), NOW(), false),
    ('77777777-7777-7777-7777-777777777777', 'Design', 'DES', true, NOW(), NOW(), false)
ON CONFLICT ("Id") DO NOTHING;

-- Now insert the 15 demo employees
INSERT INTO "Employees" (
    "Id",
    "EmployeeCode",
    "FirstName",
    "MiddleName",
    "LastName",
    "Email",
    "PhoneNumber",
    "DateOfBirth",
    "Gender",
    "MaritalStatus",
    "AddressLine1",
    "Country",
    "Nationality",
    "EmployeeType",
    "TaxResidentStatus",
    "IsNPFEligible",
    "IsNSFEligible",
    "Designation",
    "JobTitle",
    "DepartmentId",
    "JoiningDate",
    "EmploymentContractType",
    "EmploymentStatus",
    "BasicSalary",
    "PaymentFrequency",
    "SalaryCurrency",
    "AnnualLeaveDays",
    "SickLeaveDays",
    "CasualLeaveDays",
    "AnnualLeaveBalance",
    "SickLeaveBalance",
    "CasualLeaveBalance",
    "IsActive",
    "CreatedAt",
    "UpdatedAt",
    "IsDeleted"
)
VALUES
-- 1. Sarah Johnson - Senior Software Engineer
(
    'e0000001-0000-0000-0000-000000000001',
    'EMP001',
    'Sarah',
    '',
    'Johnson',
    'sarah.johnson@company.com',
    '+1-555-0101',
    '1988-05-15',
    1, -- Female
    1, -- Single
    '123 Tech Street',
    'Mauritius',
    'Mauritius',
    0, -- Local
    0, -- Tax Resident
    true, -- NPF Eligible
    false, -- NSF Eligible
    'Senior Software Engineer',
    'Senior Software Engineer',
    '11111111-1111-1111-1111-111111111111', -- Engineering
    '2021-03-15',
    'Permanent',
    'Active',
    75000.00,
    'Monthly',
    'MUR',
    20,
    15,
    5,
    20.0,
    15.0,
    5.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 2. Michael Chen - Sales Director
(
    'e0000002-0000-0000-0000-000000000002',
    'EMP002',
    'Michael',
    'Chen',
    'michael.chen@company.com',
    '+1-555-0102',
    '1985-08-22',
    0, -- Male
    2, -- Married
    '456 Sales Avenue',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'Sales Director',
    'Sales Director',
    '22222222-2222-2222-2222-222222222222', -- Sales
    '2020-01-10',
    'Permanent',
    'Active',
    95000.00,
    'Monthly',
    'MUR',
    22,
    15,
    5,
    22.0,
    15.0,
    5.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 3. Emily Rodriguez - HR Manager
(
    'e0000003-0000-0000-0000-000000000003',
    'EMP003',
    'Emily',
    'Rodriguez',
    'emily.rodriguez@company.com',
    '+1-555-0103',
    '1990-03-10',
    1, -- Female
    2, -- Married
    '789 HR Boulevard',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'HR Manager',
    'HR Manager',
    '33333333-3333-3333-3333-333333333333', -- HR
    '2019-08-22',
    'Permanent',
    'Active',
    72000.00,
    'Monthly',
    'MUR',
    20,
    15,
    5,
    20.0,
    15.0,
    5.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 4. David Thompson - Financial Analyst
(
    'e0000004-0000-0000-0000-000000000004',
    'EMP004',
    'David',
    'Thompson',
    'david.thompson@company.com',
    '+1-555-0104',
    '1992-11-05',
    0, -- Male
    1, -- Single
    '321 Finance Road',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'Financial Analyst',
    'Financial Analyst',
    '44444444-4444-4444-4444-444444444444', -- Finance
    '2022-05-01',
    'Permanent',
    'Active',
    62000.00,
    'Monthly',
    'MUR',
    20,
    15,
    5,
    20.0,
    15.0,
    5.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 5. Jessica Martinez - DevOps Engineer (On Leave)
(
    'e0000005-0000-0000-0000-000000000005',
    'EMP005',
    'Jessica',
    'Martinez',
    'jessica.martinez@company.com',
    '+1-555-0105',
    '1991-07-18',
    1, -- Female
    1, -- Single
    '654 DevOps Lane',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'DevOps Engineer',
    'DevOps Engineer',
    '11111111-1111-1111-1111-111111111111', -- Engineering
    '2021-11-15',
    'Permanent',
    'Active',
    68000.00,
    'Monthly',
    'MUR',
    20,
    15,
    5,
    18.0, -- Some leave taken
    13.0,
    4.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 6. Robert Anderson - Marketing Manager
(
    'e0000006-0000-0000-0000-000000000006',
    'EMP006',
    'Robert',
    'Anderson',
    'robert.anderson@company.com',
    '+1-555-0106',
    '1986-12-30',
    0, -- Male
    2, -- Married
    '987 Marketing Plaza',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'Marketing Manager',
    'Marketing Manager',
    '55555555-5555-5555-5555-555555555555', -- Marketing
    '2020-07-20',
    'Permanent',
    'Active',
    78000.00,
    'Monthly',
    'MUR',
    21,
    15,
    5,
    21.0,
    15.0,
    5.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 7. Amanda Taylor - QA Engineer
(
    'e0000007-0000-0000-0000-000000000007',
    'EMP007',
    'Amanda',
    'Taylor',
    'amanda.taylor@company.com',
    '+1-555-0107',
    '1993-02-14',
    1, -- Female
    1, -- Single
    '147 Quality Street',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'QA Engineer',
    'QA Engineer',
    '11111111-1111-1111-1111-111111111111', -- Engineering
    '2022-02-14',
    'Permanent',
    'Active',
    58000.00,
    'Monthly',
    'MUR',
    20,
    15,
    5,
    20.0,
    15.0,
    5.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 8. Christopher Wilson - Account Executive
(
    'e0000008-0000-0000-0000-000000000008',
    'EMP008',
    'Christopher',
    'Wilson',
    'christopher.wilson@company.com',
    '+1-555-0108',
    '1994-09-25',
    0, -- Male
    1, -- Single
    '258 Sales Court',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'Account Executive',
    'Account Executive',
    '22222222-2222-2222-2222-222222222222', -- Sales
    '2023-01-05',
    'Permanent',
    'Active',
    55000.00,
    'Monthly',
    'MUR',
    20,
    15,
    5,
    20.0,
    15.0,
    5.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 9. Jennifer Brown - Accountant (On Leave)
(
    'e0000009-0000-0000-0000-000000000009',
    'EMP009',
    'Jennifer',
    'Brown',
    'jennifer.brown@company.com',
    '+1-555-0109',
    '1989-06-10',
    1, -- Female
    2, -- Married
    '369 Finance Center',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'Accountant',
    'Accountant',
    '44444444-4444-4444-4444-444444444444', -- Finance
    '2021-06-10',
    'Permanent',
    'Active',
    64000.00,
    'Monthly',
    'MUR',
    20,
    15,
    5,
    17.0, -- Some leave taken
    12.0,
    3.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 10. Matthew Davis - Product Manager
(
    'e0000010-0000-0000-0000-000000000010',
    'EMP010',
    'Matthew',
    'Davis',
    'matthew.davis@company.com',
    '+1-555-0110',
    '1987-04-18',
    0, -- Male
    2, -- Married
    '741 Product Drive',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'Product Manager',
    'Product Manager',
    '11111111-1111-1111-1111-111111111111', -- Engineering
    '2020-09-18',
    'Permanent',
    'Active',
    85000.00,
    'Monthly',
    'MUR',
    21,
    15,
    5,
    21.0,
    15.0,
    5.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 11. Lisa Garcia - Support Team Lead
(
    'e0000011-0000-0000-0000-000000000011',
    'EMP011',
    'Lisa',
    'Garcia',
    'lisa.garcia@company.com',
    '+1-555-0111',
    '1991-10-22',
    1, -- Female
    1, -- Single
    '852 Support Way',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'Support Team Lead',
    'Support Team Lead',
    '66666666-6666-6666-6666-666666666666', -- Customer Support
    '2019-12-01',
    'Permanent',
    'Active',
    67000.00,
    'Monthly',
    'MUR',
    20,
    15,
    5,
    20.0,
    15.0,
    5.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 12. Daniel Miller - Frontend Developer
(
    'e0000012-0000-0000-0000-000000000012',
    'EMP012',
    'Daniel',
    'Miller',
    'daniel.miller@company.com',
    '+1-555-0112',
    '1995-08-25',
    0, -- Male
    1, -- Single
    '963 Frontend Plaza',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'Frontend Developer',
    'Frontend Developer',
    '11111111-1111-1111-1111-111111111111', -- Engineering
    '2022-08-25',
    'Permanent',
    'Active',
    61000.00,
    'Monthly',
    'MUR',
    20,
    15,
    5,
    20.0,
    15.0,
    5.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 13. Michelle Lee - UX Designer
(
    'e0000013-0000-0000-0000-000000000013',
    'EMP013',
    'Michelle',
    'Lee',
    'michelle.lee@company.com',
    '+1-555-0113',
    '1992-03-10',
    1, -- Female
    1, -- Single
    '159 Design Avenue',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'UX Designer',
    'UX Designer',
    '77777777-7777-7777-7777-777777777777', -- Design
    '2023-03-10',
    'Permanent',
    'Active',
    63000.00,
    'Monthly',
    'MUR',
    20,
    15,
    5,
    20.0,
    15.0,
    5.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 14. Kevin White - Business Development Manager (On Leave)
(
    'e0000014-0000-0000-0000-000000000014',
    'EMP014',
    'Kevin',
    'White',
    'kevin.white@company.com',
    '+1-555-0114',
    '1988-11-20',
    0, -- Male
    2, -- Married
    '357 Business Boulevard',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'Business Development Manager',
    'Business Development Manager',
    '22222222-2222-2222-2222-222222222222', -- Sales
    '2021-04-20',
    'Permanent',
    'Active',
    82000.00,
    'Monthly',
    'MUR',
    21,
    15,
    5,
    19.0, -- Some leave taken
    14.0,
    4.0,
    true,
    NOW(),
    NOW(),
    false
),

-- 15. Rachel Harris - Backend Developer
(
    'e0000015-0000-0000-0000-000000000015',
    'EMP015',
    'Rachel',
    'Harris',
    'rachel.harris@company.com',
    '+1-555-0115',
    '1993-11-08',
    1, -- Female
    1, -- Single
    '753 Backend Court',
    'Mauritius',
    'Mauritius',
    0, -- Local
    'Backend Developer',
    'Backend Developer',
    '11111111-1111-1111-1111-111111111111', -- Engineering
    '2022-11-08',
    'Permanent',
    'Active',
    66000.00,
    'Monthly',
    'MUR',
    20,
    15,
    5,
    20.0,
    15.0,
    5.0,
    true,
    NOW(),
    NOW(),
    false
)
ON CONFLICT ("Id") DO NOTHING;

-- Verify the seed data
SELECT
    "EmployeeCode",
    "FirstName" || ' ' || "LastName" AS "FullName",
    "Email",
    "Designation",
    "EmploymentStatus"
FROM "Employees"
WHERE "EmployeeCode" LIKE 'EMP%'
ORDER BY "EmployeeCode";

-- Print summary
DO $$
BEGIN
    RAISE NOTICE '===========================================';
    RAISE NOTICE 'Demo Employee Seed Completed Successfully!';
    RAISE NOTICE '===========================================';
    RAISE NOTICE 'Total Employees Seeded: %', (SELECT COUNT(*) FROM "Employees" WHERE "EmployeeCode" LIKE 'EMP%');
    RAISE NOTICE 'Total Departments: %', (SELECT COUNT(*) FROM "Departments");
    RAISE NOTICE '===========================================';
END $$;
