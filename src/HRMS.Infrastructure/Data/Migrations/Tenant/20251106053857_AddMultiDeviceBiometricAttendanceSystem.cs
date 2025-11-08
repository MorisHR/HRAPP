using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddMultiDeviceBiometricAttendanceSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Location",
                schema: "tenant_default",
                table: "AttendanceMachines",
                newName: "LegacyLocation");

            migrationBuilder.AddColumn<DateTime>(
                name: "BiometricEnrollmentDate",
                schema: "tenant_default",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BiometricEnrollmentId",
                schema: "tenant_default",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrimaryLocationId",
                schema: "tenant_default",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCenterCode",
                schema: "tenant_default",
                table: "Departments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorizationNote",
                schema: "tenant_default",
                table: "Attendances",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceId",
                schema: "tenant_default",
                table: "Attendances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceUserId",
                schema: "tenant_default",
                table: "Attendances",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAuthorized",
                schema: "tenant_default",
                table: "Attendances",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                schema: "tenant_default",
                table: "Attendances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PunchSource",
                schema: "tenant_default",
                table: "Attendances",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VerificationMethod",
                schema: "tenant_default",
                table: "Attendances",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Port",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConnectionMethod",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ConnectionTimeoutSeconds",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeviceCode",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceConfigJson",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceStatus",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirmwareVersion",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastSyncRecordCount",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncStatus",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncTime",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MacAddress",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OfflineAlertEnabled",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OfflineThresholdMinutes",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SyncEnabled",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SyncIntervalMinutes",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DeviceSyncLogs",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SyncStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SyncEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SyncDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    SyncStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RecordsFetched = table.Column<int>(type: "integer", nullable: false),
                    RecordsProcessed = table.Column<int>(type: "integer", nullable: false),
                    RecordsInserted = table.Column<int>(type: "integer", nullable: false),
                    RecordsUpdated = table.Column<int>(type: "integer", nullable: false),
                    RecordsSkipped = table.Column<int>(type: "integer", nullable: false),
                    RecordsErrored = table.Column<int>(type: "integer", nullable: false),
                    SyncMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DateRangeFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateRangeTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ErrorDetailsJson = table.Column<string>(type: "jsonb", nullable: true),
                    InitiatedBy = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_DeviceSyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceSyncLogs_AttendanceMachines_DeviceId",
                        column: x => x.DeviceId,
                        principalSchema: "tenant_default",
                        principalTable: "AttendanceMachines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDeviceAccesses",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccessReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AllowedDaysJson = table.Column<string>(type: "jsonb", nullable: true),
                    AllowedTimeStart = table.Column<TimeSpan>(type: "interval", nullable: true),
                    AllowedTimeEnd = table.Column<TimeSpan>(type: "interval", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeDeviceAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDeviceAccesses_AttendanceMachines_DeviceId",
                        column: x => x.DeviceId,
                        principalSchema: "tenant_default",
                        principalTable: "AttendanceMachines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeDeviceAccesses_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LocationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LocationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AddressLine1 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddressLine2 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WorkingHoursJson = table.Column<string>(type: "jsonb", nullable: true),
                    Timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LocationManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CapacityHeadcount = table.Column<int>(type: "integer", nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(11,8)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_Employees_LocationManagerId",
                        column: x => x.LocationManagerId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceAnomalies",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnomalyType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AnomalySeverity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AnomalyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnomalyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnomalyDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AnomalyDetailsJson = table.Column<string>(type: "jsonb", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpectedLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResolutionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    NotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotificationRecipientsJson = table.Column<string>(type: "jsonb", nullable: true),
                    AutoResolved = table.Column<bool>(type: "boolean", nullable: false),
                    AutoResolutionRule = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_AttendanceAnomalies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceAnomalies_AttendanceMachines_DeviceId",
                        column: x => x.DeviceId,
                        principalSchema: "tenant_default",
                        principalTable: "AttendanceMachines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AttendanceAnomalies_Attendances_AttendanceId",
                        column: x => x.AttendanceId,
                        principalSchema: "tenant_default",
                        principalTable: "Attendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceAnomalies_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceAnomalies_Locations_ExpectedLocationId",
                        column: x => x.ExpectedLocationId,
                        principalSchema: "tenant_default",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AttendanceAnomalies_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "tenant_default",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PrimaryLocationId",
                schema: "tenant_default",
                table: "Employees",
                column: "PrimaryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_DeviceId",
                schema: "tenant_default",
                table: "Attendances",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EmployeeId_DeviceId_Date",
                schema: "tenant_default",
                table: "Attendances",
                columns: new[] { "EmployeeId", "DeviceId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_LocationId",
                schema: "tenant_default",
                table: "Attendances",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceMachines_DeviceCode",
                schema: "tenant_default",
                table: "AttendanceMachines",
                column: "DeviceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceMachines_LocationId",
                schema: "tenant_default",
                table: "AttendanceMachines",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceMachines_LocationId_DeviceStatus",
                schema: "tenant_default",
                table: "AttendanceMachines",
                columns: new[] { "LocationId", "DeviceStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAnomalies_AnomalyDate",
                schema: "tenant_default",
                table: "AttendanceAnomalies",
                column: "AnomalyDate");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAnomalies_AnomalyType_AnomalySeverity",
                schema: "tenant_default",
                table: "AttendanceAnomalies",
                columns: new[] { "AnomalyType", "AnomalySeverity" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAnomalies_AttendanceId",
                schema: "tenant_default",
                table: "AttendanceAnomalies",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAnomalies_DeviceId",
                schema: "tenant_default",
                table: "AttendanceAnomalies",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAnomalies_EmployeeId",
                schema: "tenant_default",
                table: "AttendanceAnomalies",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAnomalies_EmployeeId_AnomalyDate",
                schema: "tenant_default",
                table: "AttendanceAnomalies",
                columns: new[] { "EmployeeId", "AnomalyDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAnomalies_ExpectedLocationId",
                schema: "tenant_default",
                table: "AttendanceAnomalies",
                column: "ExpectedLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAnomalies_LocationId",
                schema: "tenant_default",
                table: "AttendanceAnomalies",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAnomalies_ResolutionStatus",
                schema: "tenant_default",
                table: "AttendanceAnomalies",
                column: "ResolutionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSyncLogs_DeviceId",
                schema: "tenant_default",
                table: "DeviceSyncLogs",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSyncLogs_DeviceId_SyncStartTime",
                schema: "tenant_default",
                table: "DeviceSyncLogs",
                columns: new[] { "DeviceId", "SyncStartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSyncLogs_SyncStartTime",
                schema: "tenant_default",
                table: "DeviceSyncLogs",
                column: "SyncStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSyncLogs_SyncStatus",
                schema: "tenant_default",
                table: "DeviceSyncLogs",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeviceAccesses_AccessType",
                schema: "tenant_default",
                table: "EmployeeDeviceAccesses",
                column: "AccessType");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeviceAccesses_DeviceId",
                schema: "tenant_default",
                table: "EmployeeDeviceAccesses",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeviceAccesses_EmployeeId_DeviceId_IsActive",
                schema: "tenant_default",
                table: "EmployeeDeviceAccesses",
                columns: new[] { "EmployeeId", "DeviceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeviceAccesses_ValidFrom_ValidUntil",
                schema: "tenant_default",
                table: "EmployeeDeviceAccesses",
                columns: new[] { "ValidFrom", "ValidUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Latitude_Longitude",
                schema: "tenant_default",
                table: "Locations",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_LocationCode",
                schema: "tenant_default",
                table: "Locations",
                column: "LocationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_LocationManagerId",
                schema: "tenant_default",
                table: "Locations",
                column: "LocationManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_LocationName",
                schema: "tenant_default",
                table: "Locations",
                column: "LocationName");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_LocationType",
                schema: "tenant_default",
                table: "Locations",
                column: "LocationType");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceMachines_Locations_LocationId",
                schema: "tenant_default",
                table: "AttendanceMachines",
                column: "LocationId",
                principalSchema: "tenant_default",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_AttendanceMachines_DeviceId",
                schema: "tenant_default",
                table: "Attendances",
                column: "DeviceId",
                principalSchema: "tenant_default",
                principalTable: "AttendanceMachines",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Locations_LocationId",
                schema: "tenant_default",
                table: "Attendances",
                column: "LocationId",
                principalSchema: "tenant_default",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Locations_PrimaryLocationId",
                schema: "tenant_default",
                table: "Employees",
                column: "PrimaryLocationId",
                principalSchema: "tenant_default",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceMachines_Locations_LocationId",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_AttendanceMachines_DeviceId",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Locations_LocationId",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Locations_PrimaryLocationId",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "AttendanceAnomalies",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "DeviceSyncLogs",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "EmployeeDeviceAccesses",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "Locations",
                schema: "tenant_default");

            migrationBuilder.DropIndex(
                name: "IX_Employees_PrimaryLocationId",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_DeviceId",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_EmployeeId_DeviceId_Date",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_LocationId",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceMachines_DeviceCode",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceMachines_LocationId",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceMachines_LocationId_DeviceStatus",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "BiometricEnrollmentDate",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BiometricEnrollmentId",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PrimaryLocationId",
                schema: "tenant_default",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CostCenterCode",
                schema: "tenant_default",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "AuthorizationNote",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "DeviceUserId",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "IsAuthorized",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "LocationId",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "PunchSource",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "VerificationMethod",
                schema: "tenant_default",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ConnectionMethod",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "ConnectionTimeoutSeconds",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "DeviceCode",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "DeviceConfigJson",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "DeviceStatus",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "FirmwareVersion",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "LastSyncRecordCount",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "LastSyncStatus",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "LastSyncTime",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "LocationId",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "MacAddress",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "OfflineAlertEnabled",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "OfflineThresholdMinutes",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "SyncEnabled",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.DropColumn(
                name: "SyncIntervalMinutes",
                schema: "tenant_default",
                table: "AttendanceMachines");

            migrationBuilder.RenameColumn(
                name: "LegacyLocation",
                schema: "tenant_default",
                table: "AttendanceMachines",
                newName: "Location");

            migrationBuilder.AlterColumn<int>(
                name: "Port",
                schema: "tenant_default",
                table: "AttendanceMachines",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
