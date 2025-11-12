using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddMissingCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // PAYROLL CYCLE PERFORMANCE INDEXES
            // =====================================================

            // Optimize queries for monthly payroll reports and cycle lookups
            migrationBuilder.CreateIndex(
                name: "IX_PayrollCycles_Year_Month",
                schema: "tenant_default",
                table: "PayrollCycles",
                columns: new[] { "Year", "Month" },
                descending: new[] { true, true },
                filter: "\"IsDeleted\" = false");

            // Optimize queries filtering by status and payment date for payroll reports
            migrationBuilder.CreateIndex(
                name: "IX_PayrollCycles_Status_PaymentDate",
                schema: "tenant_default",
                table: "PayrollCycles",
                columns: new[] { "Status", "PaymentDate" },
                descending: new[] { false, true },
                filter: "\"IsDeleted\" = false");

            // =====================================================
            // LEAVE BALANCE OPTIMIZATION INDEXES
            // =====================================================

            // Optimize queries for employee leave balances by year and type
            // Critical for: Leave application processing, balance checks, encashment calculations
            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId_Year_LeaveTypeId",
                schema: "tenant_default",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "Year", "LeaveTypeId" },
                descending: new[] { false, true, false },
                filter: "\"IsDeleted\" = false");

            // =====================================================
            // ATTENDANCE MONTHLY QUERY OPTIMIZATION
            // =====================================================

            // Optimize queries for monthly attendance reports and employee attendance checks
            // Critical for: Payroll integration, attendance reports, anomaly detection
            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeId_Date_Status",
                schema: "tenant_default",
                table: "Attendances",
                columns: new[] { "EmployeeId", "Date", "Status" },
                descending: new[] { false, true, false },
                filter: "\"IsDeleted\" = false");

            // Additional index for device-based attendance lookups
            migrationBuilder.CreateIndex(
                name: "IX_Attendances_DeviceId_Date",
                schema: "tenant_default",
                table: "Attendances",
                columns: new[] { "DeviceId", "Date" },
                descending: new[] { false, true },
                filter: "\"IsDeleted\" = false");

            // =====================================================
            // TIMESHEET APPROVAL WORKFLOW INDEXES
            // =====================================================

            // Optimize queries for timesheet approvals and status filtering
            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_Status_PeriodStart",
                schema: "tenant_default",
                table: "Timesheets",
                columns: new[] { "Status", "PeriodStart" },
                descending: new[] { false, true },
                filter: "\"IsDeleted\" = false");

            // Optimize queries for employee timesheet lookups during approval workflow
            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_EmployeeId_Status_PeriodStart",
                schema: "tenant_default",
                table: "Timesheets",
                columns: new[] { "EmployeeId", "Status", "PeriodStart" },
                descending: new[] { false, false, true },
                filter: "\"IsDeleted\" = false");

            // =====================================================
            // EMPLOYEE SEARCH OPTIMIZATION
            // =====================================================

            // Optimize employee directory searches by name with active status filter
            // Critical for: Employee lookups, directory searches, data entry verification
            migrationBuilder.CreateIndex(
                name: "IX_Employees_FirstName_LastName_IsActive",
                schema: "tenant_default",
                table: "Employees",
                columns: new[] { "FirstName", "LastName" },
                filter: "\"IsActive\" = true AND \"IsDeleted\" = false");

            // =====================================================
            // TIMESHEET ENTRY INDEXES
            // =====================================================

            // Optimize queries for timesheet entry filtering by status and type
            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_TimesheetId_Date",
                schema: "tenant_default",
                table: "TimesheetEntries",
                columns: new[] { "TimesheetId", "Date" },
                descending: new[] { false, true },
                filter: "\"IsDeleted\" = false");

            // =====================================================
            // LEAVE APPLICATION WORKFLOW INDEXES
            // =====================================================

            // Optimize queries for leave application searches by date range
            migrationBuilder.CreateIndex(
                name: "IX_LeaveApplications_EmployeeId_StartDate_EndDate",
                schema: "tenant_default",
                table: "LeaveApplications",
                columns: new[] { "EmployeeId", "StartDate", "EndDate" },
                descending: new[] { false, true, true },
                filter: "\"IsDeleted\" = false");

            // =====================================================
            // BIOMETRIC PUNCH RECORD INDEXES (Fortune 500 Performance)
            // =====================================================

            // Optimize daily punch processing queries
            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_ProcessingStatus_PunchTime",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                columns: new[] { "ProcessingStatus", "PunchTime" },
                descending: new[] { false, true },
                filter: "\"IsDeleted\" = false");

            // Optimize employee punch history queries
            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_EmployeeId_PunchTime",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                columns: new[] { "EmployeeId", "PunchTime" },
                descending: new[] { false, true },
                filter: "\"IsDeleted\" = false");

            // Optimize device sync queries
            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_DeviceId_PunchTime",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                columns: new[] { "DeviceId", "PunchTime" },
                descending: new[] { false, true },
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback all composite indexes
            migrationBuilder.DropIndex(
                name: "IX_PayrollCycles_Year_Month",
                schema: "tenant_default",
                table: "PayrollCycles");

            migrationBuilder.DropIndex(
                name: "IX_PayrollCycles_Status_PaymentDate",
                schema: "tenant_default",
                table: "PayrollCycles");

            migrationBuilder.DropIndex(
                name: "IX_LeaveBalances_EmployeeId_Year_LeaveTypeId",
                schema: "tenant_default",
                table: "LeaveBalances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_EmployeeId_Date_Status",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_DeviceId_Date",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Timesheets_Status_PeriodStart",
                schema: "tenant_default",
                table: "Timesheets");

            migrationBuilder.DropIndex(
                name: "IX_Timesheets_EmployeeId_Status_PeriodStart",
                schema: "tenant_default",
                table: "Timesheets");

            migrationBuilder.DropIndex(
                name: "IX_Employees_FirstName_LastName_IsActive",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_TimesheetEntries_TimesheetId_Date",
                schema: "tenant_default",
                table: "TimesheetEntries");

            migrationBuilder.DropIndex(
                name: "IX_LeaveApplications_EmployeeId_StartDate_EndDate",
                schema: "tenant_default",
                table: "LeaveApplications");

            migrationBuilder.DropIndex(
                name: "IX_BiometricPunchRecords_ProcessingStatus_PunchTime",
                schema: "tenant_default",
                table: "BiometricPunchRecords");

            migrationBuilder.DropIndex(
                name: "IX_BiometricPunchRecords_EmployeeId_PunchTime",
                schema: "tenant_default",
                table: "BiometricPunchRecords");

            migrationBuilder.DropIndex(
                name: "IX_BiometricPunchRecords_DeviceId_PunchTime",
                schema: "tenant_default",
                table: "BiometricPunchRecords");
        }
    }
}
