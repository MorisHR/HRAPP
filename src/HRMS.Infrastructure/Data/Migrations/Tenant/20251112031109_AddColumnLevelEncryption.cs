using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddColumnLevelEncryption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TimesheetEntries_TimesheetId_Date",
                schema: "tenant_default",
                table: "TimesheetEntries");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_EmployeeId_Status_PeriodStart",
                schema: "tenant_default",
                table: "Timesheets",
                columns: new[] { "EmployeeId", "Status", "PeriodStart" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_Status_PeriodStart",
                schema: "tenant_default",
                table: "Timesheets",
                columns: new[] { "Status", "PeriodStart" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Timesheets_Hours_NonNegative",
                schema: "tenant_default",
                table: "Timesheets",
                sql: "\"TotalRegularHours\" >= 0 AND \"TotalOvertimeHours\" >= 0 AND \"TotalHolidayHours\" >= 0 AND \"TotalSickLeaveHours\" >= 0 AND \"TotalAnnualLeaveHours\" >= 0 AND \"TotalAbsentHours\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_TimesheetId_Date",
                schema: "tenant_default",
                table: "TimesheetEntries",
                columns: new[] { "TimesheetId", "Date" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "chk_TimesheetEntries_Hours_NonNegative",
                schema: "tenant_default",
                table: "TimesheetEntries",
                sql: "\"ActualHours\" >= 0 AND \"RegularHours\" >= 0 AND \"OvertimeHours\" >= 0 AND \"HolidayHours\" >= 0 AND \"SickLeaveHours\" >= 0 AND \"AnnualLeaveHours\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_SalaryComponents_Amount_NonNegative",
                schema: "tenant_default",
                table: "SalaryComponents",
                sql: "\"Amount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Payslips_Allowances_NonNegative",
                schema: "tenant_default",
                table: "Payslips",
                sql: "\"HousingAllowance\" >= 0 AND \"TransportAllowance\" >= 0 AND \"MealAllowance\" >= 0 AND \"MobileAllowance\" >= 0 AND \"OtherAllowances\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Payslips_LeaveDays_NonNegative",
                schema: "tenant_default",
                table: "Payslips",
                sql: "\"PaidLeaveDays\" >= 0 AND \"UnpaidLeaveDays\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Payslips_Overtime_NonNegative",
                schema: "tenant_default",
                table: "Payslips",
                sql: "\"OvertimeHours\" >= 0 AND \"OvertimePay\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Payslips_Salary_NonNegative",
                schema: "tenant_default",
                table: "Payslips",
                sql: "\"BasicSalary\" >= 0 AND \"TotalGrossSalary\" >= 0 AND \"TotalDeductions\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCycles_Status_PaymentDate",
                schema: "tenant_default",
                table: "PayrollCycles",
                columns: new[] { "Status", "PaymentDate" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCycles_Year_Month",
                schema: "tenant_default",
                table: "PayrollCycles",
                columns: new[] { "Year", "Month" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "chk_PayrollCycles_Month_Valid",
                schema: "tenant_default",
                table: "PayrollCycles",
                sql: "\"Month\" >= 1 AND \"Month\" <= 12");

            migrationBuilder.AddCheckConstraint(
                name: "chk_PayrollCycles_Salary_NonNegative",
                schema: "tenant_default",
                table: "PayrollCycles",
                sql: "\"TotalGrossSalary\" >= 0 AND \"TotalDeductions\" >= 0 AND \"TotalNetSalary\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_PayrollCycles_Year_Valid",
                schema: "tenant_default",
                table: "PayrollCycles",
                sql: "\"Year\" > 1900");

            migrationBuilder.AddCheckConstraint(
                name: "chk_LeaveEncashments_Amount_NonNegative",
                schema: "tenant_default",
                table: "LeaveEncashments",
                sql: "\"DailySalary\" >= 0 AND \"TotalEncashmentAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_LeaveEncashments_Days_NonNegative",
                schema: "tenant_default",
                table: "LeaveEncashments",
                sql: "\"UnusedAnnualLeaveDays\" >= 0 AND \"UnusedSickLeaveDays\" >= 0 AND \"TotalEncashableDays\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId_Year_LeaveTypeId",
                schema: "tenant_default",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "Year", "LeaveTypeId" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "chk_LeaveBalances_Accrual_NonNegative",
                schema: "tenant_default",
                table: "LeaveBalances",
                sql: "\"CarriedForward\" >= 0 AND \"Accrued\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_LeaveBalances_Days_NonNegative",
                schema: "tenant_default",
                table: "LeaveBalances",
                sql: "\"TotalEntitlement\" >= 0 AND \"UsedDays\" >= 0 AND \"PendingDays\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Employees_AnnualLeaveBalance_NonNegative",
                schema: "tenant_default",
                table: "Employees",
                sql: "\"AnnualLeaveBalance\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Employees_BasicSalary_NonNegative",
                schema: "tenant_default",
                table: "Employees",
                sql: "\"BasicSalary\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Employees_CasualLeaveBalance_NonNegative",
                schema: "tenant_default",
                table: "Employees",
                sql: "\"CasualLeaveBalance\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Employees_PasswordHash_Length",
                schema: "tenant_default",
                table: "Employees",
                sql: "\"PasswordHash\" IS NULL OR LENGTH(\"PasswordHash\") >= 32");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Employees_SickLeaveBalance_NonNegative",
                schema: "tenant_default",
                table: "Employees",
                sql: "\"SickLeaveBalance\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_DeviceApiKeys_Description_NotEmpty",
                schema: "tenant_default",
                table: "DeviceApiKeys",
                sql: "LENGTH(TRIM(\"Description\")) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_DeviceApiKeys_Hash_Length",
                schema: "tenant_default",
                table: "DeviceApiKeys",
                sql: "LENGTH(\"ApiKeyHash\") >= 64");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_DeviceId_Date",
                schema: "tenant_default",
                table: "Attendances",
                columns: new[] { "DeviceId", "Date" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeId_Date_Status",
                schema: "tenant_default",
                table: "Attendances",
                columns: new[] { "EmployeeId", "Date", "Status" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Attendances_OvertimeHours_NonNegative",
                schema: "tenant_default",
                table: "Attendances",
                sql: "\"OvertimeHours\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Attendances_OvertimeRate_NonNegative",
                schema: "tenant_default",
                table: "Attendances",
                sql: "\"OvertimeRate\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_Attendances_WorkingHours_NonNegative",
                schema: "tenant_default",
                table: "Attendances",
                sql: "\"WorkingHours\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_AttendanceAnomalies_Description_NotEmpty",
                schema: "tenant_default",
                table: "AttendanceAnomalies",
                sql: "\"AnomalyDescription\" IS NULL OR LENGTH(TRIM(\"AnomalyDescription\")) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Timesheets_EmployeeId_Status_PeriodStart",
                schema: "tenant_default",
                table: "Timesheets");

            migrationBuilder.DropIndex(
                name: "IX_Timesheets_Status_PeriodStart",
                schema: "tenant_default",
                table: "Timesheets");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Timesheets_Hours_NonNegative",
                schema: "tenant_default",
                table: "Timesheets");

            migrationBuilder.DropIndex(
                name: "IX_TimesheetEntries_TimesheetId_Date",
                schema: "tenant_default",
                table: "TimesheetEntries");

            migrationBuilder.DropCheckConstraint(
                name: "chk_TimesheetEntries_Hours_NonNegative",
                schema: "tenant_default",
                table: "TimesheetEntries");

            migrationBuilder.DropCheckConstraint(
                name: "chk_SalaryComponents_Amount_NonNegative",
                schema: "tenant_default",
                table: "SalaryComponents");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Payslips_Allowances_NonNegative",
                schema: "tenant_default",
                table: "Payslips");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Payslips_LeaveDays_NonNegative",
                schema: "tenant_default",
                table: "Payslips");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Payslips_Overtime_NonNegative",
                schema: "tenant_default",
                table: "Payslips");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Payslips_Salary_NonNegative",
                schema: "tenant_default",
                table: "Payslips");

            migrationBuilder.DropIndex(
                name: "IX_PayrollCycles_Status_PaymentDate",
                schema: "tenant_default",
                table: "PayrollCycles");

            migrationBuilder.DropIndex(
                name: "IX_PayrollCycles_Year_Month",
                schema: "tenant_default",
                table: "PayrollCycles");

            migrationBuilder.DropCheckConstraint(
                name: "chk_PayrollCycles_Month_Valid",
                schema: "tenant_default",
                table: "PayrollCycles");

            migrationBuilder.DropCheckConstraint(
                name: "chk_PayrollCycles_Salary_NonNegative",
                schema: "tenant_default",
                table: "PayrollCycles");

            migrationBuilder.DropCheckConstraint(
                name: "chk_PayrollCycles_Year_Valid",
                schema: "tenant_default",
                table: "PayrollCycles");

            migrationBuilder.DropCheckConstraint(
                name: "chk_LeaveEncashments_Amount_NonNegative",
                schema: "tenant_default",
                table: "LeaveEncashments");

            migrationBuilder.DropCheckConstraint(
                name: "chk_LeaveEncashments_Days_NonNegative",
                schema: "tenant_default",
                table: "LeaveEncashments");

            migrationBuilder.DropIndex(
                name: "IX_LeaveBalances_EmployeeId_Year_LeaveTypeId",
                schema: "tenant_default",
                table: "LeaveBalances");

            migrationBuilder.DropCheckConstraint(
                name: "chk_LeaveBalances_Accrual_NonNegative",
                schema: "tenant_default",
                table: "LeaveBalances");

            migrationBuilder.DropCheckConstraint(
                name: "chk_LeaveBalances_Days_NonNegative",
                schema: "tenant_default",
                table: "LeaveBalances");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Employees_AnnualLeaveBalance_NonNegative",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Employees_BasicSalary_NonNegative",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Employees_CasualLeaveBalance_NonNegative",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Employees_PasswordHash_Length",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Employees_SickLeaveBalance_NonNegative",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropCheckConstraint(
                name: "chk_DeviceApiKeys_Description_NotEmpty",
                schema: "tenant_default",
                table: "DeviceApiKeys");

            migrationBuilder.DropCheckConstraint(
                name: "chk_DeviceApiKeys_Hash_Length",
                schema: "tenant_default",
                table: "DeviceApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_DeviceId_Date",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_EmployeeId_Date_Status",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Attendances_OvertimeHours_NonNegative",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Attendances_OvertimeRate_NonNegative",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropCheckConstraint(
                name: "chk_Attendances_WorkingHours_NonNegative",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropCheckConstraint(
                name: "chk_AttendanceAnomalies_Description_NotEmpty",
                schema: "tenant_default",
                table: "AttendanceAnomalies");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_TimesheetId_Date",
                schema: "tenant_default",
                table: "TimesheetEntries",
                columns: new[] { "TimesheetId", "Date" });
        }
    }
}
