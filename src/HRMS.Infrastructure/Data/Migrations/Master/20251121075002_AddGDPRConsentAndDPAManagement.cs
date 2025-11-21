using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMS.Infrastructure.Data.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddGDPRConsentAndDPAManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataProcessingAgreements",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorName = table.Column<string>(type: "text", nullable: false),
                    VendorType = table.Column<string>(type: "text", nullable: false),
                    VendorContactName = table.Column<string>(type: "text", nullable: true),
                    VendorContactEmail = table.Column<string>(type: "text", nullable: true),
                    VendorPhone = table.Column<string>(type: "text", nullable: true),
                    VendorAddress = table.Column<string>(type: "text", nullable: true),
                    VendorCountry = table.Column<string>(type: "text", nullable: false),
                    VendorWebsite = table.Column<string>(type: "text", nullable: true),
                    DataProtectionOfficer = table.Column<string>(type: "text", nullable: true),
                    DpaReferenceNumber = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAutoRenewal = table.Column<bool>(type: "boolean", nullable: false),
                    NoticePeriodDays = table.Column<int>(type: "integer", nullable: false),
                    DocumentPath = table.Column<string>(type: "text", nullable: true),
                    DocumentMimeType = table.Column<string>(type: "text", nullable: true),
                    DocumentSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    DocumentHash = table.Column<string>(type: "text", nullable: true),
                    ProcessingPurpose = table.Column<string>(type: "text", nullable: false),
                    DataSubjectCategories = table.Column<string>(type: "text", nullable: false),
                    PersonalDataCategories = table.Column<string>(type: "text", nullable: false),
                    SpecialDataCategories = table.Column<string>(type: "text", nullable: true),
                    ProcessesSensitiveData = table.Column<bool>(type: "boolean", nullable: false),
                    RetentionPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    DataDisposalMethod = table.Column<string>(type: "text", nullable: false),
                    InternationalDataTransfer = table.Column<bool>(type: "boolean", nullable: false),
                    TransferCountries = table.Column<string>(type: "text", nullable: true),
                    TransferMechanism = table.Column<string>(type: "text", nullable: true),
                    AdequacyDecisionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    LastRiskAssessmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRiskAssessmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Certifications = table.Column<string>(type: "text", nullable: true),
                    SecurityMeasures = table.Column<string>(type: "text", nullable: true),
                    BreachNotificationHours = table.Column<int>(type: "integer", nullable: false),
                    AllowsSubProcessors = table.Column<bool>(type: "boolean", nullable: false),
                    AuthorizedSubProcessors = table.Column<string>(type: "text", nullable: true),
                    RequiresPriorConsentForSubProcessors = table.Column<bool>(type: "boolean", nullable: false),
                    AuditRights = table.Column<string>(type: "text", nullable: false),
                    LastAuditDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextAuditDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuditNotes = table.Column<string>(type: "text", nullable: true),
                    AnnualValueUsd = table.Column<decimal>(type: "numeric", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    PaymentTerms = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "text", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    TerminatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminationReason = table.Column<string>(type: "text", nullable: true),
                    TerminatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProcessingAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataProcessingAgreements_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "master",
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserConsents",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserEmail = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsentType = table.Column<int>(type: "integer", nullable: false),
                    ConsentCategory = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    ConsentText = table.Column<string>(type: "text", nullable: false),
                    ConsentVersion = table.Column<string>(type: "text", nullable: false),
                    ConsentTextHash = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsExplicit = table.Column<bool>(type: "boolean", nullable: false),
                    IsOptIn = table.Column<bool>(type: "boolean", nullable: false),
                    GivenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WithdrawnAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WithdrawalReason = table.Column<string>(type: "text", nullable: true),
                    LegalBasis = table.Column<int>(type: "integer", nullable: false),
                    LegalNotes = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    ConsentMethod = table.Column<string>(type: "text", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: true),
                    CountryCode = table.Column<string>(type: "text", nullable: true),
                    RequiresParentalConsent = table.Column<bool>(type: "boolean", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentalConsentGivenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ThirdParties = table.Column<string>(type: "text", nullable: true),
                    RetentionPeriodDays = table.Column<int>(type: "integer", nullable: true),
                    InternationalTransfer = table.Column<bool>(type: "boolean", nullable: false),
                    TransferCountries = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreviousConsentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataProcessingAgreementId = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConsents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "master",
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataProcessingAgreements_TenantId",
                schema: "master",
                table: "DataProcessingAgreements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_TenantId",
                schema: "master",
                table: "UserConsents",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProcessingAgreements",
                schema: "master");

            migrationBuilder.DropTable(
                name: "UserConsents",
                schema: "master");
        }
    }
}
