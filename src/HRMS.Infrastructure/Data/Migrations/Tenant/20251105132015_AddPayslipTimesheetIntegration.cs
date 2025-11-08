using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddPayslipTimesheetIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCalculatedFromTimesheets",
                schema: "tenant_default",
                table: "Payslips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TimesheetIdsJson",
                schema: "tenant_default",
                table: "Payslips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimesheetsProcessed",
                schema: "tenant_default",
                table: "Payslips",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCalculatedFromTimesheets",
                schema: "tenant_default",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "TimesheetIdsJson",
                schema: "tenant_default",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "TimesheetsProcessed",
                schema: "tenant_default",
                table: "Payslips");
        }
    }
}
