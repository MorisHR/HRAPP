using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class ExpandedEmployeeModelAndDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnnualLeaveDays",
                schema: "tenant_default",
                table: "Employees",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BloodGroup",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CSGNumber",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CarryForwardAllowed",
                schema: "tenant_default",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CasualLeaveDays",
                schema: "tenant_default",
                table: "Employees",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CertificatesFilePath",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractFilePath",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmploymentContractType",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmploymentStatus",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HighestQualification",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HousingAllowance",
                schema: "tenant_default",
                table: "Employees",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdCopyFilePath",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndustrySector",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Languages",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MealAllowance",
                schema: "tenant_default",
                table: "Employees",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentFrequency",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProbationPeriodMonths",
                schema: "tenant_default",
                table: "Employees",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeFilePath",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SickLeaveDays",
                schema: "tenant_default",
                table: "Employees",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TransportAllowance",
                schema: "tenant_default",
                table: "Employees",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "University",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkLocation",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeDrafts",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormDataJson = table.Column<string>(type: "text", nullable: false),
                    DraftName = table.Column<string>(type: "text", nullable: false),
                    CompletionPercentage = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastEditedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LastEditedByName = table.Column<string>(type: "text", nullable: true),
                    LastEditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDrafts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeDrafts",
                schema: "tenant_default");

            migrationBuilder.DropColumn(
                name: "AnnualLeaveDays",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BloodGroup",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CSGNumber",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CarryForwardAllowed",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CasualLeaveDays",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CertificatesFilePath",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ContractFilePath",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Designation",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmploymentContractType",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmploymentStatus",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "HighestQualification",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "HousingAllowance",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IdCopyFilePath",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IndustrySector",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Languages",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MealAllowance",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Notes",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PaymentFrequency",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ProbationPeriodMonths",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ResumeFilePath",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SickLeaveDays",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Skills",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TransportAllowance",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "University",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WorkLocation",
                schema: "tenant_default",
                table: "Employees");
        }
    }
}
