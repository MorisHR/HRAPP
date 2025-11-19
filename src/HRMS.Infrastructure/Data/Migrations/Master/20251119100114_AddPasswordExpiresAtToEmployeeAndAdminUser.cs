using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddPasswordExpiresAtToEmployeeAndAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivationResendLogs",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Tenant ID (enables per-tenant rate limiting)"),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When resend was requested (UTC) - used for sliding window rate limits"),
                    RequestedFromIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true, comment: "IP address of requester (IPv4 or IPv6) for security monitoring"),
                    RequestedByEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Email address used in request (must match tenant email)"),
                    TokenGenerated = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "New token generated (truncated for security - never full token!)"),
                    TokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Token expiration timestamp (UTC) - typically 24 hours from RequestedAt"),
                    Success = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Was resend successful? (false if rate limited or email send failed)"),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Failure reason if Success=false (rate limit, email error, validation failure, etc.)"),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "User agent string for fraud detection"),
                    DeviceInfo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Parsed device info (Mobile/Desktop, browser, OS)"),
                    Geolocation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "City, country for fraud detection"),
                    EmailDelivered = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Was activation email delivered successfully?"),
                    EmailSendError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "SMTP error or bounce reason if delivery failed"),
                    ResendCountLastHour = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Number of resend attempts in last hour (real-time rate limit tracking)"),
                    WasRateLimited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Was this request blocked by rate limiting?"),
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
                    table.PrimaryKey("PK_ActivationResendLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivationResendLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "master",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Fortune 500 activation resend audit log for multi-tenant SaaS. Enables rate limiting (max 3 per hour), security monitoring, and GDPR compliance. IMMUTABLE logs with cascade delete on tenant deletion.");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationResendLogs_IP_RequestedAt",
                schema: "master",
                table: "ActivationResendLogs",
                columns: new[] { "RequestedFromIp", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivationResendLogs_RequestedAt",
                schema: "master",
                table: "ActivationResendLogs",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationResendLogs_RequestedFromIp",
                schema: "master",
                table: "ActivationResendLogs",
                column: "RequestedFromIp");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationResendLogs_Success",
                schema: "master",
                table: "ActivationResendLogs",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationResendLogs_TenantId",
                schema: "master",
                table: "ActivationResendLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationResendLogs_TenantId_RequestedAt",
                schema: "master",
                table: "ActivationResendLogs",
                columns: new[] { "TenantId", "RequestedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivationResendLogs",
                schema: "master");
        }
    }
}
