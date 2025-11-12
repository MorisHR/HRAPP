using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddNationalIdUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add UNIQUE constraint on NationalIdCard with filter to exclude NULL and deleted records
            // This is CRITICAL for data integrity - prevents duplicate National IDs for active employees
            // PostgreSQL supports partial unique indexes with WHERE clause
            migrationBuilder.CreateIndex(
                name: "IX_Employees_NationalIdCard_Unique",
                schema: "tenant_default",
                table: "Employees",
                column: "NationalIdCard",
                unique: true,
                filter: "\"NationalIdCard\" IS NOT NULL AND \"IsDeleted\" = false");

            // Add index for PassportNumber as well (important for expatriate identification)
            migrationBuilder.CreateIndex(
                name: "IX_Employees_PassportNumber_Unique",
                schema: "tenant_default",
                table: "Employees",
                column: "PassportNumber",
                unique: true,
                filter: "\"PassportNumber\" IS NOT NULL AND \"IsDeleted\" = false");

            // Add index for TaxIdNumber (required for tax compliance)
            migrationBuilder.CreateIndex(
                name: "IX_Employees_TaxIdNumber_Unique",
                schema: "tenant_default",
                table: "Employees",
                column: "TaxIdNumber",
                unique: true,
                filter: "\"TaxIdNumber\" IS NOT NULL AND \"IsDeleted\" = false");

            // Add index for NPFNumber (Mauritius statutory requirement)
            migrationBuilder.CreateIndex(
                name: "IX_Employees_NPFNumber_Unique",
                schema: "tenant_default",
                table: "Employees",
                column: "NPFNumber",
                unique: true,
                filter: "\"NPFNumber\" IS NOT NULL AND \"IsDeleted\" = false");

            // Add index for NSFNumber (Mauritius statutory requirement)
            migrationBuilder.CreateIndex(
                name: "IX_Employees_NSFNumber_Unique",
                schema: "tenant_default",
                table: "Employees",
                column: "NSFNumber",
                unique: true,
                filter: "\"NSFNumber\" IS NOT NULL AND \"IsDeleted\" = false");

            // Add index for BankAccountNumber (important for payroll)
            migrationBuilder.CreateIndex(
                name: "IX_Employees_BankAccountNumber_Unique",
                schema: "tenant_default",
                table: "Employees",
                column: "BankAccountNumber",
                unique: true,
                filter: "\"BankAccountNumber\" IS NOT NULL AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all the indexes in reverse order
            migrationBuilder.DropIndex(
                name: "IX_Employees_NationalIdCard_Unique",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_PassportNumber_Unique",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_TaxIdNumber_Unique",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_NPFNumber_Unique",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_NSFNumber_Unique",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_BankAccountNumber_Unique",
                schema: "tenant_default",
                table: "Employees");
        }
    }
}
