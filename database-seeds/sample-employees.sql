-- ============================================
-- Sample Employees for Demo/Testing
-- ============================================
-- Creates 10 realistic employees across different departments
-- for demonstration and testing purposes
-- ============================================

DO $$
DECLARE
    v_tenant_id UUID;
    v_tenant_schema TEXT;
    v_dept_engineering UUID;
    v_dept_sales UUID;
    v_dept_hr UUID;
    v_dept_finance UUID;
    v_dept_marketing UUID;
    v_dept_operations UUID;
BEGIN
    -- Get first active tenant
    SELECT "Id", "SchemaName" INTO v_tenant_id, v_tenant_schema
    FROM master."Tenants"
    WHERE "IsDeleted" = false
    ORDER BY "CreatedAt"
    LIMIT 1;

    IF v_tenant_id IS NULL THEN
        RAISE EXCEPTION 'No active tenant found. Please create a tenant first.';
    END IF;

    RAISE NOTICE 'Using tenant: % (schema: %)', v_tenant_id, v_tenant_schema;

    -- Create departments if they don't exist
    EXECUTE format('
        INSERT INTO %I."Departments" ("Id", "Name", "Code", "Description", "IsActive", "IsDeleted", "CreatedAt")
        VALUES
            ($1, ''Engineering'', ''ENG'', ''Product Engineering Team'', true, false, NOW()),
            ($2, ''Sales'', ''SAL'', ''Sales and Business Development'', true, false, NOW()),
            ($3, ''Human Resources'', ''HR'', ''Human Resources Department'', true, false, NOW()),
            ($4, ''Finance'', ''FIN'', ''Finance and Accounting'', true, false, NOW()),
            ($5, ''Marketing'', ''MKT'', ''Marketing and Communications'', true, false, NOW()),
            ($6, ''Operations'', ''OPS'', ''Operations and Support'', true, false, NOW())
        ON CONFLICT ("Code") DO NOTHING
    ', v_tenant_schema)
    USING
        gen_random_uuid(),
        gen_random_uuid(),
        gen_random_uuid(),
        gen_random_uuid(),
        gen_random_uuid(),
        gen_random_uuid();

    -- Get department IDs
    EXECUTE format('SELECT "Id" FROM %I."Departments" WHERE "Code" = ''ENG''', v_tenant_schema)
    INTO v_dept_engineering;

    EXECUTE format('SELECT "Id" FROM %I."Departments" WHERE "Code" = ''SAL''', v_tenant_schema)
    INTO v_dept_sales;

    EXECUTE format('SELECT "Id" FROM %I."Departments" WHERE "Code" = ''HR''', v_tenant_schema)
    INTO v_dept_hr;

    EXECUTE format('SELECT "Id" FROM %I."Departments" WHERE "Code" = ''FIN''', v_tenant_schema)
    INTO v_dept_finance;

    EXECUTE format('SELECT "Id" FROM %I."Departments" WHERE "Code" = ''MKT''', v_tenant_schema)
    INTO v_dept_marketing;

    EXECUTE format('SELECT "Id" FROM %I."Departments" WHERE "Code" = ''OPS''', v_tenant_schema)
    INTO v_dept_operations;

    -- Insert sample employees
    EXECUTE format('
        INSERT INTO %I."Employees" (
            "Id", "EmployeeCode", "FirstName", "LastName", "MiddleName",
            "Email", "PhoneNumber", "DateOfBirth", "Gender", "MaritalStatus",
            "AddressLine1", "JoiningDate", "DepartmentId", "JobTitle", "Designation",
            "EmploymentContractType", "EmployeeType", "TaxResidentStatus",
            "EmploymentStatus", "PasswordHash", "IsActive", "IsDeleted",
            "CreatedAt", "BasicSalary", "SalaryCurrency", "Nationality",
            "AnnualLeaveBalance", "SickLeaveBalance", "CasualLeaveBalance",
            "AccessFailedCount", "LockoutEnabled", "IsNPFEligible", "IsNSFEligible",
            "Country", "IsAdmin", "MustChangePassword", "IsTwoFactorEnabled",
            "IsOffboarded", "IndustrySector", "PaymentFrequency"
        ) VALUES
        -- Engineering Team
        (
            gen_random_uuid(), ''EMP001'', ''Sarah'', ''Johnson'', '''',
            ''sarah.johnson@company.com'', ''+230-5234-5678'', ''1990-03-15'', 1, 1,
            ''123 Main Street, Port Louis'', ''2022-01-10'', $1, ''Senior Software Engineer'', ''Senior Engineer'',
            ''Permanent'', 1, 0,
            ''Active'', ''$2a$11$dummy.hash.for.Password123'', true, false,
            NOW(), 85000, ''MUR'', ''Mauritian'',
            20, 15, 5,
            0, false, true, true,
            ''Mauritius'', false, false, false,
            false, ''Technology'', ''Monthly''
        ),
        (
            gen_random_uuid(), ''EMP002'', ''Michael'', ''Chen'', '''',
            ''michael.chen@company.com'', ''+230-5234-5679'', ''1988-07-22'', 0, 1,
            ''456 Oak Avenue, Curepipe'', ''2021-06-15'', $1, ''Tech Lead'', ''Lead Engineer'',
            ''Permanent'', 1, 0,
            ''Active'', ''$2a$11$dummy.hash.for.Password123'', true, false,
            NOW(), 95000, ''MUR'', ''Mauritian'',
            20, 15, 5,
            0, false, true, true,
            ''Mauritius'', false, false, false,
            false, ''Technology'', ''Monthly''
        ),
        (
            gen_random_uuid(), ''EMP003'', ''Priya'', ''Sharma'', '''',
            ''priya.sharma@company.com'', ''+230-5234-5680'', ''1992-11-08'', 1, 0,
            ''789 Pine Road, Quatre Bornes'', ''2023-02-20'', $1, ''Software Engineer'', ''Engineer'',
            ''Permanent'', 1, 0,
            ''Active'', ''$2a$11$dummy.hash.for.Password123'', true, false,
            NOW(), 65000, ''MUR'', ''Mauritian'',
            20, 15, 5,
            0, false, true, true,
            ''Mauritius'', false, false, false,
            false, ''Technology'', ''Monthly''
        ),

        -- Sales Team
        (
            gen_random_uuid(), ''EMP004'', ''David'', ''Williams'', '''',
            ''david.williams@company.com'', ''+230-5234-5681'', ''1985-05-12'', 0, 1,
            ''321 Beach Road, Flic-en-Flac'', ''2020-03-01'', $2, ''Sales Manager'', ''Manager'',
            ''Permanent'', 1, 0,
            ''Active'', ''$2a$11$dummy.hash.for.Password123'', true, false,
            NOW(), 75000, ''MUR'', ''Mauritian'',
            20, 15, 5,
            0, false, true, true,
            ''Mauritius'', false, false, false,
            false, ''Sales'', ''Monthly''
        ),
        (
            gen_random_uuid(), ''EMP005'', ''Emma'', ''Davis'', '''',
            ''emma.davis@company.com'', ''+230-5234-5682'', ''1993-09-18'', 1, 0,
            ''654 Palm Street, Grand Baie'', ''2022-08-15'', $2, ''Sales Executive'', ''Executive'',
            ''Permanent'', 1, 0,
            ''OnLeave'', ''$2a$11$dummy.hash.for.Password123'', true, false,
            NOW(), 55000, ''MUR'', ''Mauritian'',
            20, 15, 5,
            0, false, true, true,
            ''Mauritius'', false, false, false,
            false, ''Sales'', ''Monthly''
        ),

        -- HR Team
        (
            gen_random_uuid(), ''EMP006'', ''Lisa'', ''Anderson'', '''',
            ''lisa.anderson@company.com'', ''+230-5234-5683'', ''1987-12-25'', 1, 1,
            ''987 Rose Hill Street'', ''2019-11-20'', $3, ''HR Manager'', ''Manager'',
            ''Permanent'', 1, 0,
            ''Active'', ''$2a$11$dummy.hash.for.Password123'', true, false,
            NOW(), 70000, ''MUR'', ''Mauritian'',
            20, 15, 5,
            0, false, true, true,
            ''Mauritius'', false, false, false,
            false, ''Human Resources'', ''Monthly''
        ),

        -- Finance Team
        (
            gen_random_uuid(), ''EMP007'', ''James'', ''Taylor'', '''',
            ''james.taylor@company.com'', ''+230-5234-5684'', ''1989-04-30'', 0, 1,
            ''234 Finance Plaza, Ebene'', ''2021-01-15'', $4, ''Financial Analyst'', ''Analyst'',
            ''Permanent'', 1, 0,
            ''Active'', ''$2a$11$dummy.hash.for.Password123'', true, false,
            NOW(), 68000, ''MUR'', ''Mauritian'',
            20, 15, 5,
            0, false, true, true,
            ''Mauritius'', false, false, false,
            false, ''Finance'', ''Monthly''
        ),
        (
            gen_random_uuid(), ''EMP008'', ''Sophie'', ''Martin'', '''',
            ''sophie.martin@company.com'', ''+230-5234-5685'', ''1991-08-14'', 1, 0,
            ''567 Cyber City, Ebene'', ''2022-05-10'', $4, ''Accountant'', ''Accountant'',
            ''Permanent'', 1, 0,
            ''Active'', ''$2a$11$dummy.hash.for.Password123'', true, false,
            NOW(), 62000, ''MUR'', ''Mauritian'',
            20, 15, 5,
            0, false, true, true,
            ''Mauritius'', false, false, false,
            false, ''Finance'', ''Monthly''
        ),

        -- Marketing Team
        (
            gen_random_uuid(), ''EMP009'', ''Alex'', ''Brown'', '''',
            ''alex.brown@company.com'', ''+230-5234-5686'', ''1994-02-28'', 0, 0,
            ''890 Marketing Hub, Moka'', ''2023-01-05'', $5, ''Marketing Specialist'', ''Specialist'',
            ''Permanent'', 1, 0,
            ''Active'', ''$2a$11$dummy.hash.for.Password123'', true, false,
            NOW(), 58000, ''MUR'', ''Mauritian'',
            20, 15, 5,
            0, false, true, true,
            ''Mauritius'', false, false, false,
            false, ''Marketing'', ''Monthly''
        ),

        -- Operations Team (Suspended)
        (
            gen_random_uuid(), ''EMP010'', ''Nina'', ''Patel'', '''',
            ''nina.patel@company.com'', ''+230-5234-5687'', ''1986-10-05'', 1, 1,
            ''111 Operations Center, Vacoas'', ''2020-07-01'', $6, ''Operations Coordinator'', ''Coordinator'',
            ''Contract'', 2, 0,
            ''Suspended'', ''$2a$11$dummy.hash.for.Password123'', false, false,
            NOW(), 52000, ''MUR'', ''Mauritian'',
            20, 15, 5,
            0, false, true, true,
            ''Mauritius'', false, false, false,
            false, ''Operations'', ''Monthly''
        )
        ON CONFLICT ("EmployeeCode") DO NOTHING
    ', v_tenant_schema)
    USING
        v_dept_engineering,
        v_dept_sales,
        v_dept_hr,
        v_dept_finance,
        v_dept_marketing,
        v_dept_operations;

    RAISE NOTICE 'Sample employees created successfully in schema: %', v_tenant_schema;
    RAISE NOTICE 'Employee Codes: EMP001 - EMP010';
    RAISE NOTICE 'Employment Status breakdown:';
    RAISE NOTICE '  - Active: 8 employees';
    RAISE NOTICE '  - OnLeave: 1 employee (EMP005 - Emma Davis)';
    RAISE NOTICE '  - Suspended: 1 employee (EMP010 - Nina Patel)';
    RAISE NOTICE '';
    RAISE NOTICE 'Departments created: Engineering, Sales, HR, Finance, Marketing, Operations';

END $$;
