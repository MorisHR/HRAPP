using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddFortune500ComplianceFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUnderLegalHold",
                schema: "master",
                table: "AuditLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LegalHoldId",
                schema: "master",
                table: "AuditLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DetectedAnomalies",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnomalyType = table.Column<int>(type: "integer", nullable: false),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserEmail = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: false),
                    RelatedAuditLogIds = table.Column<string>(type: "text", nullable: true),
                    DetectionRule = table.Column<string>(type: "text", nullable: false),
                    ModelVersion = table.Column<string>(type: "text", nullable: true),
                    InvestigatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    InvestigatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvestigationNotes = table.Column<string>(type: "text", nullable: true),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotificationRecipients = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectedAnomalies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalHolds",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CaseNumber = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UserIds = table.Column<string>(type: "text", nullable: true),
                    EntityTypes = table.Column<string>(type: "text", nullable: true),
                    SearchKeywords = table.Column<string>(type: "text", nullable: true),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalRepresentative = table.Column<string>(type: "text", nullable: true),
                    LegalRepresentativeEmail = table.Column<string>(type: "text", nullable: true),
                    LawFirm = table.Column<string>(type: "text", nullable: true),
                    CourtOrder = table.Column<string>(type: "text", nullable: true),
                    ReleasedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleaseNotes = table.Column<string>(type: "text", nullable: true),
                    AffectedAuditLogCount = table.Column<int>(type: "integer", nullable: false),
                    AffectedEntityCount = table.Column<int>(type: "integer", nullable: false),
                    NotifiedUsers = table.Column<string>(type: "text", nullable: true),
                    NotificationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ComplianceFrameworks = table.Column<string>(type: "text", nullable: true),
                    RetentionPeriodDays = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    AdditionalMetadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalHolds", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectedAnomalies",
                schema: "master");

            migrationBuilder.DropTable(
                name: "LegalHolds",
                schema: "master");

            migrationBuilder.DropColumn(
                name: "IsUnderLegalHold",
                schema: "master",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "LegalHoldId",
                schema: "master",
                table: "AuditLogs");
        }
    }
}
