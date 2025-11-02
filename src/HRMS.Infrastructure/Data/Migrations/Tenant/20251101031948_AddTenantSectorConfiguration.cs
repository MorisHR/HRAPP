using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddTenantSectorConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantCustomComplianceRules",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorComplianceRuleId = table.Column<int>(type: "integer", nullable: false),
                    IsUsingDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CustomRuleConfig = table.Column<string>(type: "jsonb", nullable: true),
                    Justification = table.Column<string>(type: "text", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RuleCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_TenantCustomComplianceRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantSectorConfigurations",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorId = table.Column<int>(type: "integer", nullable: false),
                    SelectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SelectedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    SectorName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SectorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_TenantSectorConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantCustomComplianceRules_RuleCategory",
                schema: "tenant_default",
                table: "TenantCustomComplianceRules",
                column: "RuleCategory");

            migrationBuilder.CreateIndex(
                name: "IX_TenantCustomComplianceRules_SectorComplianceRuleId",
                schema: "tenant_default",
                table: "TenantCustomComplianceRules",
                column: "SectorComplianceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSectorConfigurations_SectorId",
                schema: "tenant_default",
                table: "TenantSectorConfigurations",
                column: "SectorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantCustomComplianceRules",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "TenantSectorConfigurations",
                schema: "tenant_default");
        }
    }
}
