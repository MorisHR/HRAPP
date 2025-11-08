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
            var schemas = new[] { "tenant_default", "tenant_siraaj" };

            foreach (var schema in schemas)
            {
                // 1. RENAME: Address → AddressLine1
                migrationBuilder.RenameColumn(
                    name: "Address",
                    schema: schema,
                    table: "Employees",
                    newName: "AddressLine1");

                // 2. ADD NEW COLUMNS
                migrationBuilder.AddColumn<string>(
                    name: "AddressLine2",
                    schema: schema,
                    table: "Employees",
                    type: "character varying(500)",
                    maxLength: 500,
                    nullable: true);

                migrationBuilder.AddColumn<string>(
                    name: "Village",
                    schema: schema,
                    table: "Employees",
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: true);

                migrationBuilder.AddColumn<string>(
                    name: "District",
                    schema: schema,
                    table: "Employees",
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: true);

                migrationBuilder.AddColumn<string>(
                    name: "Region",
                    schema: schema,
                    table: "Employees",
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: true);

                migrationBuilder.AddColumn<string>(
                    name: "Country",
                    schema: schema,
                    table: "Employees",
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false,
                    defaultValue: "Mauritius");

                // 3. CREATE INDEXES
                migrationBuilder.CreateIndex(
                    name: $"IX_Employees_District_{schema}",
                    schema: schema,
                    table: "Employees",
                    column: "District");

                migrationBuilder.CreateIndex(
                    name: $"IX_Employees_Region_{schema}",
                    schema: schema,
                    table: "Employees",
                    column: "Region");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var schemas = new[] { "tenant_default", "tenant_siraaj" };

            foreach (var schema in schemas)
            {
                // Drop indexes
                migrationBuilder.DropIndex(
                    name: $"IX_Employees_District_{schema}",
                    schema: schema,
                    table: "Employees");

                migrationBuilder.DropIndex(
                    name: $"IX_Employees_Region_{schema}",
                    schema: schema,
                    table: "Employees");

                // Drop new columns
                migrationBuilder.DropColumn(
                    name: "AddressLine2",
                    schema: schema,
                    table: "Employees");

                migrationBuilder.DropColumn(
                    name: "Village",
                    schema: schema,
                    table: "Employees");

                migrationBuilder.DropColumn(
                    name: "District",
                    schema: schema,
                    table: "Employees");

                migrationBuilder.DropColumn(
                    name: "Region",
                    schema: schema,
                    table: "Employees");

                migrationBuilder.DropColumn(
                    name: "Country",
                    schema: schema,
                    table: "Employees");

                // Rename back: AddressLine1 → Address
                migrationBuilder.RenameColumn(
                    name: "AddressLine1",
                    schema: schema,
                    table: "Employees",
                    newName: "Address");
            }
        }
    }
}
