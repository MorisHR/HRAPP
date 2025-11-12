-- ===============================================================================
-- DATABASE CONSTRAINTS AND INDEXES - MANUAL EXECUTION SCRIPT
-- ===============================================================================
-- PostgreSQL Database
-- Schema: tenant_default (and tenant-specific schemas)
-- Purpose: Add unique constraints, composite indexes, and data validation rules
-- Date: November 12, 2025
-- ===============================================================================

-- ===============================================================================
-- PART 1: UNIQUE CONSTRAINTS ON CRITICAL IDENTIFICATION DOCUMENTS
-- ===============================================================================

-- CRITICAL: Prevent duplicate National IDs (Mauritius compliance)
CREATE UNIQUE INDEX CONCURRENTLY "IX_Employees_NationalIdCard_Unique"
    ON tenant_default."Employees"("NationalIdCard")
    WHERE "NationalIdCard" IS NOT NULL AND "IsDeleted" = false;

-- Prevent duplicate Passport numbers (expatriate tracking)
CREATE UNIQUE INDEX CONCURRENTLY "IX_Employees_PassportNumber_Unique"
    ON tenant_default."Employees"("PassportNumber")
    WHERE "PassportNumber" IS NOT NULL AND "IsDeleted" = false;

-- Prevent duplicate Tax IDs
CREATE UNIQUE INDEX CONCURRENTLY "IX_Employees_TaxIdNumber_Unique"
    ON tenant_default."Employees"("TaxIdNumber")
    WHERE "TaxIdNumber" IS NOT NULL AND "IsDeleted" = false;

-- Prevent duplicate NPF numbers (Mauritius)
CREATE UNIQUE INDEX CONCURRENTLY "IX_Employees_NPFNumber_Unique"
    ON tenant_default."Employees"("NPFNumber")
    WHERE "NPFNumber" IS NOT NULL AND "IsDeleted" = false;

-- Prevent duplicate NSF numbers (Mauritius)
CREATE UNIQUE INDEX CONCURRENTLY "IX_Employees_NSFNumber_Unique"
    ON tenant_default."Employees"("NSFNumber")
    WHERE "NSFNumber" IS NOT NULL AND "IsDeleted" = false;

-- Prevent duplicate Bank Account numbers
CREATE UNIQUE INDEX CONCURRENTLY "IX_Employees_BankAccountNumber_Unique"
    ON tenant_default."Employees"("BankAccountNumber")
    WHERE "BankAccountNumber" IS NOT NULL AND "IsDeleted" = false;

-- ===============================================================================
-- PART 2: PAYROLL CYCLE OPTIMIZATION INDEXES
-- ===============================================================================

-- Optimize monthly payroll report queries
CREATE INDEX CONCURRENTLY "IX_PayrollCycles_Year_Month"
    ON tenant_default."PayrollCycles"("Year" DESC, "Month" DESC)
    WHERE "IsDeleted" = false;

-- Optimize payroll status and payment date filtering
CREATE INDEX CONCURRENTLY "IX_PayrollCycles_Status_PaymentDate"
    ON tenant_default."PayrollCycles"("Status", "PaymentDate" DESC)
    WHERE "IsDeleted" = false;

-- ===============================================================================
-- PART 3: LEAVE BALANCE OPTIMIZATION
-- ===============================================================================

-- Optimize employee leave balance queries by year and type
CREATE INDEX CONCURRENTLY "IX_LeaveBalances_EmployeeId_Year_LeaveTypeId"
    ON tenant_default."LeaveBalances"("EmployeeId", "Year" DESC, "LeaveTypeId")
    WHERE "IsDeleted" = false;

-- ===============================================================================
-- PART 4: ATTENDANCE MONTHLY QUERY OPTIMIZATION
-- ===============================================================================

-- Optimize monthly attendance reports and employee attendance checks
CREATE INDEX CONCURRENTLY "IX_Attendances_EmployeeId_Date_Status"
    ON tenant_default."Attendances"("EmployeeId", "Date" DESC, "Status")
    WHERE "IsDeleted" = false;

-- Optimize device-based attendance lookups
CREATE INDEX CONCURRENTLY "IX_Attendances_DeviceId_Date"
    ON tenant_default."Attendances"("DeviceId", "Date" DESC)
    WHERE "IsDeleted" = false;

-- ===============================================================================
-- PART 5: TIMESHEET APPROVAL WORKFLOW OPTIMIZATION
-- ===============================================================================

-- Optimize timesheet approval status filtering
CREATE INDEX CONCURRENTLY "IX_Timesheets_Status_PeriodStart"
    ON tenant_default."Timesheets"("Status", "PeriodStart" DESC)
    WHERE "IsDeleted" = false;

-- Optimize employee timesheet lookups during approval
CREATE INDEX CONCURRENTLY "IX_Timesheets_EmployeeId_Status_PeriodStart"
    ON tenant_default."Timesheets"("EmployeeId", "Status", "PeriodStart" DESC)
    WHERE "IsDeleted" = false;

-- ===============================================================================
-- PART 6: EMPLOYEE SEARCH OPTIMIZATION
-- ===============================================================================

-- Optimize employee directory searches by name
CREATE INDEX CONCURRENTLY "IX_Employees_FirstName_LastName_IsActive"
    ON tenant_default."Employees"("FirstName", "LastName")
    WHERE "IsActive" = true AND "IsDeleted" = false;

-- ===============================================================================
-- PART 7: TIMESHEET ENTRY OPTIMIZATION
-- ===============================================================================

-- Optimize timesheet entry filtering by date
CREATE INDEX CONCURRENTLY "IX_TimesheetEntries_TimesheetId_Date"
    ON tenant_default."TimesheetEntries"("TimesheetId", "Date" DESC)
    WHERE "IsDeleted" = false;

-- ===============================================================================
-- PART 8: LEAVE APPLICATION WORKFLOW OPTIMIZATION
-- ===============================================================================

-- Optimize leave application searches by date range
CREATE INDEX CONCURRENTLY "IX_LeaveApplications_EmployeeId_StartDate_EndDate"
    ON tenant_default."LeaveApplications"("EmployeeId", "StartDate" DESC, "EndDate" DESC)
    WHERE "IsDeleted" = false;

-- ===============================================================================
-- PART 9: BIOMETRIC PUNCH RECORD OPTIMIZATION (Fortune 500 Performance)
-- ===============================================================================

-- Optimize daily punch processing queries
CREATE INDEX CONCURRENTLY "IX_BiometricPunchRecords_ProcessingStatus_PunchTime"
    ON tenant_default."BiometricPunchRecords"("ProcessingStatus", "PunchTime" DESC)
    WHERE "IsDeleted" = false;

-- Optimize employee punch history queries
CREATE INDEX CONCURRENTLY "IX_BiometricPunchRecords_EmployeeId_PunchTime"
    ON tenant_default."BiometricPunchRecords"("EmployeeId", "PunchTime" DESC)
    WHERE "IsDeleted" = false;

-- Optimize device sync queries
CREATE INDEX CONCURRENTLY "IX_BiometricPunchRecords_DeviceId_PunchTime"
    ON tenant_default."BiometricPunchRecords"("DeviceId", "PunchTime" DESC)
    WHERE "IsDeleted" = false;

-- ===============================================================================
-- PART 10: DATA VALIDATION CHECK CONSTRAINTS
-- ===============================================================================

-- ===================== EMPLOYEE TABLE CONSTRAINTS =====================

-- Ensure password hash is valid when present
ALTER TABLE tenant_default."Employees"
    ADD CONSTRAINT "chk_Employees_PasswordHash_Length"
    CHECK ("PasswordHash" IS NULL OR LENGTH("PasswordHash") >= 32);

-- Ensure basic salary is never negative
ALTER TABLE tenant_default."Employees"
    ADD CONSTRAINT "chk_Employees_BasicSalary_NonNegative"
    CHECK ("BasicSalary" >= 0);

-- Ensure annual leave balance is not negative
ALTER TABLE tenant_default."Employees"
    ADD CONSTRAINT "chk_Employees_AnnualLeaveBalance_NonNegative"
    CHECK ("AnnualLeaveBalance" >= 0);

-- Ensure sick leave balance is not negative
ALTER TABLE tenant_default."Employees"
    ADD CONSTRAINT "chk_Employees_SickLeaveBalance_NonNegative"
    CHECK ("SickLeaveBalance" >= 0);

-- Ensure casual leave balance is not negative
ALTER TABLE tenant_default."Employees"
    ADD CONSTRAINT "chk_Employees_CasualLeaveBalance_NonNegative"
    CHECK ("CasualLeaveBalance" >= 0);

-- ===================== LEAVE BALANCE TABLE CONSTRAINTS =====================

-- Ensure leave days are never negative
ALTER TABLE tenant_default."LeaveBalances"
    ADD CONSTRAINT "chk_LeaveBalances_Days_NonNegative"
    CHECK ("TotalEntitlement" >= 0 AND "UsedDays" >= 0 AND "PendingDays" >= 0);

-- Ensure carried forward and accrued days are non-negative
ALTER TABLE tenant_default."LeaveBalances"
    ADD CONSTRAINT "chk_LeaveBalances_Accrual_NonNegative"
    CHECK ("CarriedForward" >= 0 AND "Accrued" >= 0);

-- ===================== ATTENDANCE TABLE CONSTRAINTS =====================

-- Ensure working hours are non-negative
ALTER TABLE tenant_default."Attendances"
    ADD CONSTRAINT "chk_Attendances_WorkingHours_NonNegative"
    CHECK ("WorkingHours" >= 0);

-- Ensure overtime hours are non-negative
ALTER TABLE tenant_default."Attendances"
    ADD CONSTRAINT "chk_Attendances_OvertimeHours_NonNegative"
    CHECK ("OvertimeHours" >= 0);

-- Ensure overtime rate is non-negative
ALTER TABLE tenant_default."Attendances"
    ADD CONSTRAINT "chk_Attendances_OvertimeRate_NonNegative"
    CHECK ("OvertimeRate" >= 0);

-- ===================== PAYROLL CYCLE TABLE CONSTRAINTS =====================

-- Ensure salary amounts are non-negative
ALTER TABLE tenant_default."PayrollCycles"
    ADD CONSTRAINT "chk_PayrollCycles_Salary_NonNegative"
    CHECK ("TotalGrossSalary" >= 0 AND "TotalDeductions" >= 0 AND "TotalNetSalary" >= 0);

-- Ensure month is valid (1-12)
ALTER TABLE tenant_default."PayrollCycles"
    ADD CONSTRAINT "chk_PayrollCycles_Month_Valid"
    CHECK ("Month" >= 1 AND "Month" <= 12);

-- Ensure year is reasonable (greater than 1900)
ALTER TABLE tenant_default."PayrollCycles"
    ADD CONSTRAINT "chk_PayrollCycles_Year_Valid"
    CHECK ("Year" > 1900);

-- ===================== PAYSLIP TABLE CONSTRAINTS =====================

-- Ensure salary components are non-negative
ALTER TABLE tenant_default."Payslips"
    ADD CONSTRAINT "chk_Payslips_Salary_NonNegative"
    CHECK ("BasicSalary" >= 0 AND "TotalGrossSalary" >= 0 AND "TotalDeductions" >= 0);

-- Ensure overtime components are non-negative
ALTER TABLE tenant_default."Payslips"
    ADD CONSTRAINT "chk_Payslips_Overtime_NonNegative"
    CHECK ("OvertimeHours" >= 0 AND "OvertimePay" >= 0);

-- Ensure leave days are non-negative
ALTER TABLE tenant_default."Payslips"
    ADD CONSTRAINT "chk_Payslips_LeaveDays_NonNegative"
    CHECK ("PaidLeaveDays" >= 0 AND "UnpaidLeaveDays" >= 0);

-- Ensure allowances are non-negative
ALTER TABLE tenant_default."Payslips"
    ADD CONSTRAINT "chk_Payslips_Allowances_NonNegative"
    CHECK ("HousingAllowance" >= 0 AND "TransportAllowance" >= 0 AND "MealAllowance" >= 0
           AND "MobileAllowance" >= 0 AND "OtherAllowances" >= 0);

-- ===================== TIMESHEET TABLE CONSTRAINTS =====================

-- Ensure timesheet hours are non-negative
ALTER TABLE tenant_default."Timesheets"
    ADD CONSTRAINT "chk_Timesheets_Hours_NonNegative"
    CHECK ("TotalRegularHours" >= 0 AND "TotalOvertimeHours" >= 0 AND "TotalHolidayHours" >= 0
           AND "TotalSickLeaveHours" >= 0 AND "TotalAnnualLeaveHours" >= 0 AND "TotalAbsentHours" >= 0);

-- ===================== TIMESHEET ENTRY CONSTRAINTS =====================

-- Ensure timesheet entry hours are non-negative
ALTER TABLE tenant_default."TimesheetEntries"
    ADD CONSTRAINT "chk_TimesheetEntries_Hours_NonNegative"
    CHECK ("ActualHours" >= 0 AND "RegularHours" >= 0 AND "OvertimeHours" >= 0
           AND "HolidayHours" >= 0 AND "SickLeaveHours" >= 0 AND "AnnualLeaveHours" >= 0);

-- ===================== SALARY COMPONENT CONSTRAINTS =====================

-- Ensure salary component amount is non-negative
ALTER TABLE tenant_default."SalaryComponents"
    ADD CONSTRAINT "chk_SalaryComponents_Amount_NonNegative"
    CHECK ("Amount" >= 0);

-- ===================== LEAVE ENCASHMENT CONSTRAINTS =====================

-- Ensure encashment amounts are non-negative
ALTER TABLE tenant_default."LeaveEncashments"
    ADD CONSTRAINT "chk_LeaveEncashments_Days_NonNegative"
    CHECK ("UnusedAnnualLeaveDays" >= 0 AND "UnusedSickLeaveDays" >= 0 AND "TotalEncashableDays" >= 0);

ALTER TABLE tenant_default."LeaveEncashments"
    ADD CONSTRAINT "chk_LeaveEncashments_Amount_NonNegative"
    CHECK ("DailySalary" >= 0 AND "TotalEncashmentAmount" >= 0);

-- ===================== ATTENDANCE ANOMALY CONSTRAINTS =====================

-- Ensure anomaly details are not empty when present
ALTER TABLE tenant_default."AttendanceAnomalies"
    ADD CONSTRAINT "chk_AttendanceAnomalies_Description_NotEmpty"
    CHECK ("AnomalyDescription" IS NULL OR LENGTH(TRIM("AnomalyDescription")) > 0);

-- ===================== DEVICE API KEY CONSTRAINTS =====================

-- Ensure API key hash is at least 64 characters (SHA256)
ALTER TABLE tenant_default."DeviceApiKeys"
    ADD CONSTRAINT "chk_DeviceApiKeys_Hash_Length"
    CHECK (LENGTH("ApiKeyHash") >= 64);

-- Ensure description is not empty
ALTER TABLE tenant_default."DeviceApiKeys"
    ADD CONSTRAINT "chk_DeviceApiKeys_Description_NotEmpty"
    CHECK (LENGTH(TRIM("Description")) > 0);

-- ===============================================================================
-- VERIFICATION QUERIES
-- ===============================================================================

-- Verify all indexes were created
SELECT schemaname, tablename, indexname, idx_scan
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_default' AND indexname LIKE 'IX_%'
ORDER BY indexname;

-- Verify all constraints were added
SELECT constraint_name, constraint_type, table_name
FROM information_schema.table_constraints
WHERE table_schema = 'tenant_default' AND constraint_type = 'CHECK'
ORDER BY constraint_name;

-- Check index size
SELECT schemaname, tablename, indexname, pg_size_pretty(pg_relation_size(indexrelid)) as size
FROM pg_stat_user_indexes
WHERE schemaname = 'tenant_default'
ORDER BY pg_relation_size(indexrelid) DESC;

-- ===============================================================================
-- PERFORMANCE ANALYSIS
-- ===============================================================================

-- After running migrations, analyze tables for query planning
ANALYZE tenant_default."Employees";
ANALYZE tenant_default."PayrollCycles";
ANALYZE tenant_default."LeaveBalances";
ANALYZE tenant_default."Attendances";
ANALYZE tenant_default."Timesheets";
ANALYZE tenant_default."BiometricPunchRecords";
ANALYZE tenant_default."Payslips";
ANALYZE tenant_default."LeaveApplications";

-- ===============================================================================
-- ROLLBACK SCRIPT (if needed)
-- ===============================================================================

-- To rollback, run the following commands:

-- Drop unique constraints
-- DROP INDEX IF EXISTS tenant_default."IX_Employees_NationalIdCard_Unique";
-- DROP INDEX IF EXISTS tenant_default."IX_Employees_PassportNumber_Unique";
-- DROP INDEX IF EXISTS tenant_default."IX_Employees_TaxIdNumber_Unique";
-- DROP INDEX IF EXISTS tenant_default."IX_Employees_NPFNumber_Unique";
-- DROP INDEX IF EXISTS tenant_default."IX_Employees_NSFNumber_Unique";
-- DROP INDEX IF EXISTS tenant_default."IX_Employees_BankAccountNumber_Unique";

-- Drop composite indexes
-- DROP INDEX IF EXISTS tenant_default."IX_PayrollCycles_Year_Month";
-- DROP INDEX IF EXISTS tenant_default."IX_PayrollCycles_Status_PaymentDate";
-- DROP INDEX IF EXISTS tenant_default."IX_LeaveBalances_EmployeeId_Year_LeaveTypeId";
-- ... (continue with other indexes)

-- Drop check constraints
-- ALTER TABLE tenant_default."Employees" DROP CONSTRAINT "chk_Employees_PasswordHash_Length";
-- ALTER TABLE tenant_default."Employees" DROP CONSTRAINT "chk_Employees_BasicSalary_NonNegative";
-- ... (continue with other constraints)

-- ===============================================================================
-- END OF SCRIPT
-- ===============================================================================
