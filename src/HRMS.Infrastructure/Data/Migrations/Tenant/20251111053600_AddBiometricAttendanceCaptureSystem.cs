using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddBiometricAttendanceCaptureSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: Employee address changes (Address → AddressLine1, add AddressLine2, Village, District, Region, Country)
            // are handled by migration 20251107041234_UpgradeEmployeeAddressForMauritiusCompliance.
            // Removed duplicate operations to prevent migration conflicts.

            migrationBuilder.CreateTable(
                name: "BiometricPunchRecords",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeviceSerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    PunchTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PunchType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerificationMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerificationQuality = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(11,8)", nullable: true),
                    PhotoPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RawData = table.Column<string>(type: "jsonb", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AttendanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    HashChain = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
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
                    table.PrimaryKey("PK_BiometricPunchRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BiometricPunchRecords_AttendanceMachines_DeviceId",
                        column: x => x.DeviceId,
                        principalSchema: "tenant_default",
                        principalTable: "AttendanceMachines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BiometricPunchRecords_Attendances_AttendanceId",
                        column: x => x.AttendanceId,
                        principalSchema: "tenant_default",
                        principalTable: "Attendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BiometricPunchRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "tenant_default",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DeviceApiKeys",
                schema: "tenant_default",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKeyHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    AllowedIpAddresses = table.Column<string>(type: "jsonb", nullable: true),
                    RateLimitPerMinute = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceApiKeys_AttendanceMachines_DeviceId",
                        column: x => x.DeviceId,
                        principalSchema: "tenant_default",
                        principalTable: "AttendanceMachines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_AttendanceId",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_DeviceId",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_DeviceId_DeviceUserId_PunchTime",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                columns: new[] { "DeviceId", "DeviceUserId", "PunchTime" });

            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_EmployeeId",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_EmployeeId_PunchTime",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                columns: new[] { "EmployeeId", "PunchTime" });

            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_HashChain",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                column: "HashChain");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_ProcessingStatus",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_ProcessingStatus_PunchTime",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                columns: new[] { "ProcessingStatus", "PunchTime" });

            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_PunchTime",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                column: "PunchTime");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_TenantId",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricPunchRecords_TenantId_DeviceId_PunchTime",
                schema: "tenant_default",
                table: "BiometricPunchRecords",
                columns: new[] { "TenantId", "DeviceId", "PunchTime" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceApiKeys_ApiKeyHash",
                schema: "tenant_default",
                table: "DeviceApiKeys",
                column: "ApiKeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceApiKeys_DeviceId",
                schema: "tenant_default",
                table: "DeviceApiKeys",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceApiKeys_DeviceId_IsActive",
                schema: "tenant_default",
                table: "DeviceApiKeys",
                columns: new[] { "DeviceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceApiKeys_ExpiresAt",
                schema: "tenant_default",
                table: "DeviceApiKeys",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceApiKeys_IsActive_ExpiresAt",
                schema: "tenant_default",
                table: "DeviceApiKeys",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceApiKeys_LastUsedAt",
                schema: "tenant_default",
                table: "DeviceApiKeys",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceApiKeys_TenantId",
                schema: "tenant_default",
                table: "DeviceApiKeys",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BiometricPunchRecords",
                schema: "tenant_default");

            migrationBuilder.DropTable(
                name: "DeviceApiKeys",
                schema: "tenant_default");

            // NOTE: Employee address rollback (drop AddressLine2, Village, District, Region, Country, rename AddressLine1 → Address)
            // is handled by migration 20251107041234_UpgradeEmployeeAddressForMauritiusCompliance.
            // Removed duplicate operations to prevent migration conflicts.
        }
    }
}
