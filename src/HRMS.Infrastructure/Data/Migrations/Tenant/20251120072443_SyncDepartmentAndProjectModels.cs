using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class SyncDepartmentAndProjectModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectCode = table.Column<string>(type: "text", nullable: false),
                    ProjectName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ClientName = table.Column<string>(type: "text", nullable: true),
                    ProjectType = table.Column<string>(type: "text", nullable: false),
                    IsBillable = table.Column<bool>(type: "boolean", nullable: false),
                    BillingRate = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BudgetHours = table.Column<decimal>(type: "numeric", nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowTimeEntry = table.Column<bool>(type: "boolean", nullable: false),
                    RequireApproval = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "tenant_default",
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Employees_ProjectManagerId",
                        column: x => x.ProjectManagerId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpectedHoursPerWeek = table.Column<decimal>(type: "numeric", nullable: true),
                    BillingRateOverride = table.Column<decimal>(type: "numeric", nullable: true),
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
                    table.PrimaryKey("PK_ProjectMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "tenant_default",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetIntelligenceEvents",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TimesheetId = table.Column<Guid>(type: "uuid", nullable: true),
                    TimesheetEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    EventTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    EventData = table.Column<string>(type: "text", nullable: true),
                    ModelUsed = table.Column<string>(type: "text", nullable: true),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: true),
                    AutomatedAction = table.Column<string>(type: "text", nullable: true),
                    RequiredManualIntervention = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "text", nullable: true),
                    WasDecisionCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    FeedbackNotes = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_TimesheetIntelligenceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimesheetIntelligenceEvents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TimesheetIntelligenceEvents_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "tenant_default",
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TimesheetIntelligenceEvents_TimesheetEntries_TimesheetEntry~",
                        column: x => x.TimesheetEntryId,
                        principalSchema: "tenant_default",
                        principalTable: "TimesheetEntries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TimesheetIntelligenceEvents_Timesheets_TimesheetId",
                        column: x => x.TimesheetId,
                        principalSchema: "tenant_default",
                        principalTable: "Timesheets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TimesheetProjectAllocations",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Hours = table.Column<decimal>(type: "numeric", nullable: false),
                    TaskDescription = table.Column<string>(type: "text", nullable: true),
                    IsBillable = table.Column<bool>(type: "boolean", nullable: false),
                    BillingRate = table.Column<decimal>(type: "numeric", nullable: true),
                    AllocationSource = table.Column<string>(type: "text", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: true),
                    SuggestionAccepted = table.Column<bool>(type: "boolean", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_TimesheetProjectAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimesheetProjectAllocations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimesheetProjectAllocations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "tenant_default",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimesheetProjectAllocations_TimesheetEntries_TimesheetEntry~",
                        column: x => x.TimesheetEntryId,
                        principalSchema: "tenant_default",
                        principalTable: "TimesheetEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkPatterns",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    HourOfDay = table.Column<int>(type: "integer", nullable: true),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    AverageHours = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalHours = table.Column<decimal>(type: "numeric", nullable: false),
                    LastOccurrence = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstOccurrence = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PatternContext = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_WorkPatterns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkPatterns_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkPatterns_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "tenant_default",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectAllocationSuggestions",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestedHours = table.Column<decimal>(type: "numeric", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "integer", nullable: false),
                    SuggestionSource = table.Column<string>(type: "text", nullable: false),
                    SuggestionReason = table.Column<string>(type: "text", nullable: true),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ActionedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimesheetProjectAllocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    FinalHours = table.Column<decimal>(type: "numeric", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    EmployeeFeedback = table.Column<string>(type: "text", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ProjectAllocationSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectAllocationSuggestions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectAllocationSuggestions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "tenant_default",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectAllocationSuggestions_TimesheetProjectAllocations_Ti~",
                        column: x => x.TimesheetProjectAllocationId,
                        principalSchema: "tenant_default",
                        principalTable: "TimesheetProjectAllocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAllocationSuggestions_EmployeeId",
                schema: "tenant_default",
                table: "ProjectAllocationSuggestions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAllocationSuggestions_ProjectId",
                schema: "tenant_default",
                table: "ProjectAllocationSuggestions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectAllocationSuggestions_TimesheetProjectAllocationId",
                schema: "tenant_default",
                table: "ProjectAllocationSuggestions",
                column: "TimesheetProjectAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_EmployeeId",
                schema: "tenant_default",
                table: "ProjectMembers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId",
                schema: "tenant_default",
                table: "ProjectMembers",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DepartmentId",
                schema: "tenant_default",
                table: "Projects",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectManagerId",
                schema: "tenant_default",
                table: "Projects",
                column: "ProjectManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetIntelligenceEvents_EmployeeId",
                schema: "tenant_default",
                table: "TimesheetIntelligenceEvents",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetIntelligenceEvents_ProjectId",
                schema: "tenant_default",
                table: "TimesheetIntelligenceEvents",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetIntelligenceEvents_TimesheetEntryId",
                schema: "tenant_default",
                table: "TimesheetIntelligenceEvents",
                column: "TimesheetEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetIntelligenceEvents_TimesheetId",
                schema: "tenant_default",
                table: "TimesheetIntelligenceEvents",
                column: "TimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetProjectAllocations_EmployeeId",
                schema: "tenant_default",
                table: "TimesheetProjectAllocations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetProjectAllocations_ProjectId",
                schema: "tenant_default",
                table: "TimesheetProjectAllocations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetProjectAllocations_TimesheetEntryId",
                schema: "tenant_default",
                table: "TimesheetProjectAllocations",
                column: "TimesheetEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPatterns_EmployeeId",
                schema: "tenant_default",
                table: "WorkPatterns",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPatterns_ProjectId",
                schema: "tenant_default",
                table: "WorkPatterns",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectAllocationSuggestions",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "ProjectMembers",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "TimesheetIntelligenceEvents",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "WorkPatterns",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "TimesheetProjectAllocations",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "Projects",
                schema: "tenant_default");
        }
    }
}
