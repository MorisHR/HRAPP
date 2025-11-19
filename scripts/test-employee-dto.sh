#!/bin/bash
# Test that sample employees have all required fields for frontend

echo "================================================================"
echo "Testing Employee Data Format for Frontend Compatibility"
echo "================================================================"
echo ""

PGPASSWORD=postgres psql -h localhost -U postgres -d hrms_master << 'EOSQL'
-- Test query to simulate what the frontend will receive
SELECT
    e."Id" as id,
    e."EmployeeCode" as "employeeCode",
    e."FirstName" as "firstName",
    e."LastName" as "lastName",
    e."Email" as email,
    e."PhoneNumber" as phone,
    d."Name" as department,
    e."Designation" as designation,
    e."EmploymentStatus" as status,
    e."JoiningDate" as "joiningDate"
FROM tenant_siraaj."Employees" e
LEFT JOIN tenant_siraaj."Departments" d ON e."DepartmentId" = d."Id"
WHERE e."IsDeleted" = false
ORDER BY e."EmployeeCode"
LIMIT 5;

-- Count by status
SELECT
    '"EmploymentStatus"' as field,
    "EmploymentStatus" as value,
    COUNT(*) as count
FROM tenant_siraaj."Employees"
WHERE "IsDeleted" = false
GROUP BY "EmploymentStatus"
ORDER BY count DESC;
EOSQL

echo ""
echo "================================================================"
echo "✅ If you see firstName, lastName, department, designation, and"
echo "   status fields above, the data format is correct!"
echo "================================================================"
