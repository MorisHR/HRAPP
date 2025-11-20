using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddEmployeeLifecycleTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEligibleForRehire",
                schema: "tenant_default",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TerminationDate",
                schema: "tenant_default",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TerminationNotes",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TerminationReason",
                schema: "tenant_default",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TerminationType",
                schema: "tenant_default",
                table: "Employees",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JiraIntegrations",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    JiraInstanceUrl = table.Column<string>(type: "text", nullable: false),
                    JiraApiTokenEncrypted = table.Column<string>(type: "text", nullable: false),
                    JiraUserEmail = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebhookSecret = table.Column<string>(type: "text", nullable: true),
                    ProjectMappingsJson = table.Column<string>(type: "text", nullable: true),
                    SyncIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    AutoSyncEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncError = table.Column<string>(type: "text", nullable: true),
                    TotalWorkLogsSynced = table.Column<int>(type: "integer", nullable: false),
                    TotalIssuesSynced = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_JiraIntegrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JiraIssueAssignments",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    JiraIssueKey = table.Column<string>(type: "text", nullable: false),
                    JiraIssueSummary = table.Column<string>(type: "text", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    JiraProjectKey = table.Column<string>(type: "text", nullable: true),
                    IssueType = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: true),
                    EstimateHours = table.Column<decimal>(type: "numeric", nullable: true),
                    RemainingHours = table.Column<decimal>(type: "numeric", nullable: true),
                    TimeSpentHours = table.Column<decimal>(type: "numeric", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SprintName = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedInJira = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    JiraIssueUrl = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_JiraIssueAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JiraIssueAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JiraIssueAssignments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "tenant_default",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JiraWorkLogs",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    JiraWorkLogId = table.Column<string>(type: "text", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    JiraIssueKey = table.Column<string>(type: "text", nullable: false),
                    JiraIssueSummary = table.Column<string>(type: "text", nullable: true),
                    JiraIssueType = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeSpentHours = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    JiraAuthorUsername = table.Column<string>(type: "text", nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WasConverted = table.Column<bool>(type: "boolean", nullable: false),
                    TimesheetProjectAllocationId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_JiraWorkLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JiraWorkLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JiraWorkLogs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "tenant_default",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JiraWorkLogs_TimesheetProjectAllocations_TimesheetProjectAl~",
                        column: x => x.TimesheetProjectAllocationId,
                        principalSchema: "tenant_default",
                        principalTable: "TimesheetProjectAllocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_JiraIssueAssignments_EmployeeId",
                schema: "tenant_default",
                table: "JiraIssueAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_JiraIssueAssignments_ProjectId",
                schema: "tenant_default",
                table: "JiraIssueAssignments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_JiraWorkLogs_EmployeeId",
                schema: "tenant_default",
                table: "JiraWorkLogs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_JiraWorkLogs_ProjectId",
                schema: "tenant_default",
                table: "JiraWorkLogs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_JiraWorkLogs_TimesheetProjectAllocationId",
                schema: "tenant_default",
                table: "JiraWorkLogs",
                column: "TimesheetProjectAllocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JiraIntegrations",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "JiraIssueAssignments",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "JiraWorkLogs",
                schema: "tenant_default");

            migrationBuilder.DropColumn(
                name: "IsEligibleForRehire",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TerminationDate",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TerminationNotes",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TerminationReason",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TerminationType",
                schema: "tenant_default",
                table: "Employees");
        }
    }
}
