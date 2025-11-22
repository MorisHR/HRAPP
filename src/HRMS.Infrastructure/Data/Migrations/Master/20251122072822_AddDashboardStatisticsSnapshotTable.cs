using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddDashboardStatisticsSnapshotTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardStatisticsSnapshots",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalTenants = table.Column<int>(type: "integer", nullable: false),
                    ActiveTenants = table.Column<int>(type: "integer", nullable: false),
                    TotalEmployees = table.Column<int>(type: "integer", nullable: false),
                    MonthlyRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    TrialTenants = table.Column<int>(type: "integer", nullable: false),
                    SuspendedTenants = table.Column<int>(type: "integer", nullable: false),
                    ExpiredTenants = table.Column<int>(type: "integer", nullable: false),
                    TotalStorageGB = table.Column<decimal>(type: "numeric", nullable: false),
                    AverageStorageUsagePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    NewTenantsToday = table.Column<int>(type: "integer", nullable: false),
                    ChurnedTenantsToday = table.Column<int>(type: "integer", nullable: false),
                    IsAutomatic = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardStatisticsSnapshots", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardStatisticsSnapshots",
                schema: "master");
        }
    }
}
