using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddSubscriptionManagementSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyPrice",
                schema: "master",
                table: "Tenants");

            migrationBuilder.AddColumn<DateTime>(
                name: "GracePeriodStartDate",
                schema: "master",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Grace period start date (when subscription expired)");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastNotificationSent",
                schema: "master",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Last subscription notification sent (timestamp)");

            migrationBuilder.AddColumn<int>(
                name: "LastNotificationType",
                schema: "master",
                table: "Tenants",
                type: "integer",
                nullable: true,
                comment: "Type of last notification sent (prevents duplicates)");

            migrationBuilder.AddColumn<decimal>(
                name: "YearlyPriceMUR",
                schema: "master",
                table: "Tenants",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "Yearly subscription price in Mauritian Rupees (MUR)");

            // Use raw SQL for jsonb conversion with USING clause
            migrationBuilder.Sql(@"
                ALTER TABLE master.""LegalHolds""
                ALTER COLUMN ""UserIds"" TYPE jsonb
                USING CASE
                    WHEN ""UserIds"" IS NULL OR ""UserIds"" = '' THEN NULL
                    ELSE ""UserIds""::jsonb
                END;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                schema: "master",
                table: "LegalHolds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LegalRepresentative",
                schema: "master",
                table: "LegalHolds",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // Use raw SQL for jsonb conversion with USING clause
            migrationBuilder.Sql(@"
                ALTER TABLE master.""LegalHolds""
                ALTER COLUMN ""EntityTypes"" TYPE jsonb
                USING CASE
                    WHEN ""EntityTypes"" IS NULL OR ""EntityTypes"" = '' THEN NULL
                    ELSE ""EntityTypes""::jsonb
                END;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "master",
                table: "LegalHolds",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CourtOrder",
                schema: "master",
                table: "LegalHolds",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CaseNumber",
                schema: "master",
                table: "LegalHolds",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                schema: "master",
                table: "DetectedAnomalies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                schema: "master",
                table: "DetectedAnomalies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                schema: "master",
                table: "DetectedAnomalies",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Evidence",
                schema: "master",
                table: "DetectedAnomalies",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "DetectionRule",
                schema: "master",
                table: "DetectedAnomalies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "master",
                table: "DetectedAnomalies",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "SubscriptionPayments",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Foreign key to Tenant"),
                    PeriodStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Subscription period start date"),
                    PeriodEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Subscription period end date (usually +365 days)"),
                    AmountMUR = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, comment: "Amount in Mauritian Rupees (MUR) - decimal(18,2)"),
                    SubtotalMUR = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmountMUR = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalMUR = table.Column<decimal>(type: "numeric", nullable: false),
                    IsTaxExempt = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, comment: "Payment status (Pending, Paid, Overdue, Failed, etc.)"),
                    PaidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Date when payment was marked as paid"),
                    ProcessedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "SuperAdmin who confirmed the payment (audit trail)"),
                    PaymentReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Invoice, receipt, or bank transaction reference"),
                    PaymentMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Payment method (Bank Transfer, Cash, Cheque, etc.)"),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Payment due date for grace period calculations"),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "Additional notes about the payment"),
                    EmployeeTier = table.Column<int>(type: "integer", nullable: false, comment: "Employee tier at time of payment (audit trail)"),
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
                    table.PrimaryKey("PK_SubscriptionPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionPayments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "master",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Production-grade yearly subscription payment history. IMMUTABLE - payments are historical records. Manual payment processing by SuperAdmin with full audit trail. Indexed on TenantId, PaymentDate, Status for fast queries.");

            migrationBuilder.CreateTable(
                name: "SubscriptionNotificationLogs",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Foreign key to Tenant"),
                    NotificationType = table.Column<int>(type: "integer", nullable: false, comment: "Type of notification sent (30d, 15d, 7d, expiry, etc.)"),
                    SentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Date/time when notification was sent (UTC)"),
                    RecipientEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Recipient email address (audit trail)"),
                    EmailSubject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Email subject line"),
                    DeliverySuccess = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether email was delivered successfully"),
                    DeliveryError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Error message if delivery failed"),
                    SubscriptionEndDateAtNotification = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Subscription end date at time of notification (audit trail)"),
                    DaysUntilExpiryAtNotification = table.Column<int>(type: "integer", nullable: false, comment: "Days until expiry at time of notification (audit trail)"),
                    SubscriptionPaymentId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Related subscription payment ID (if applicable)"),
                    RequiresFollowUp = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Does this notification require follow-up action?"),
                    FollowUpCompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When follow-up action was completed"),
                    FollowUpNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Notes about follow-up action taken"),
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
                    table.PrimaryKey("PK_SubscriptionNotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionNotificationLogs_SubscriptionPayments_Subscript~",
                        column: x => x.SubscriptionPaymentId,
                        principalSchema: "master",
                        principalTable: "SubscriptionPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionNotificationLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "master",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Production-grade subscription notification audit log. Prevents duplicate email sends (Stripe, Chargebee pattern). IMMUTABLE - logs are historical records for compliance. Indexed on TenantId, NotificationType, SentDate for fast duplicate checks.");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Status_SubscriptionEndDate",
                schema: "master",
                table: "Tenants",
                columns: new[] { "Status", "SubscriptionEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_SubscriptionEndDate",
                schema: "master",
                table: "Tenants",
                column: "SubscriptionEndDate");

            migrationBuilder.CreateIndex(
                name: "IX_LegalHolds_CaseNumber",
                schema: "master",
                table: "LegalHolds",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalHolds_CreatedAt",
                schema: "master",
                table: "LegalHolds",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LegalHolds_Status",
                schema: "master",
                table: "LegalHolds",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LegalHolds_TenantId",
                schema: "master",
                table: "LegalHolds",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedAnomalies_AnomalyType",
                schema: "master",
                table: "DetectedAnomalies",
                column: "AnomalyType");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedAnomalies_DetectedAt",
                schema: "master",
                table: "DetectedAnomalies",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedAnomalies_RiskLevel",
                schema: "master",
                table: "DetectedAnomalies",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedAnomalies_Status",
                schema: "master",
                table: "DetectedAnomalies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedAnomalies_Status_DetectedAt",
                schema: "master",
                table: "DetectedAnomalies",
                columns: new[] { "Status", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DetectedAnomalies_TenantId",
                schema: "master",
                table: "DetectedAnomalies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedAnomalies_TenantId_DetectedAt",
                schema: "master",
                table: "DetectedAnomalies",
                columns: new[] { "TenantId", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionNotificationLogs_NotificationType",
                schema: "master",
                table: "SubscriptionNotificationLogs",
                column: "NotificationType");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionNotificationLogs_RequiresFollowUp",
                schema: "master",
                table: "SubscriptionNotificationLogs",
                columns: new[] { "RequiresFollowUp", "FollowUpCompletedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionNotificationLogs_SentDate",
                schema: "master",
                table: "SubscriptionNotificationLogs",
                column: "SentDate");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionNotificationLogs_SubscriptionPaymentId",
                schema: "master",
                table: "SubscriptionNotificationLogs",
                column: "SubscriptionPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionNotificationLogs_TenantId",
                schema: "master",
                table: "SubscriptionNotificationLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionNotificationLogs_TenantId_SentDate",
                schema: "master",
                table: "SubscriptionNotificationLogs",
                columns: new[] { "TenantId", "SentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionNotificationLogs_TenantId_Type_SentDate",
                schema: "master",
                table: "SubscriptionNotificationLogs",
                columns: new[] { "TenantId", "NotificationType", "SentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_DueDate",
                schema: "master",
                table: "SubscriptionPayments",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_PaidDate",
                schema: "master",
                table: "SubscriptionPayments",
                column: "PaidDate");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_Status",
                schema: "master",
                table: "SubscriptionPayments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_TenantId",
                schema: "master",
                table: "SubscriptionPayments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_TenantId_PeriodStartDate",
                schema: "master",
                table: "SubscriptionPayments",
                columns: new[] { "TenantId", "PeriodStartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPayments_TenantId_Status",
                schema: "master",
                table: "SubscriptionPayments",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionNotificationLogs",
                schema: "master");

            migrationBuilder.DropTable(
                name: "SubscriptionPayments",
                schema: "master");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_Status_SubscriptionEndDate",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_SubscriptionEndDate",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_LegalHolds_CaseNumber",
                schema: "master",
                table: "LegalHolds");

            migrationBuilder.DropIndex(
                name: "IX_LegalHolds_CreatedAt",
                schema: "master",
                table: "LegalHolds");

            migrationBuilder.DropIndex(
                name: "IX_LegalHolds_Status",
                schema: "master",
                table: "LegalHolds");

            migrationBuilder.DropIndex(
                name: "IX_LegalHolds_TenantId",
                schema: "master",
                table: "LegalHolds");

            migrationBuilder.DropIndex(
                name: "IX_DetectedAnomalies_AnomalyType",
                schema: "master",
                table: "DetectedAnomalies");

            migrationBuilder.DropIndex(
                name: "IX_DetectedAnomalies_DetectedAt",
                schema: "master",
                table: "DetectedAnomalies");

            migrationBuilder.DropIndex(
                name: "IX_DetectedAnomalies_RiskLevel",
                schema: "master",
                table: "DetectedAnomalies");

            migrationBuilder.DropIndex(
                name: "IX_DetectedAnomalies_Status",
                schema: "master",
                table: "DetectedAnomalies");

            migrationBuilder.DropIndex(
                name: "IX_DetectedAnomalies_Status_DetectedAt",
                schema: "master",
                table: "DetectedAnomalies");

            migrationBuilder.DropIndex(
                name: "IX_DetectedAnomalies_TenantId",
                schema: "master",
                table: "DetectedAnomalies");

            migrationBuilder.DropIndex(
                name: "IX_DetectedAnomalies_TenantId_DetectedAt",
                schema: "master",
                table: "DetectedAnomalies");

            migrationBuilder.DropColumn(
                name: "GracePeriodStartDate",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LastNotificationSent",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LastNotificationType",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "YearlyPriceMUR",
                schema: "master",
                table: "Tenants");

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPrice",
                schema: "master",
                table: "Tenants",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "UserIds",
                schema: "master",
                table: "LegalHolds",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                schema: "master",
                table: "LegalHolds",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "LegalRepresentative",
                schema: "master",
                table: "LegalHolds",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityTypes",
                schema: "master",
                table: "LegalHolds",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "master",
                table: "LegalHolds",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "CourtOrder",
                schema: "master",
                table: "LegalHolds",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CaseNumber",
                schema: "master",
                table: "LegalHolds",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                schema: "master",
                table: "DetectedAnomalies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                schema: "master",
                table: "DetectedAnomalies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                schema: "master",
                table: "DetectedAnomalies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Evidence",
                schema: "master",
                table: "DetectedAnomalies",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "DetectionRule",
                schema: "master",
                table: "DetectedAnomalies",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "master",
                table: "DetectedAnomalies",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);
        }
    }
}
