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
            // ===================================
            // STEP 1: RENAME COLUMNS
            // ===================================

            // NOTE: The following column renames were removed because these columns
            // were already created with the correct names in migration 20251106053857_AddMultiDeviceBiometricAttendanceSystem
            // - Region (not State)
            // - AddressLine1 (not Address)
            // - Phone (not ContactPhone)
            // - Email (not ContactEmail)
            // - AddressLine2 already exists

            // ===================================
            // STEP 2: ADD NEW COLUMNS
            // ===================================

            // NOTE: AddressLine2, WorkingHoursJson, LocationManagerId, and CapacityHeadcount
            // were already added in migration 20251106053857, skipping

            // Add: District (Mauritius-specific) - This is the only new column
            migrationBuilder.AddColumn<string>(
                name: "District",
                schema: "tenant_default",
                table: "Locations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // ===================================
            // STEP 3: ALTER COLUMN TYPES & CONSTRAINTS
            // ===================================

            // NOTE: The following AlterColumn operations were removed because these columns
            // were already created with the correct types in migration 20251106053857:
            // - LocationType: Already character varying(100), nullable
            // - Latitude: Already numeric(10,8)
            // - Longitude: Already numeric(11,8)

            // ===================================
            // STEP 4: DROP UNUSED COLUMNS
            // ===================================

            // NOTE: The following DropColumn operations were removed because these columns
            // never existed in the Locations table (they were not in migration 20251106053857):
            // - ContactPerson
            // - IsHeadOffice

            // ===================================
            // STEP 5: CREATE INDEXES
            // ===================================

            // Index on District for Mauritius location queries - This is NEW
            migrationBuilder.CreateIndex(
                name: "IX_Locations_District",
                schema: "tenant_default",
                table: "Locations",
                column: "District");

            // Index on Region for broader regional queries - This is NEW
            migrationBuilder.CreateIndex(
                name: "IX_Locations_Region",
                schema: "tenant_default",
                table: "Locations",
                column: "Region");

            // NOTE: IX_Locations_LocationManagerId and FK_Locations_Employees_LocationManagerId
            // already exist from migration 20251106053857, skipping
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ===================================
            // STEP 1: DROP INDEXES
            // ===================================

            // Drop only the indexes we actually created
            migrationBuilder.DropIndex(
                name: "IX_Locations_Region",
                schema: "tenant_default",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Locations_District",
                schema: "tenant_default",
                table: "Locations");

            // ===================================
            // STEP 2: DROP NEW COLUMNS
            // ===================================

            // Drop only the District column we actually added
            migrationBuilder.DropColumn(
                name: "District",
                schema: "tenant_default",
                table: "Locations");

            // NOTE: We don't drop or revert any other columns because we didn't add or modify them
            // - AddressLine2, LocationManagerId, CapacityHeadcount, WorkingHoursJson: Already existed
            // - LocationType, Latitude, Longitude: Not modified
            // - ContactPerson, IsHeadOffice: Never existed
            // - Email, Phone, AddressLine1, Region: Already had these names
        }
    }
}
