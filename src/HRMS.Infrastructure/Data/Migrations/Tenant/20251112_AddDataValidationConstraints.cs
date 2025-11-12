using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddDataValidationConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // EMPLOYEE TABLE CONSTRAINTS
            // =====================================================

            // Ensure password hash is valid when present
            // Password hashes must be at least 32 characters (common for bcrypt/argon2)
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Employees""
                  ADD CONSTRAINT ""chk_Employees_PasswordHash_Length""
                  CHECK (""PasswordHash"" IS NULL OR LENGTH(""PasswordHash"") >= 32)");

            // Ensure basic salary is never negative
            // Critical for payroll calculations and data integrity
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Employees""
                  ADD CONSTRAINT ""chk_Employees_BasicSalary_NonNegative""
                  CHECK (""BasicSalary"" >= 0)");

            // Ensure annual leave balance is not negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Employees""
                  ADD CONSTRAINT ""chk_Employees_AnnualLeaveBalance_NonNegative""
                  CHECK (""AnnualLeaveBalance"" >= 0)");

            // Ensure sick leave balance is not negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Employees""
                  ADD CONSTRAINT ""chk_Employees_SickLeaveBalance_NonNegative""
                  CHECK (""SickLeaveBalance"" >= 0)");

            // Ensure casual leave balance is not negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Employees""
                  ADD CONSTRAINT ""chk_Employees_CasualLeaveBalance_NonNegative""
                  CHECK (""CasualLeaveBalance"" >= 0)");

            // =====================================================
            // LEAVE BALANCE TABLE CONSTRAINTS
            // =====================================================

            // Ensure leave days are never negative
            // Critical for leave management accuracy
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""LeaveBalances""
                  ADD CONSTRAINT ""chk_LeaveBalances_Days_NonNegative""
                  CHECK (""TotalEntitlement"" >= 0 AND ""UsedDays"" >= 0 AND ""PendingDays"" >= 0)");

            // Ensure carried forward and accrued days are non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""LeaveBalances""
                  ADD CONSTRAINT ""chk_LeaveBalances_Accrual_NonNegative""
                  CHECK (""CarriedForward"" >= 0 AND ""Accrued"" >= 0)");

            // =====================================================
            // ATTENDANCE TABLE CONSTRAINTS
            // =====================================================

            // Ensure working hours are non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Attendances""
                  ADD CONSTRAINT ""chk_Attendances_WorkingHours_NonNegative""
                  CHECK (""WorkingHours"" >= 0)");

            // Ensure overtime hours are non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Attendances""
                  ADD CONSTRAINT ""chk_Attendances_OvertimeHours_NonNegative""
                  CHECK (""OvertimeHours"" >= 0)");

            // Ensure overtime rate is non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Attendances""
                  ADD CONSTRAINT ""chk_Attendances_OvertimeRate_NonNegative""
                  CHECK (""OvertimeRate"" >= 0)");

            // =====================================================
            // PAYROLL CYCLE TABLE CONSTRAINTS
            // =====================================================

            // Ensure salary amounts are non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""PayrollCycles""
                  ADD CONSTRAINT ""chk_PayrollCycles_Salary_NonNegative""
                  CHECK (""TotalGrossSalary"" >= 0 AND ""TotalDeductions"" >= 0 AND ""TotalNetSalary"" >= 0)");

            // Ensure month is valid (1-12)
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""PayrollCycles""
                  ADD CONSTRAINT ""chk_PayrollCycles_Month_Valid""
                  CHECK (""Month"" >= 1 AND ""Month"" <= 12)");

            // Ensure year is reasonable (greater than 1900)
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""PayrollCycles""
                  ADD CONSTRAINT ""chk_PayrollCycles_Year_Valid""
                  CHECK (""Year"" > 1900)");

            // =====================================================
            // PAYSLIP TABLE CONSTRAINTS
            // =====================================================

            // Ensure salary components are non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Payslips""
                  ADD CONSTRAINT ""chk_Payslips_Salary_NonNegative""
                  CHECK (""BasicSalary"" >= 0 AND ""TotalGrossSalary"" >= 0 AND ""TotalDeductions"" >= 0)");

            // Ensure overtime components are non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Payslips""
                  ADD CONSTRAINT ""chk_Payslips_Overtime_NonNegative""
                  CHECK (""OvertimeHours"" >= 0 AND ""OvertimePay"" >= 0)");

            // Ensure leave days are non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Payslips""
                  ADD CONSTRAINT ""chk_Payslips_LeaveDays_NonNegative""
                  CHECK (""PaidLeaveDays"" >= 0 AND ""UnpaidLeaveDays"" >= 0)");

            // Ensure allowances are non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Payslips""
                  ADD CONSTRAINT ""chk_Payslips_Allowances_NonNegative""
                  CHECK (""HousingAllowance"" >= 0 AND ""TransportAllowance"" >= 0
                         AND ""MealAllowance"" >= 0 AND ""MobileAllowance"" >= 0
                         AND ""OtherAllowances"" >= 0)");

            // =====================================================
            // TIMESHEET TABLE CONSTRAINTS
            // =====================================================

            // Ensure timesheet hours are non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Timesheets""
                  ADD CONSTRAINT ""chk_Timesheets_Hours_NonNegative""
                  CHECK (""TotalRegularHours"" >= 0 AND ""TotalOvertimeHours"" >= 0
                         AND ""TotalHolidayHours"" >= 0 AND ""TotalSickLeaveHours"" >= 0
                         AND ""TotalAnnualLeaveHours"" >= 0 AND ""TotalAbsentHours"" >= 0)");

            // =====================================================
            // TIMESHEET ENTRY CONSTRAINTS
            // =====================================================

            // Ensure timesheet entry hours are non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""TimesheetEntries""
                  ADD CONSTRAINT ""chk_TimesheetEntries_Hours_NonNegative""
                  CHECK (""ActualHours"" >= 0 AND ""RegularHours"" >= 0 AND ""OvertimeHours"" >= 0
                         AND ""HolidayHours"" >= 0 AND ""SickLeaveHours"" >= 0
                         AND ""AnnualLeaveHours"" >= 0)");

            // =====================================================
            // SALARY COMPONENT CONSTRAINTS
            // =====================================================

            // Ensure salary component amount is non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""SalaryComponents""
                  ADD CONSTRAINT ""chk_SalaryComponents_Amount_NonNegative""
                  CHECK (""Amount"" >= 0)");

            // =====================================================
            // LEAVE ENCASHMENT CONSTRAINTS
            // =====================================================

            // Ensure encashment amounts are non-negative
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""LeaveEncashments""
                  ADD CONSTRAINT ""chk_LeaveEncashments_Days_NonNegative""
                  CHECK (""UnusedAnnualLeaveDays"" >= 0 AND ""UnusedSickLeaveDays"" >= 0
                         AND ""TotalEncashableDays"" >= 0)");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""LeaveEncashments""
                  ADD CONSTRAINT ""chk_LeaveEncashments_Amount_NonNegative""
                  CHECK (""DailySalary"" >= 0 AND ""TotalEncashmentAmount"" >= 0)");

            // =====================================================
            // ATTENDANCE ANOMALY CONSTRAINTS
            // =====================================================

            // Ensure anomaly details are not empty when present
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""AttendanceAnomalies""
                  ADD CONSTRAINT ""chk_AttendanceAnomalies_Description_NotEmpty""
                  CHECK (""AnomalyDescription"" IS NULL OR LENGTH(TRIM(""AnomalyDescription"")) > 0)");

            // =====================================================
            // DEVICE API KEY CONSTRAINTS
            // =====================================================

            // Ensure API key hash is at least 64 characters (SHA256)
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""DeviceApiKeys""
                  ADD CONSTRAINT ""chk_DeviceApiKeys_Hash_Length""
                  CHECK (LENGTH(""ApiKeyHash"") >= 64)");

            // Ensure description is not empty
            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""DeviceApiKeys""
                  ADD CONSTRAINT ""chk_DeviceApiKeys_Description_NotEmpty""
                  CHECK (LENGTH(TRIM(""Description"")) > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // DROP EMPLOYEE TABLE CONSTRAINTS
            // =====================================================

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Employees""
                  DROP CONSTRAINT ""chk_Employees_PasswordHash_Length""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Employees""
                  DROP CONSTRAINT ""chk_Employees_BasicSalary_NonNegative""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Employees""
                  DROP CONSTRAINT ""chk_Employees_AnnualLeaveBalance_NonNegative""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Employees""
                  DROP CONSTRAINT ""chk_Employees_SickLeaveBalance_NonNegative""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Employees""
                  DROP CONSTRAINT ""chk_Employees_CasualLeaveBalance_NonNegative""");

            // =====================================================
            // DROP LEAVE BALANCE TABLE CONSTRAINTS
            // =====================================================

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""LeaveBalances""
                  DROP CONSTRAINT ""chk_LeaveBalances_Days_NonNegative""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""LeaveBalances""
                  DROP CONSTRAINT ""chk_LeaveBalances_Accrual_NonNegative""");

            // =====================================================
            // DROP ATTENDANCE TABLE CONSTRAINTS
            // =====================================================

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Attendances""
                  DROP CONSTRAINT ""chk_Attendances_WorkingHours_NonNegative""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Attendances""
                  DROP CONSTRAINT ""chk_Attendances_OvertimeHours_NonNegative""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Attendances""
                  DROP CONSTRAINT ""chk_Attendances_OvertimeRate_NonNegative""");

            // =====================================================
            // DROP PAYROLL CYCLE TABLE CONSTRAINTS
            // =====================================================

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""PayrollCycles""
                  DROP CONSTRAINT ""chk_PayrollCycles_Salary_NonNegative""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""PayrollCycles""
                  DROP CONSTRAINT ""chk_PayrollCycles_Month_Valid""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""PayrollCycles""
                  DROP CONSTRAINT ""chk_PayrollCycles_Year_Valid""");

            // =====================================================
            // DROP PAYSLIP TABLE CONSTRAINTS
            // =====================================================

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Payslips""
                  DROP CONSTRAINT ""chk_Payslips_Salary_NonNegative""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Payslips""
                  DROP CONSTRAINT ""chk_Payslips_Overtime_NonNegative""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Payslips""
                  DROP CONSTRAINT ""chk_Payslips_LeaveDays_NonNegative""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Payslips""
                  DROP CONSTRAINT ""chk_Payslips_Allowances_NonNegative""");

            // =====================================================
            // DROP TIMESHEET TABLE CONSTRAINTS
            // =====================================================

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""Timesheets""
                  DROP CONSTRAINT ""chk_Timesheets_Hours_NonNegative""");

            // =====================================================
            // DROP TIMESHEET ENTRY CONSTRAINTS
            // =====================================================

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""TimesheetEntries""
                  DROP CONSTRAINT ""chk_TimesheetEntries_Hours_NonNegative""");

            // =====================================================
            // DROP SALARY COMPONENT CONSTRAINTS
            // =====================================================

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""SalaryComponents""
                  DROP CONSTRAINT ""chk_SalaryComponents_Amount_NonNegative""");

            // =====================================================
            // DROP LEAVE ENCASHMENT CONSTRAINTS
            // =====================================================

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""LeaveEncashments""
                  DROP CONSTRAINT ""chk_LeaveEncashments_Days_NonNegative""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""LeaveEncashments""
                  DROP CONSTRAINT ""chk_LeaveEncashments_Amount_NonNegative""");

            // =====================================================
            // DROP ATTENDANCE ANOMALY CONSTRAINTS
            // =====================================================

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""AttendanceAnomalies""
                  DROP CONSTRAINT ""chk_AttendanceAnomalies_Description_NotEmpty""");

            // =====================================================
            // DROP DEVICE API KEY CONSTRAINTS
            // =====================================================

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""DeviceApiKeys""
                  DROP CONSTRAINT ""chk_DeviceApiKeys_Hash_Length""");

            migrationBuilder.Sql(
                @"ALTER TABLE ""tenant_default"".""DeviceApiKeys""
                  DROP CONSTRAINT ""chk_DeviceApiKeys_Description_NotEmpty""");
        }
    }
}
