using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddSuperAdminSecurityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedIPAddresses",
                schema: "master",
                table: "AdminUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedLoginHours",
                schema: "master",
                table: "AdminUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBySuperAdminId",
                schema: "master",
                table: "AdminUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInitialSetupAccount",
                schema: "master",
                table: "AdminUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailedLoginAttempt",
                schema: "master",
                table: "AdminUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastLoginIPAddress",
                schema: "master",
                table: "AdminUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedBySuperAdminId",
                schema: "master",
                table: "AdminUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangeDate",
                schema: "master",
                table: "AdminUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                schema: "master",
                table: "AdminUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordExpiresAt",
                schema: "master",
                table: "AdminUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHistory",
                schema: "master",
                table: "AdminUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Permissions",
                schema: "master",
                table: "AdminUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionTimeoutMinutes",
                schema: "master",
                table: "AdminUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StatusNotes",
                schema: "master",
                table: "AdminUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedIPAddresses",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "AllowedLoginHours",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "CreatedBySuperAdminId",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "IsInitialSetupAccount",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "LastFailedLoginAttempt",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginIPAddress",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "LastModifiedBySuperAdminId",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "LastPasswordChangeDate",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "PasswordExpiresAt",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "PasswordHistory",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "Permissions",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "SessionTimeoutMinutes",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "StatusNotes",
                schema: "master",
                table: "AdminUsers");
        }
    }
}
