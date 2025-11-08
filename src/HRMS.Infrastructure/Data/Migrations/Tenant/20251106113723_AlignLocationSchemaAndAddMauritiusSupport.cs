using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AlignLocationSchemaAndAddMauritiusSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Apply changes to both tenant schemas
            foreach (var schema in new[] { "tenant_default", "tenant_siraaj" })
            {
                // ===================================
                // STEP 1: RENAME COLUMNS
                // ===================================

                // Rename: State → Region
                migrationBuilder.RenameColumn(
                    name: "State",
                    schema: schema,
                    table: "Locations",
                    newName: "Region");

                // Rename: Address → AddressLine1
                migrationBuilder.RenameColumn(
                    name: "Address",
                    schema: schema,
                    table: "Locations",
                    newName: "AddressLine1");

                // Rename: ContactPhone → Phone
                migrationBuilder.RenameColumn(
                    name: "ContactPhone",
                    schema: schema,
                    table: "Locations",
                    newName: "Phone");

                // Rename: ContactEmail → Email
                migrationBuilder.RenameColumn(
                    name: "ContactEmail",
                    schema: schema,
                    table: "Locations",
                    newName: "Email");

                // ===================================
                // STEP 2: ADD NEW COLUMNS
                // ===================================

                // Add: AddressLine2
                migrationBuilder.AddColumn<string>(
                    name: "AddressLine2",
                    schema: schema,
                    table: "Locations",
                    type: "character varying(500)",
                    maxLength: 500,
                    nullable: true);

                // Add: District (Mauritius-specific)
                migrationBuilder.AddColumn<string>(
                    name: "District",
                    schema: schema,
                    table: "Locations",
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: true);

                // Add: WorkingHoursJson (JSONB type for flexibility)
                migrationBuilder.AddColumn<string>(
                    name: "WorkingHoursJson",
                    schema: schema,
                    table: "Locations",
                    type: "jsonb",
                    nullable: true);

                // Add: LocationManagerId
                migrationBuilder.AddColumn<Guid>(
                    name: "LocationManagerId",
                    schema: schema,
                    table: "Locations",
                    type: "uuid",
                    nullable: true);

                // Add: CapacityHeadcount
                migrationBuilder.AddColumn<int>(
                    name: "CapacityHeadcount",
                    schema: schema,
                    table: "Locations",
                    type: "integer",
                    nullable: true);

                // ===================================
                // STEP 3: ALTER COLUMN TYPES & CONSTRAINTS
                // ===================================

                // LocationType: Make nullable, adjust max length
                migrationBuilder.AlterColumn<string>(
                    name: "LocationType",
                    schema: schema,
                    table: "Locations",
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: true,
                    oldClrType: typeof(string),
                    oldType: "character varying(50)",
                    oldMaxLength: 50,
                    oldNullable: false);

                // Latitude: Change precision from (10,7) to (10,8)
                migrationBuilder.AlterColumn<decimal>(
                    name: "Latitude",
                    schema: schema,
                    table: "Locations",
                    type: "numeric(10,8)",
                    precision: 10,
                    scale: 8,
                    nullable: true,
                    oldClrType: typeof(decimal),
                    oldType: "numeric(10,7)",
                    oldPrecision: 10,
                    oldScale: 7,
                    oldNullable: true);

                // Longitude: Change precision from (10,7) to (11,8)
                migrationBuilder.AlterColumn<decimal>(
                    name: "Longitude",
                    schema: schema,
                    table: "Locations",
                    type: "numeric(11,8)",
                    precision: 11,
                    scale: 8,
                    nullable: true,
                    oldClrType: typeof(decimal),
                    oldType: "numeric(10,7)",
                    oldPrecision: 10,
                    oldScale: 7,
                    oldNullable: true);

                // ===================================
                // STEP 4: DROP UNUSED COLUMNS
                // ===================================

                // Drop: ContactPerson (not in entity model)
                migrationBuilder.DropColumn(
                    name: "ContactPerson",
                    schema: schema,
                    table: "Locations");

                // Drop: IsHeadOffice (not in entity model)
                migrationBuilder.DropColumn(
                    name: "IsHeadOffice",
                    schema: schema,
                    table: "Locations");

                // ===================================
                // STEP 5: CREATE INDEXES
                // ===================================

                // Index on District for Mauritius location queries
                migrationBuilder.CreateIndex(
                    name: $"IX_Locations_District_{schema}",
                    schema: schema,
                    table: "Locations",
                    column: "District");

                // Index on Region for broader regional queries
                migrationBuilder.CreateIndex(
                    name: $"IX_Locations_Region_{schema}",
                    schema: schema,
                    table: "Locations",
                    column: "Region");

                // Index on LocationManagerId for manager queries
                migrationBuilder.CreateIndex(
                    name: $"IX_Locations_LocationManagerId_{schema}",
                    schema: schema,
                    table: "Locations",
                    column: "LocationManagerId");

                // ===================================
                // STEP 6: ADD FOREIGN KEY
                // ===================================

                // Foreign Key: LocationManagerId → Employees.Id
                migrationBuilder.AddForeignKey(
                    name: $"FK_Locations_Employees_LocationManagerId_{schema}",
                    schema: schema,
                    table: "Locations",
                    column: "LocationManagerId",
                    principalSchema: schema,
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback changes for both tenant schemas
            foreach (var schema in new[] { "tenant_default", "tenant_siraaj" })
            {
                // ===================================
                // STEP 1: DROP FOREIGN KEY
                // ===================================

                migrationBuilder.DropForeignKey(
                    name: $"FK_Locations_Employees_LocationManagerId_{schema}",
                    schema: schema,
                    table: "Locations");

                // ===================================
                // STEP 2: DROP INDEXES
                // ===================================

                migrationBuilder.DropIndex(
                    name: $"IX_Locations_LocationManagerId_{schema}",
                    schema: schema,
                    table: "Locations");

                migrationBuilder.DropIndex(
                    name: $"IX_Locations_Region_{schema}",
                    schema: schema,
                    table: "Locations");

                migrationBuilder.DropIndex(
                    name: $"IX_Locations_District_{schema}",
                    schema: schema,
                    table: "Locations");

                // ===================================
                // STEP 3: RE-ADD DROPPED COLUMNS
                // ===================================

                // Re-add: IsHeadOffice
                migrationBuilder.AddColumn<bool>(
                    name: "IsHeadOffice",
                    schema: schema,
                    table: "Locations",
                    type: "boolean",
                    nullable: false,
                    defaultValue: false);

                // Re-add: ContactPerson
                migrationBuilder.AddColumn<string>(
                    name: "ContactPerson",
                    schema: schema,
                    table: "Locations",
                    type: "character varying(200)",
                    maxLength: 200,
                    nullable: true);

                // ===================================
                // STEP 4: REVERT COLUMN TYPE CHANGES
                // ===================================

                // Revert Longitude precision
                migrationBuilder.AlterColumn<decimal>(
                    name: "Longitude",
                    schema: schema,
                    table: "Locations",
                    type: "numeric(10,7)",
                    precision: 10,
                    scale: 7,
                    nullable: true,
                    oldClrType: typeof(decimal),
                    oldType: "numeric(11,8)",
                    oldPrecision: 11,
                    oldScale: 8,
                    oldNullable: true);

                // Revert Latitude precision
                migrationBuilder.AlterColumn<decimal>(
                    name: "Latitude",
                    schema: schema,
                    table: "Locations",
                    type: "numeric(10,7)",
                    precision: 10,
                    scale: 7,
                    nullable: true,
                    oldClrType: typeof(decimal),
                    oldType: "numeric(10,8)",
                    oldPrecision: 10,
                    oldScale: 8,
                    oldNullable: true);

                // Revert LocationType
                migrationBuilder.AlterColumn<string>(
                    name: "LocationType",
                    schema: schema,
                    table: "Locations",
                    type: "character varying(50)",
                    maxLength: 50,
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "character varying(100)",
                    oldMaxLength: 100,
                    oldNullable: true);

                // ===================================
                // STEP 5: DROP NEW COLUMNS
                // ===================================

                migrationBuilder.DropColumn(
                    name: "CapacityHeadcount",
                    schema: schema,
                    table: "Locations");

                migrationBuilder.DropColumn(
                    name: "LocationManagerId",
                    schema: schema,
                    table: "Locations");

                migrationBuilder.DropColumn(
                    name: "WorkingHoursJson",
                    schema: schema,
                    table: "Locations");

                migrationBuilder.DropColumn(
                    name: "District",
                    schema: schema,
                    table: "Locations");

                migrationBuilder.DropColumn(
                    name: "AddressLine2",
                    schema: schema,
                    table: "Locations");

                // ===================================
                // STEP 6: RENAME COLUMNS BACK
                // ===================================

                // Revert: Email → ContactEmail
                migrationBuilder.RenameColumn(
                    name: "Email",
                    schema: schema,
                    table: "Locations",
                    newName: "ContactEmail");

                // Revert: Phone → ContactPhone
                migrationBuilder.RenameColumn(
                    name: "Phone",
                    schema: schema,
                    table: "Locations",
                    newName: "ContactPhone");

                // Revert: AddressLine1 → Address
                migrationBuilder.RenameColumn(
                    name: "AddressLine1",
                    schema: schema,
                    table: "Locations",
                    newName: "Address");

                // Revert: Region → State
                migrationBuilder.RenameColumn(
                    name: "Region",
                    schema: schema,
                    table: "Locations",
                    newName: "State");
            }
        }
    }
}
