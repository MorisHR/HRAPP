using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddEmployeePasswordResetAndAdminFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                schema: "tenant_default",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTwoFactorEnabled",
                schema: "tenant_default",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangeDate",
                schema: "tenant_default",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                schema: "tenant_default",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                schema: "tenant_default",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecret",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsTwoFactorEnabled",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LastPasswordChangeDate",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiry",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TwoFactorSecret",
                schema: "tenant_default",
                table: "Employees");
        }
    }
}
