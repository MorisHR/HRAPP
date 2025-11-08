using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddTimesheetManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Timesheets",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodType = table.Column<int>(type: "integer", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalRegularHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TotalOvertimeHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TotalHolidayHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TotalSickLeaveHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TotalAnnualLeaveHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TotalAbsentHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_Timesheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheets_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetComments",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CommentedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_TimesheetComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimesheetComments_Timesheets_TimesheetId",
                        column: x => x.TimesheetId,
                        principalSchema: "tenant_default",
                        principalTable: "Timesheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetEntries",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttendanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClockInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClockOutTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BreakDuration = table.Column<int>(type: "integer", nullable: false),
                    ActualHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    RegularHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    HolidayHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SickLeaveHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    AnnualLeaveHours = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    IsAbsent = table.Column<bool>(type: "boolean", nullable: false),
                    IsHoliday = table.Column<bool>(type: "boolean", nullable: false),
                    IsWeekend = table.Column<bool>(type: "boolean", nullable: false),
                    IsOnLeave = table.Column<bool>(type: "boolean", nullable: false),
                    DayType = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_TimesheetEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimesheetEntries_Attendances_AttendanceId",
                        column: x => x.AttendanceId,
                        principalSchema: "tenant_default",
                        principalTable: "Attendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimesheetEntries_Timesheets_TimesheetId",
                        column: x => x.TimesheetId,
                        principalSchema: "tenant_default",
                        principalTable: "Timesheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetAdjustments",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentType = table.Column<int>(type: "integer", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AdjustedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AdjustedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_TimesheetAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimesheetAdjustments_TimesheetEntries_TimesheetEntryId",
                        column: x => x.TimesheetEntryId,
                        principalSchema: "tenant_default",
                        principalTable: "TimesheetEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetAdjustments_AdjustedBy",
                schema: "tenant_default",
                table: "TimesheetAdjustments",
                column: "AdjustedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetAdjustments_Status",
                schema: "tenant_default",
                table: "TimesheetAdjustments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetAdjustments_TimesheetEntryId",
                schema: "tenant_default",
                table: "TimesheetAdjustments",
                column: "TimesheetEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetComments_CommentedAt",
                schema: "tenant_default",
                table: "TimesheetComments",
                column: "CommentedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetComments_TimesheetId",
                schema: "tenant_default",
                table: "TimesheetComments",
                column: "TimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_AttendanceId",
                schema: "tenant_default",
                table: "TimesheetEntries",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_Date",
                schema: "tenant_default",
                table: "TimesheetEntries",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_TimesheetId_Date",
                schema: "tenant_default",
                table: "TimesheetEntries",
                columns: new[] { "TimesheetId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_EmployeeId_PeriodStart_PeriodEnd",
                schema: "tenant_default",
                table: "Timesheets",
                columns: new[] { "EmployeeId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_PeriodStart_PeriodEnd",
                schema: "tenant_default",
                table: "Timesheets",
                columns: new[] { "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_Status",
                schema: "tenant_default",
                table: "Timesheets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_TenantId",
                schema: "tenant_default",
                table: "Timesheets",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimesheetAdjustments",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "TimesheetComments",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "TimesheetEntries",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "Timesheets",
                schema: "tenant_default");
        }
    }
}
