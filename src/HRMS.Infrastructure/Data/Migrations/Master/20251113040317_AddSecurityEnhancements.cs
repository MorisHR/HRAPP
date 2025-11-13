using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddSecurityEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "AdminUserId",
                schema: "master",
                table: "RefreshTokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                schema: "master",
                table: "RefreshTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                schema: "master",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "SessionTimeoutMinutes",
                schema: "master",
                table: "RefreshTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "master",
                table: "RefreshTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivationTokenExpiry",
                schema: "master",
                table: "AdminUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                schema: "master",
                table: "AdminUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                schema: "master",
                table: "AdminUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeId",
                schema: "master",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                schema: "master",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "SessionTimeoutMinutes",
                schema: "master",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "master",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "ActivationTokenExpiry",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiry",
                schema: "master",
                table: "AdminUsers");

            migrationBuilder.AlterColumn<Guid>(
                name: "AdminUserId",
                schema: "master",
                table: "RefreshTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
