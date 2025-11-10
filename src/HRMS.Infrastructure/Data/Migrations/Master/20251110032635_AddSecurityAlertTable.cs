using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddSecurityAlertTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecurityAlerts",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertType = table.Column<int>(type: "integer", nullable: false, comment: "Type of security alert (enum stored as integer)"),
                    Severity = table.Column<int>(type: "integer", nullable: false, comment: "Alert severity level (CRITICAL, EMERGENCY, HIGH, MEDIUM, LOW)"),
                    Category = table.Column<int>(type: "integer", nullable: false, comment: "Alert category for classification"),
                    Status = table.Column<int>(type: "integer", nullable: false, comment: "Alert status (NEW, ACKNOWLEDGED, IN_PROGRESS, RESOLVED, etc.)"),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Alert title/summary"),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false, comment: "Detailed alert description"),
                    RecommendedActions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Recommended actions to address the alert"),
                    RiskScore = table.Column<int>(type: "integer", nullable: false, comment: "Risk score 0-100 calculated by anomaly detection"),
                    AuditLogId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Related audit log entry ID"),
                    AuditActionType = table.Column<int>(type: "integer", nullable: true, comment: "Related audit log action type"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Tenant ID (null for platform-level alerts)"),
                    TenantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Tenant name for reporting"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true, comment: "User ID who triggered the alert"),
                    UserEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "User email address"),
                    UserFullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "User full name"),
                    UserRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "User role at time of alert"),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true, comment: "IP address associated with alert"),
                    Geolocation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Geolocation information"),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "User agent string"),
                    DeviceInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Device information"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When alert was created (UTC)"),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When alert was first detected (UTC)"),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When alert was acknowledged (UTC)"),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When alert was resolved (UTC)"),
                    AcknowledgedBy = table.Column<Guid>(type: "uuid", nullable: true, comment: "User ID who acknowledged the alert"),
                    AcknowledgedByEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Email of user who acknowledged"),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true, comment: "User ID who resolved the alert"),
                    ResolvedByEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Email of user who resolved"),
                    ResolutionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Resolution notes"),
                    AssignedTo = table.Column<Guid>(type: "uuid", nullable: true, comment: "Assigned to user ID"),
                    AssignedToEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Assigned to user email"),
                    EmailSent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether email notification was sent"),
                    EmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Email sent timestamp"),
                    EmailRecipients = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "Email recipients (comma-separated)"),
                    SmsSent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether SMS notification was sent"),
                    SmsSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "SMS sent timestamp"),
                    SmsRecipients = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "SMS recipients (comma-separated)"),
                    SlackSent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether Slack notification was sent"),
                    SlackSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Slack sent timestamp"),
                    SlackChannels = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Slack channels notified"),
                    SiemSent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether SIEM notification was sent"),
                    SiemSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "SIEM sent timestamp"),
                    SiemSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "SIEM system name"),
                    DetectionRule = table.Column<string>(type: "jsonb", nullable: true, comment: "Detection rule that triggered alert (JSON)"),
                    BaselineMetrics = table.Column<string>(type: "jsonb", nullable: true, comment: "Baseline metrics (JSON)"),
                    CurrentMetrics = table.Column<string>(type: "jsonb", nullable: true, comment: "Current metrics that triggered alert (JSON)"),
                    DeviationPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true, comment: "Deviation percentage from baseline"),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Correlation ID for distributed tracing"),
                    AdditionalMetadata = table.Column<string>(type: "jsonb", nullable: true, comment: "Additional metadata (JSON)"),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Tags for categorization"),
                    ComplianceFrameworks = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Related compliance frameworks"),
                    RequiresEscalation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether alert requires escalation"),
                    EscalatedTo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Escalated to (email or system)"),
                    EscalatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Escalation timestamp"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Soft delete flag"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When alert was soft-deleted")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAlerts", x => x.Id);
                },
                comment: "Production-grade security alert system for real-time threat detection. Supports SOX, GDPR, ISO 27001, PCI-DSS compliance. Integrates with Email, SMS, Slack, and SIEM systems.");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_AlertType",
                schema: "master",
                table: "SecurityAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_AuditLogId",
                schema: "master",
                table: "SecurityAlerts",
                column: "AuditLogId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_CreatedAt",
                schema: "master",
                table: "SecurityAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Severity",
                schema: "master",
                table: "SecurityAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Severity_Status_CreatedAt",
                schema: "master",
                table: "SecurityAlerts",
                columns: new[] { "Severity", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Status",
                schema: "master",
                table: "SecurityAlerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_Status_CreatedAt",
                schema: "master",
                table: "SecurityAlerts",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_TenantId",
                schema: "master",
                table: "SecurityAlerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_TenantId_CreatedAt",
                schema: "master",
                table: "SecurityAlerts",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_UserId",
                schema: "master",
                table: "SecurityAlerts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityAlerts",
                schema: "master");
        }
    }
}
