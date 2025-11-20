using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddDepartmentAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProcessedBy",
                schema: "tenant_default",
                table: "PayrollCycles",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                schema: "tenant_default",
                table: "PayrollCycles",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DepartmentAuditLogs",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OldValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PerformedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentAuditLogs_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "tenant_default",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentAuditLogs_ActivityType",
                schema: "tenant_default",
                table: "DepartmentAuditLogs",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentAuditLogs_DepartmentId",
                schema: "tenant_default",
                table: "DepartmentAuditLogs",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentAuditLogs_PerformedAt",
                schema: "tenant_default",
                table: "DepartmentAuditLogs",
                column: "PerformedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentAuditLogs",
                schema: "tenant_default");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProcessedBy",
                schema: "tenant_default",
                table: "PayrollCycles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ApprovedBy",
                schema: "tenant_default",
                table: "PayrollCycles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
