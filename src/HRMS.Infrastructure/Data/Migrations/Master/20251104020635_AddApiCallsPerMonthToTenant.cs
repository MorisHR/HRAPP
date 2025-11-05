using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddApiCallsPerMonthToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentStorageBytes",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MaxStorageBytes",
                schema: "master",
                table: "Tenants");

            migrationBuilder.RenameColumn(
                name: "SubscriptionPlan",
                schema: "master",
                table: "Tenants",
                newName: "MaxStorageGB");

            migrationBuilder.RenameColumn(
                name: "MaxApiCallsPerHour",
                schema: "master",
                table: "Tenants",
                newName: "EmployeeTier");

            migrationBuilder.AddColumn<int>(
                name: "ApiCallsPerMonth",
                schema: "master",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStorageGB",
                schema: "master",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPrice",
                schema: "master",
                table: "Tenants",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            // Note: SectorId and SectorSelectedAt already exist from previous migration
            // Note: IndustrySectors and SectorComplianceRules tables already exist from previous migration
            // Note: AccessFailedCount, LockoutEnabled, LockoutEnd already exist in AdminUsers from previous migration
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: Not dropping SectorId/SectorSelectedAt or IndustrySectors/SectorComplianceRules tables
            // as they existed before this migration
            // Note: Not dropping AccessFailedCount/LockoutEnabled/LockoutEnd from AdminUsers
            // as they existed before this migration

            migrationBuilder.DropColumn(
                name: "ApiCallsPerMonth",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CurrentStorageGB",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MonthlyPrice",
                schema: "master",
                table: "Tenants");

            migrationBuilder.RenameColumn(
                name: "MaxStorageGB",
                schema: "master",
                table: "Tenants",
                newName: "SubscriptionPlan");

            migrationBuilder.RenameColumn(
                name: "EmployeeTier",
                schema: "master",
                table: "Tenants",
                newName: "MaxApiCallsPerHour");

            migrationBuilder.AddColumn<long>(
                name: "CurrentStorageBytes",
                schema: "master",
                table: "Tenants",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "MaxStorageBytes",
                schema: "master",
                table: "Tenants",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
