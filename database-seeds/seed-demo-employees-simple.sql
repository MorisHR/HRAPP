-- =========================================================================
-- DEMO EMPLOYEE SEED DATA - SIMPLIFIED
-- Fortune 500-grade sample data for HRMS demo
-- Run this script against tenant_siraaj schema
-- =========================================================================

SET search_path TO tenant_siraaj;

-- Note: Using existing department IDs from the database
-- IT Department: db9d19e8-f042-40f7-bfe6-859845dbcb4e
-- Finance Department: b463f778-7a08-4107-b3d8-4f389dfd4896
-- Human Resources Department: b97c5799-a5bc-457a-967c-4fe1f842b037

-- Insert 15 demo employees with all required fields
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
    "IsOffboarded",
    "IsAdmin",
    "IsTwoFactorEnabled",
    "LockoutEnabled",
    "MustChangePassword",
    "CarryForwardAllowed",
    "CreatedAt",
    "UpdatedAt",
    "IsDeleted"
)
VALUES
('e0000001-0000-0000-0000-000000000001', 'EMP001', 'Sarah', '', 'Johnson', 'sarah.johnson@company.com', '+230-5234-1001', '1988-05-15', 1, 1, '123 Tech Street', 'Mauritius', 'Mauritius', 0, 0, true, false, 'Senior Software Engineer', 'Senior Software Engineer', 'db9d19e8-f042-40f7-bfe6-859845dbcb4e', '2021-03-15', 'Permanent', 'Active', 75000.00, 'Monthly', 'MUR', 20, 15, 5, 20.0, 15.0, 5.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000002-0000-0000-0000-000000000002', 'EMP002', 'Michael', '', 'Chen', 'michael.chen@company.com', '+230-5234-1002', '1985-08-22', 0, 2, '456 Sales Avenue', 'Mauritius', 'Mauritius', 0, 0, true, false, 'Sales Director', 'Sales Director', 'b97c5799-a5bc-457a-967c-4fe1f842b037', '2020-01-10', 'Permanent', 'Active', 95000.00, 'Monthly', 'MUR', 22, 15, 5, 22.0, 15.0, 5.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000003-0000-0000-0000-000000000003', 'EMP003', 'Emily', '', 'Rodriguez', 'emily.rodriguez@company.com', '+230-5234-1003', '1990-03-10', 1, 2, '789 HR Boulevard', 'Mauritius', 'Mauritius', 0, 0, true, false, 'HR Manager', 'HR Manager', 'b97c5799-a5bc-457a-967c-4fe1f842b037', '2019-08-22', 'Permanent', 'Active', 72000.00, 'Monthly', 'MUR', 20, 15, 5, 20.0, 15.0, 5.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000004-0000-0000-0000-000000000004', 'EMP004', 'David', '', 'Thompson', 'david.thompson@company.com', '+230-5234-1004', '1992-11-05', 0, 1, '321 Finance Road', 'Mauritius', 'Mauritius', 0, 0, true, false, 'Financial Analyst', 'Financial Analyst', 'b463f778-7a08-4107-b3d8-4f389dfd4896', '2022-05-01', 'Permanent', 'Active', 62000.00, 'Monthly', 'MUR', 20, 15, 5, 20.0, 15.0, 5.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000005-0000-0000-0000-000000000005', 'EMP005', 'Jessica', '', 'Martinez', 'jessica.martinez@company.com', '+230-5234-1005', '1991-07-18', 1, 1, '654 DevOps Lane', 'Mauritius', 'Mauritius', 0, 0, true, false, 'DevOps Engineer', 'DevOps Engineer', 'db9d19e8-f042-40f7-bfe6-859845dbcb4e', '2021-11-15', 'Permanent', 'Active', 68000.00, 'Monthly', 'MUR', 20, 15, 5, 18.0, 13.0, 4.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000006-0000-0000-0000-000000000006', 'EMP006', 'Robert', '', 'Anderson', 'robert.anderson@company.com', '+230-5234-1006', '1986-12-30', 0, 2, '987 Marketing Plaza', 'Mauritius', 'Mauritius', 0, 0, true, false, 'Marketing Manager', 'Marketing Manager', 'b97c5799-a5bc-457a-967c-4fe1f842b037', '2020-07-20', 'Permanent', 'Active', 78000.00, 'Monthly', 'MUR', 21, 15, 5, 21.0, 15.0, 5.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000007-0000-0000-0000-000000000007', 'EMP007', 'Amanda', '', 'Taylor', 'amanda.taylor@company.com', '+230-5234-1007', '1993-02-14', 1, 1, '147 Quality Street', 'Mauritius', 'Mauritius', 0, 0, true, false, 'QA Engineer', 'QA Engineer', 'db9d19e8-f042-40f7-bfe6-859845dbcb4e', '2022-02-14', 'Permanent', 'Active', 58000.00, 'Monthly', 'MUR', 20, 15, 5, 20.0, 15.0, 5.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000008-0000-0000-0000-000000000008', 'EMP008', 'Christopher', '', 'Wilson', 'christopher.wilson@company.com', '+230-5234-1008', '1994-09-25', 0, 1, '258 Sales Court', 'Mauritius', 'Mauritius', 0, 0, true, false, 'Account Executive', 'Account Executive', 'b97c5799-a5bc-457a-967c-4fe1f842b037', '2023-01-05', 'Permanent', 'Active', 55000.00, 'Monthly', 'MUR', 20, 15, 5, 20.0, 15.0, 5.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000009-0000-0000-0000-000000000009', 'EMP009', 'Jennifer', '', 'Brown', 'jennifer.brown@company.com', '+230-5234-1009', '1989-06-10', 1, 2, '369 Finance Center', 'Mauritius', 'Mauritius', 0, 0, true, false, 'Accountant', 'Accountant', 'b463f778-7a08-4107-b3d8-4f389dfd4896', '2021-06-10', 'Permanent', 'Active', 64000.00, 'Monthly', 'MUR', 20, 15, 5, 17.0, 12.0, 3.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000010-0000-0000-0000-000000000010', 'EMP010', 'Matthew', '', 'Davis', 'matthew.davis@company.com', '+230-5234-1010', '1987-04-18', 0, 2, '741 Product Drive', 'Mauritius', 'Mauritius', 0, 0, true, false, 'Product Manager', 'Product Manager', 'db9d19e8-f042-40f7-bfe6-859845dbcb4e', '2020-09-18', 'Permanent', 'Active', 85000.00, 'Monthly', 'MUR', 21, 15, 5, 21.0, 15.0, 5.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000011-0000-0000-0000-000000000011', 'EMP011', 'Lisa', '', 'Garcia', 'lisa.garcia@company.com', '+230-5234-1011', '1991-10-22', 1, 1, '852 Support Way', 'Mauritius', 'Mauritius', 0, 0, true, false, 'Support Team Lead', 'Support Team Lead', 'b97c5799-a5bc-457a-967c-4fe1f842b037', '2019-12-01', 'Permanent', 'Active', 67000.00, 'Monthly', 'MUR', 20, 15, 5, 20.0, 15.0, 5.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000012-0000-0000-0000-000000000012', 'EMP012', 'Daniel', '', 'Miller', 'daniel.miller@company.com', '+230-5234-1012', '1995-08-25', 0, 1, '963 Frontend Plaza', 'Mauritius', 'Mauritius', 0, 0, true, false, 'Frontend Developer', 'Frontend Developer', 'db9d19e8-f042-40f7-bfe6-859845dbcb4e', '2022-08-25', 'Permanent', 'Active', 61000.00, 'Monthly', 'MUR', 20, 15, 5, 20.0, 15.0, 5.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000013-0000-0000-0000-000000000013', 'EMP013', 'Michelle', '', 'Lee', 'michelle.lee@company.com', '+230-5234-1013', '1992-03-10', 1, 1, '159 Design Avenue', 'Mauritius', 'Mauritius', 0, 0, true, false, 'UX Designer', 'UX Designer', 'db9d19e8-f042-40f7-bfe6-859845dbcb4e', '2023-03-10', 'Permanent', 'Active', 63000.00, 'Monthly', 'MUR', 20, 15, 5, 20.0, 15.0, 5.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000014-0000-0000-0000-000000000014', 'EMP014', 'Kevin', '', 'White', 'kevin.white@company.com', '+230-5234-1014', '1988-11-20', 0, 2, '357 Business Boulevard', 'Mauritius', 'Mauritius', 0, 0, true, false, 'Business Development Manager', 'Business Development Manager', 'b97c5799-a5bc-457a-967c-4fe1f842b037', '2021-04-20', 'Permanent', 'Active', 82000.00, 'Monthly', 'MUR', 21, 15, 5, 19.0, 14.0, 4.0, true, false, false, false, false, false, true, NOW(), NOW(), false),
('e0000015-0000-0000-0000-000000000015', 'EMP015', 'Rachel', '', 'Harris', 'rachel.harris@company.com', '+230-5234-1015', '1993-11-08', 1, 1, '753 Backend Court', 'Mauritius', 'Mauritius', 0, 0, true, false, 'Backend Developer', 'Backend Developer', 'db9d19e8-f042-40f7-bfe6-859845dbcb4e', '2022-11-08', 'Permanent', 'Active', 66000.00, 'Monthly', 'MUR', 20, 15, 5, 20.0, 15.0, 5.0, true, false, false, false, false, false, true, NOW(), NOW(), false)
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
