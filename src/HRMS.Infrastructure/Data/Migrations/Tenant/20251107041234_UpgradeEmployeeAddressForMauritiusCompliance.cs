using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class UpgradeEmployeeAddressForMauritiusCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. RENAME: Address → AddressLine1
            migrationBuilder.RenameColumn(
                name: "Address",
                schema: "tenant_default",
                table: "Employees",
                newName: "AddressLine1");

            // 2. ADD NEW COLUMNS
            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                schema: "tenant_default",
                table: "Employees",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Village",
                schema: "tenant_default",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                schema: "tenant_default",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                schema: "tenant_default",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "tenant_default",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Mauritius");

            // 3. CREATE INDEXES
            migrationBuilder.CreateIndex(
                name: "IX_Employees_District",
                schema: "tenant_default",
                table: "Employees",
                column: "District");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Region",
                schema: "tenant_default",
                table: "Employees",
                column: "Region");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Employees_District",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Region",
                schema: "tenant_default",
                table: "Employees");

            // Drop new columns
            migrationBuilder.DropColumn(
                name: "AddressLine2",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Village",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "District",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Region",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "tenant_default",
                table: "Employees");

            // Rename back: AddressLine1 → Address
            migrationBuilder.RenameColumn(
                name: "AddressLine1",
                schema: "tenant_default",
                table: "Employees",
                newName: "Address");
        }
    }
}
