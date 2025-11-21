using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddTenantManagementEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountManagerEmail",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountManagerId",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountManagerName",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessDescription",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClonedFromTenantId",
                schema: "master",
                table: "Tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanySize",
                schema: "master",
                table: "Tenants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractEndDate",
                schema: "master",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractNumber",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContractStartDate",
                schema: "master",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractTerms",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CustomBrandingEnabled",
                schema: "master",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CustomDomain",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomNotes",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomSLA",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataResetReason",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactEmail",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactRole",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HealthScore",
                schema: "master",
                table: "Tenants",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HealthScoreFactors",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HealthScoreLastCalculated",
                schema: "master",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Industry",
                schema: "master",
                table: "Tenants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InfrastructureRegion",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClone",
                schema: "master",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemo",
                schema: "master",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastDataResetBy",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDataResetDate",
                schema: "master",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastImpersonatedAt",
                schema: "master",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastImpersonatedBy",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MigrationNotes",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryUseCase",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProvisioningCompletedAt",
                schema: "master",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProvisioningError",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProvisioningProgress",
                schema: "master",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProvisioningStartedAt",
                schema: "master",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProvisioningStatus",
                schema: "master",
                table: "Tenants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
                schema: "master",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalImpersonationCount",
                schema: "master",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FileUploadLogs",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    Module = table.Column<string>(type: "text", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    StorageClass = table.Column<string>(type: "text", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByEmail = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AccessCount = table.Column<int>(type: "integer", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PermanentlyDeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FileHash = table.Column<string>(type: "text", nullable: true),
                    IsDuplicate = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "text", nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    IsEncrypted = table.Column<bool>(type: "boolean", nullable: false),
                    ScanStatus = table.Column<int>(type: "integer", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MonthlyCostUSD = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileUploadLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileUploadLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "master",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StorageAlerts",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AlertType = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CurrentUsageGB = table.Column<decimal>(type: "numeric", nullable: false),
                    QuotaGB = table.Column<decimal>(type: "numeric", nullable: false),
                    UsagePercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    ThresholdPercentage = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    AcknowledgementNotes = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionMethod = table.Column<string>(type: "text", nullable: true),
                    TimeToResolveMinutes = table.Column<int>(type: "integer", nullable: true),
                    EmailSent = table.Column<bool>(type: "boolean", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailRecipients = table.Column<string>(type: "text", nullable: true),
                    InAppNotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendedActions = table.Column<string>(type: "text", nullable: true),
                    PredictedDaysUntilFull = table.Column<int>(type: "integer", nullable: true),
                    RecurrenceCount = table.Column<int>(type: "integer", nullable: false),
                    LastRecurrenceAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_StorageAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageAlerts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "master",
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TenantHealthHistories",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Tenant this health record belongs to"),
                    HealthScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, comment: "Health score 0-100"),
                    Severity = table.Column<int>(type: "integer", nullable: false, comment: "Severity level based on score (enum)"),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When this health score was calculated (UTC)"),
                    ScoreChange = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true, comment: "Change from previous score (+/-)"),
                    PreviousScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true, comment: "Previous health score for comparison"),
                    HealthScoreFactors = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}", comment: "Breakdown of factors: uptime, performance, errors, activity"),
                    IssuesDetected = table.Column<string>(type: "jsonb", nullable: true, comment: "JSON array of detected issues"),
                    CriticalIssueCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Count of critical issues"),
                    WarningCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Count of warnings"),
                    RecommendedActions = table.Column<string>(type: "jsonb", nullable: true, comment: "JSON array of recommended actions"),
                    AlertSent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Was an alert sent to tenant admin?"),
                    AlertType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Alert type if sent (email, in-app, etc.)"),
                    AcknowledgedByAdmin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Has admin acknowledged the health issue?"),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When admin acknowledged the health issue"),
                    AutoRemediationAttempted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Was automatic remediation attempted?"),
                    AutoRemediationResult = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "Result of auto-remediation"),
                    TenantStatusAtCheck = table.Column<int>(type: "integer", nullable: false, comment: "Tenant subscription status at time of check"),
                    ActiveUsersAtCheck = table.Column<int>(type: "integer", nullable: true, comment: "Number of active users at time of check"),
                    ApiCallVolume24h = table.Column<int>(type: "integer", nullable: true, comment: "API call volume in last 24h"),
                    StorageUsagePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true, comment: "Storage usage percentage at time of check"),
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
                    table.PrimaryKey("PK_TenantHealthHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantHealthHistories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "master",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "TIER 1 - Tenant health score history for predictive analytics. Tracks health score changes over time for trend analysis. Enables proactive intervention before critical failures.");

            migrationBuilder.CreateTable(
                name: "TenantImpersonationLogs",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Tenant being impersonated"),
                    SuperAdminUserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "SuperAdmin user ID who initiated impersonation"),
                    SuperAdminUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "SuperAdmin username for quick reference"),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When impersonation started (UTC)"),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When impersonation ended (null if still active)"),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true, comment: "Duration in seconds (computed on end)"),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Business justification for impersonation (COMPLIANCE)"),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false, comment: "IP address of superadmin (IPv4 or IPv6)"),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "User agent of superadmin"),
                    ActionsPerformed = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]", comment: "JSON array of ImpersonationAction enums"),
                    DataModified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Was any data modified during impersonation? (HIGH RISK)"),
                    DataExported = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Was any data exported during impersonation? (GDPR)"),
                    ActivityLog = table.Column<string>(type: "jsonb", nullable: true, comment: "Detailed activity log - timestamped actions for forensics"),
                    RiskScore = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Risk score 0-100 (ML-based anomaly detection)"),
                    FlaggedBySecurity = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Was this session flagged by security systems?"),
                    SecurityFlagReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Security flag reason"),
                    WasForcedLogout = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Session ended normally or forced logout?"),
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
                    table.PrimaryKey("PK_TenantImpersonationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantImpersonationLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "master",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "TIER 1 - Critical security audit trail for superadmin impersonations. Tracks every impersonation session with complete activity log. Required for SOX, GDPR, ISO 27001 compliance. IMMUTABLE - no UPDATE/DELETE allowed (enforced by triggers).");

            migrationBuilder.CreateTable(
                name: "TenantStorageSnapshots",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalStorageGB = table.Column<decimal>(type: "numeric", nullable: false),
                    DatabaseStorageGB = table.Column<decimal>(type: "numeric", nullable: false),
                    FileStorageGB = table.Column<decimal>(type: "numeric", nullable: false),
                    BackupStorageGB = table.Column<decimal>(type: "numeric", nullable: false),
                    QuotaGB = table.Column<decimal>(type: "numeric", nullable: false),
                    UsagePercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalFiles = table.Column<int>(type: "integer", nullable: false),
                    ActiveFiles = table.Column<int>(type: "integer", nullable: false),
                    DeletedFiles = table.Column<int>(type: "integer", nullable: false),
                    DuplicateFiles = table.Column<int>(type: "integer", nullable: false),
                    DuplicateStorageGB = table.Column<decimal>(type: "numeric", nullable: false),
                    GrowthGB = table.Column<decimal>(type: "numeric", nullable: false),
                    GrowthPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    AvgGrowthRate7Day = table.Column<decimal>(type: "numeric", nullable: false),
                    AvgGrowthRate30Day = table.Column<decimal>(type: "numeric", nullable: false),
                    PredictedDaysUntilFull = table.Column<int>(type: "integer", nullable: true),
                    StorageByModule = table.Column<string>(type: "text", nullable: true),
                    StorageByFileType = table.Column<string>(type: "text", nullable: true),
                    LargestFiles = table.Column<string>(type: "text", nullable: true),
                    MonthlyCostUSD = table.Column<decimal>(type: "numeric", nullable: false),
                    CostChangeUSD = table.Column<decimal>(type: "numeric", nullable: false),
                    GenerationDurationMs = table.Column<int>(type: "integer", nullable: false),
                    DataQualityScore = table.Column<int>(type: "integer", nullable: false),
                    Errors = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_TenantStorageSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantStorageSnapshots_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "master",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileUploadLogs_TenantId",
                schema: "master",
                table: "FileUploadLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageAlerts_TenantId",
                schema: "master",
                table: "StorageAlerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantHealthHistories_Alerts",
                schema: "master",
                table: "TenantHealthHistories",
                columns: new[] { "AlertSent", "AcknowledgedByAdmin", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantHealthHistories_CalculatedAt",
                schema: "master",
                table: "TenantHealthHistories",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantHealthHistories_Severity",
                schema: "master",
                table: "TenantHealthHistories",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_TenantHealthHistories_Severity_CalculatedAt",
                schema: "master",
                table: "TenantHealthHistories",
                columns: new[] { "Severity", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantHealthHistories_TenantId",
                schema: "master",
                table: "TenantHealthHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantHealthHistories_TenantId_CalculatedAt",
                schema: "master",
                table: "TenantHealthHistories",
                columns: new[] { "TenantId", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantImpersonationLogs_DataModified_StartedAt",
                schema: "master",
                table: "TenantImpersonationLogs",
                columns: new[] { "DataModified", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantImpersonationLogs_EndedAt",
                schema: "master",
                table: "TenantImpersonationLogs",
                column: "EndedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantImpersonationLogs_FlaggedBySecurity_StartedAt",
                schema: "master",
                table: "TenantImpersonationLogs",
                columns: new[] { "FlaggedBySecurity", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantImpersonationLogs_StartedAt",
                schema: "master",
                table: "TenantImpersonationLogs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantImpersonationLogs_SuperAdminUserId",
                schema: "master",
                table: "TenantImpersonationLogs",
                column: "SuperAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantImpersonationLogs_SuperAdminUserId_StartedAt",
                schema: "master",
                table: "TenantImpersonationLogs",
                columns: new[] { "SuperAdminUserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantImpersonationLogs_TenantId",
                schema: "master",
                table: "TenantImpersonationLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantImpersonationLogs_TenantId_StartedAt",
                schema: "master",
                table: "TenantImpersonationLogs",
                columns: new[] { "TenantId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantStorageSnapshots_TenantId",
                schema: "master",
                table: "TenantStorageSnapshots",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileUploadLogs",
                schema: "master");

            migrationBuilder.DropTable(
                name: "StorageAlerts",
                schema: "master");

            migrationBuilder.DropTable(
                name: "TenantHealthHistories",
                schema: "master");

            migrationBuilder.DropTable(
                name: "TenantImpersonationLogs",
                schema: "master");

            migrationBuilder.DropTable(
                name: "TenantStorageSnapshots",
                schema: "master");

            migrationBuilder.DropColumn(
                name: "AccountManagerEmail",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AccountManagerId",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AccountManagerName",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "BusinessDescription",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ClonedFromTenantId",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CompanySize",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ContractEndDate",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ContractNumber",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ContractStartDate",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ContractTerms",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CustomBrandingEnabled",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CustomDomain",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CustomNotes",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CustomSLA",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DataResetReason",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EmergencyContactEmail",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EmergencyContactRole",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "HealthScore",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "HealthScoreFactors",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "HealthScoreLastCalculated",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Industry",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "InfrastructureRegion",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsClone",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsDemo",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LastDataResetBy",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LastDataResetDate",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LastImpersonatedAt",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LastImpersonatedBy",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MigrationNotes",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PrimaryUseCase",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ProvisioningCompletedAt",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ProvisioningError",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ProvisioningProgress",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ProvisioningStartedAt",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ProvisioningStatus",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                schema: "master",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TotalImpersonationCount",
                schema: "master",
                table: "Tenants");
        }
    }
}
