using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddTenantActivationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                schema: "master",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivatedBy",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationToken",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivationTokenExpiry",
                schema: "master",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminFirstName",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdminLastName",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsGovernmentEntity",
                schema: "master",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ActivatedBy",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ActivationToken",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ActivationTokenExpiry",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AdminFirstName",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AdminLastName",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsGovernmentEntity",
                schema: "master",
                table: "Tenants");
        }
    }
}
