using Microsoft.EntityFrameworkCore;
using HRMS.Core.Entities.Master;

namespace HRMS.Infrastructure.Data;

/// <summary>
/// Master Database Context - handles system-wide entities
/// Schema: master (shared across all tenants)
/// </summary>
public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SecurityAlert> SecurityAlerts { get; set; }
    public DbSet<DetectedAnomaly> DetectedAnomalies { get; set; }
    public DbSet<LegalHold> LegalHolds { get; set; }

    // JWT Refresh Tokens (Production-Grade Security)
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    // Industry Sectors (Mauritius Remuneration Orders)
    public DbSet<IndustrySector> IndustrySectors { get; set; }
    public DbSet<SectorComplianceRule> SectorComplianceRules { get; set; }

    // Mauritius Address Hierarchy
    public DbSet<District> Districts { get; set; }
    public DbSet<Village> Villages { get; set; }
    public DbSet<PostalCode> PostalCodes { get; set; }

    /// <summary>
    /// PRODUCTION-GRADE: Yearly subscription payment history tracking
    /// FORTUNE 500 PATTERN: Manual payment processing with full audit trail
    /// </summary>
    public DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }

    /// <summary>
    /// PRODUCTION-GRADE: Subscription notification log (email deduplication)
    /// FORTUNE 500 PATTERN: Prevents duplicate emails and provides compliance audit trail
    /// </summary>
    public DbSet<SubscriptionNotificationLog> SubscriptionNotificationLogs { get; set; }

    /// <summary>
    /// FORTUNE 500: Feature flags for per-tenant control and gradual rollouts
    /// PATTERN: Canary deployment, emergency rollback, A/B testing
    /// </summary>
    public DbSet<FeatureFlag> FeatureFlagsConfig { get; set; }

    /// <summary>
    /// FORTUNE 500: Activation resend audit log (rate limiting + security monitoring)
    /// PATTERN: Netflix/Stripe audit logging for tenant activation emails
    /// </summary>
    public DbSet<ActivationResendLog> ActivationResendLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Suppress pending model changes warning (for testing optimizations)
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set the schema to "master"
        modelBuilder.HasDefaultSchema("master");

        // Configure Tenant entity
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Subdomain).IsUnique();
            entity.HasIndex(e => e.SchemaName).IsUnique();
            entity.HasIndex(e => e.ContactEmail);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Subdomain).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SchemaName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ContactPhone).HasMaxLength(20);
            entity.Property(e => e.AdminUserName).HasMaxLength(100);
            entity.Property(e => e.AdminEmail).HasMaxLength(100);

            // Query filter to exclude soft-deleted records by default
            entity.HasQueryFilter(t => !t.IsDeleted);
        });

        // Configure AdminUser entity
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);

            // MFA Configuration
            entity.Property(e => e.TwoFactorSecret).HasMaxLength(500);
            entity.Property(e => e.BackupCodes)
                .HasColumnType("jsonb")
                .HasComment("JSON array of SHA256-hashed backup codes for MFA recovery");

            entity.HasQueryFilter(u => !u.IsDeleted);
        });

        // Configure AuditLog entity (Production-Grade Compliance Audit Logging)
        modelBuilder.Entity<AuditLog>(entity =>
        {
            // Table configuration
            entity.ToTable("AuditLogs", schema: "master", tb =>
            {
                tb.HasComment("Production-grade audit log with 10+ year retention. " +
                             "IMMUTABLE - no UPDATE/DELETE allowed (enforced by DB triggers). " +
                             "Partitioned by PerformedAt (monthly). Meets Mauritius compliance requirements.");
            });

            entity.HasKey(e => e.Id);

            // ============================================
            // WHO - User Information
            // ============================================
            entity.Property(e => e.TenantId)
                .HasComment("Tenant ID (null for SuperAdmin platform-level actions)");

            entity.Property(e => e.TenantName)
                .HasMaxLength(200)
                .HasComment("Denormalized tenant name for reporting");

            entity.Property(e => e.UserId)
                .HasComment("User ID who performed the action");

            entity.Property(e => e.UserEmail)
                .HasMaxLength(100)
                .HasComment("User email address");

            entity.Property(e => e.UserFullName)
                .HasMaxLength(200)
                .HasComment("User full name for audit trail readability");

            entity.Property(e => e.UserRole)
                .HasMaxLength(50)
                .HasComment("User role at time of action (SuperAdmin, TenantAdmin, HR, Manager, Employee)");

            entity.Property(e => e.SessionId)
                .HasMaxLength(100)
                .HasComment("Session ID for tracking related actions");

            // ============================================
            // WHAT - Action Information
            // ============================================
            entity.Property(e => e.ActionType)
                .IsRequired()
                .HasComment("Standardized action type (enum stored as integer)");

            entity.Property(e => e.Category)
                .IsRequired()
                .HasComment("High-level category for filtering (enum stored as integer)");

            entity.Property(e => e.Severity)
                .IsRequired()
                .HasComment("Severity level for alerting (enum stored as integer)");

            entity.Property(e => e.EntityType)
                .HasMaxLength(100)
                .HasComment("Entity type affected (Employee, LeaveRequest, Payroll, etc.)");

            entity.Property(e => e.EntityId)
                .HasComment("Entity ID affected (if applicable)");

            entity.Property(e => e.Success)
                .IsRequired()
                .HasComment("Whether the action succeeded");

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000)
                .HasComment("Error message if action failed");

            // ============================================
            // HOW - Change Details
            // ============================================
            entity.Property(e => e.OldValues)
                .HasColumnType("jsonb")
                .HasComment("Old values before change (JSON format)");

            entity.Property(e => e.NewValues)
                .HasColumnType("jsonb")
                .HasComment("New values after change (JSON format)");

            entity.Property(e => e.ChangedFields)
                .HasMaxLength(1000)
                .HasComment("Comma-separated list of changed field names");

            entity.Property(e => e.Reason)
                .HasMaxLength(1000)
                .HasComment("User-provided reason for the action");

            entity.Property(e => e.ApprovalReference)
                .HasMaxLength(100)
                .HasComment("Approval reference if action required approval");

            // ============================================
            // WHERE - Location Information
            // ============================================
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45) // IPv6 max length
                .HasComment("IP address of the user (IPv4 or IPv6)");

            entity.Property(e => e.Geolocation)
                .HasMaxLength(500)
                .HasComment("Geolocation information (city, country, coordinates)");

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500)
                .HasComment("User agent string (browser/device information)");

            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(500)
                .HasComment("Parsed device information (mobile, desktop, tablet, OS, browser)");

            entity.Property(e => e.NetworkInfo)
                .HasMaxLength(500)
                .HasComment("Network information (ISP, organization)");

            // ============================================
            // WHEN - Timestamp Information
            // ============================================
            entity.Property(e => e.PerformedAt)
                .IsRequired()
                .HasComment("Timestamp when action was performed (UTC)");

            entity.Property(e => e.DurationMs)
                .HasComment("Action duration in milliseconds (for performance tracking)");

            entity.Property(e => e.BusinessDate)
                .HasComment("Business date for payroll/leave actions");

            // ============================================
            // WHY - Justification
            // ============================================
            entity.Property(e => e.PolicyReference)
                .HasMaxLength(200)
                .HasComment("Policy reference that triggered this action");

            entity.Property(e => e.DocumentationLink)
                .HasMaxLength(500)
                .HasComment("Link to related documentation or policy");

            // ============================================
            // CONTEXT - Technical Details
            // ============================================
            entity.Property(e => e.HttpMethod)
                .HasMaxLength(10)
                .HasComment("HTTP method (GET, POST, PUT, DELETE)");

            entity.Property(e => e.RequestPath)
                .HasMaxLength(500)
                .HasComment("Request path/endpoint");

            entity.Property(e => e.QueryString)
                .HasMaxLength(2000)
                .HasComment("Query string parameters");

            entity.Property(e => e.ResponseCode)
                .HasComment("HTTP response status code");

            entity.Property(e => e.CorrelationId)
                .HasMaxLength(100)
                .HasComment("Correlation ID for distributed tracing");

            entity.Property(e => e.ParentActionId)
                .HasComment("Parent action ID for multi-step operations");

            entity.Property(e => e.AdditionalMetadata)
                .HasColumnType("jsonb")
                .HasComment("Additional metadata in JSON format (flexible for future extensions)");

            // ============================================
            // SECURITY & INTEGRITY
            // ============================================
            entity.Property(e => e.Checksum)
                .HasMaxLength(64) // SHA256 hex string length
                .HasComment("SHA256 checksum for tamper detection");

            entity.Property(e => e.IsArchived)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Flag indicating if entry has been archived to cold storage");

            entity.Property(e => e.ArchivedAt)
                .HasComment("Archival date (when moved to cold storage)");

            // ============================================
            // INDEXES FOR PERFORMANCE
            // ============================================

            // Primary lookup indexes
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_AuditLogs_TenantId");

            entity.HasIndex(e => e.PerformedAt)
                .HasDatabaseName("IX_AuditLogs_PerformedAt");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_AuditLogs_UserId");

            entity.HasIndex(e => e.SessionId)
                .HasDatabaseName("IX_AuditLogs_SessionId");

            entity.HasIndex(e => e.CorrelationId)
                .HasDatabaseName("IX_AuditLogs_CorrelationId");

            // Entity tracking indexes
            entity.HasIndex(e => new { e.EntityType, e.EntityId })
                .HasDatabaseName("IX_AuditLogs_EntityType_EntityId");

            // Category and severity indexes
            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_AuditLogs_Category");

            entity.HasIndex(e => e.Severity)
                .HasDatabaseName("IX_AuditLogs_Severity");

            entity.HasIndex(e => e.ActionType)
                .HasDatabaseName("IX_AuditLogs_ActionType");

            // Composite indexes for common queries
            entity.HasIndex(e => new { e.TenantId, e.PerformedAt })
                .HasDatabaseName("IX_AuditLogs_TenantId_PerformedAt");

            entity.HasIndex(e => new { e.UserId, e.PerformedAt })
                .HasDatabaseName("IX_AuditLogs_UserId_PerformedAt");

            entity.HasIndex(e => new { e.Category, e.PerformedAt })
                .HasDatabaseName("IX_AuditLogs_Category_PerformedAt");

            entity.HasIndex(e => new { e.TenantId, e.Category, e.PerformedAt })
                .HasDatabaseName("IX_AuditLogs_TenantId_Category_PerformedAt");

            // Security monitoring indexes (partial index for failures)
            entity.HasIndex(e => new { e.Success, e.PerformedAt })
                .HasDatabaseName("IX_AuditLogs_Success_PerformedAt");

            // Archival index
            entity.HasIndex(e => new { e.IsArchived, e.PerformedAt })
                .HasDatabaseName("IX_AuditLogs_IsArchived_PerformedAt");

            // NOTE: Table partitioning by PerformedAt (monthly) is handled at the PostgreSQL level
            // via declarative partitioning. Migration SQL script will be created separately.
            // Partitioning improves query performance and enables efficient data retention.
        });

        // Configure IndustrySector entity
        modelBuilder.Entity<IndustrySector>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SectorCode).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ParentSectorId);

            entity.Property(e => e.SectorCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SectorName).IsRequired().HasMaxLength(300);
            entity.Property(e => e.SectorNameFrench).HasMaxLength(300);
            entity.Property(e => e.RemunerationOrderReference).HasMaxLength(200);

            entity.HasOne(e => e.ParentSector)
                  .WithMany(s => s.SubSectors)
                  .HasForeignKey(e => e.ParentSectorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure SectorComplianceRule entity
        modelBuilder.Entity<SectorComplianceRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SectorId);
            entity.HasIndex(e => e.RuleCategory);
            entity.HasIndex(e => new { e.EffectiveFrom, e.EffectiveTo });

            entity.Property(e => e.RuleCategory).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.RuleConfig).HasColumnType("jsonb");

            entity.HasOne(e => e.Sector)
                  .WithMany(s => s.ComplianceRules)
                  .HasForeignKey(e => e.SectorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Update Tenant entity - add Sector relationship
        modelBuilder.Entity<Tenant>()
            .HasOne(t => t.Sector)
            .WithMany()
            .HasForeignKey(t => t.SectorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure District entity
        modelBuilder.Entity<District>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DistrictCode).IsUnique();
            entity.HasIndex(e => e.Region);
            entity.HasIndex(e => e.DisplayOrder);

            entity.Property(e => e.DistrictCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.DistrictName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DistrictNameFrench).HasMaxLength(100);
            entity.Property(e => e.Region).IsRequired().HasMaxLength(50);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure Village entity
        modelBuilder.Entity<Village>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VillageCode).IsUnique();
            entity.HasIndex(e => e.DistrictId);
            entity.HasIndex(e => e.PostalCode);
            entity.HasIndex(e => e.DisplayOrder);

            entity.Property(e => e.VillageCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.VillageName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.VillageNameFrench).HasMaxLength(200);
            entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.LocalityType).HasMaxLength(50);

            entity.HasOne(e => e.District)
                  .WithMany(d => d.Villages)
                  .HasForeignKey(e => e.DistrictId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure PostalCode entity
        modelBuilder.Entity<PostalCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.VillageId);
            entity.HasIndex(e => e.DistrictId);
            entity.HasIndex(e => new { e.VillageName, e.DistrictName });

            entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
            entity.Property(e => e.VillageName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DistrictName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Region).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LocalityType).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Village)
                  .WithMany()
                  .HasForeignKey(e => e.VillageId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.District)
                  .WithMany()
                  .HasForeignKey(e => e.DistrictId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure RefreshToken entity (Production-Grade JWT Security)
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens", schema: "master");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.CreatedByIp)
                .IsRequired()
                .HasMaxLength(45); // IPv6 max length

            entity.Property(e => e.RevokedByIp)
                .HasMaxLength(45);

            entity.Property(e => e.ReplacedByToken)
                .HasMaxLength(500);

            entity.Property(e => e.ReasonRevoked)
                .HasMaxLength(200);

            // Indexes for performance
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.AdminUserId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.AdminUserId, e.ExpiresAt });

            // Relationship with AdminUser
            entity.HasOne(e => e.AdminUser)
                .WithMany()
                .HasForeignKey(e => e.AdminUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SecurityAlert entity (Fortune 500 Compliance - Real-Time Security Monitoring)
        modelBuilder.Entity<SecurityAlert>(entity =>
        {
            entity.ToTable("SecurityAlerts", schema: "master", tb =>
            {
                tb.HasComment("Production-grade security alert system for real-time threat detection. " +
                             "Supports SOX, GDPR, ISO 27001, PCI-DSS compliance. " +
                             "Integrates with Email, SMS, Slack, and SIEM systems.");
            });

            entity.HasKey(e => e.Id);

            // Alert Classification
            entity.Property(e => e.AlertType)
                .IsRequired()
                .HasComment("Type of security alert (enum stored as integer)");

            entity.Property(e => e.Severity)
                .IsRequired()
                .HasComment("Alert severity level (CRITICAL, EMERGENCY, HIGH, MEDIUM, LOW)");

            entity.Property(e => e.Category)
                .IsRequired()
                .HasComment("Alert category for classification");

            entity.Property(e => e.Status)
                .IsRequired()
                .HasComment("Alert status (NEW, ACKNOWLEDGED, IN_PROGRESS, RESOLVED, etc.)");

            // Alert Details
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500)
                .HasComment("Alert title/summary");

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(2000)
                .HasComment("Detailed alert description");

            entity.Property(e => e.RecommendedActions)
                .HasMaxLength(2000)
                .HasComment("Recommended actions to address the alert");

            entity.Property(e => e.RiskScore)
                .IsRequired()
                .HasComment("Risk score 0-100 calculated by anomaly detection");

            // Related Audit Log
            entity.Property(e => e.AuditLogId)
                .HasComment("Related audit log entry ID");

            entity.Property(e => e.AuditActionType)
                .HasComment("Related audit log action type");

            // WHO - User/Target Information
            entity.Property(e => e.TenantId)
                .HasComment("Tenant ID (null for platform-level alerts)");

            entity.Property(e => e.TenantName)
                .HasMaxLength(200)
                .HasComment("Tenant name for reporting");

            entity.Property(e => e.UserId)
                .HasComment("User ID who triggered the alert");

            entity.Property(e => e.UserEmail)
                .HasMaxLength(100)
                .HasComment("User email address");

            entity.Property(e => e.UserFullName)
                .HasMaxLength(200)
                .HasComment("User full name");

            entity.Property(e => e.UserRole)
                .HasMaxLength(50)
                .HasComment("User role at time of alert");

            // WHERE - Location Information
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasComment("IP address associated with alert");

            entity.Property(e => e.Geolocation)
                .HasMaxLength(500)
                .HasComment("Geolocation information");

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500)
                .HasComment("User agent string");

            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(500)
                .HasComment("Device information");

            // WHEN - Timestamp Information
            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasComment("When alert was created (UTC)");

            entity.Property(e => e.DetectedAt)
                .IsRequired()
                .HasComment("When alert was first detected (UTC)");

            entity.Property(e => e.AcknowledgedAt)
                .HasComment("When alert was acknowledged (UTC)");

            entity.Property(e => e.ResolvedAt)
                .HasComment("When alert was resolved (UTC)");

            // Workflow & Assignment
            entity.Property(e => e.AcknowledgedBy)
                .HasComment("User ID who acknowledged the alert");

            entity.Property(e => e.AcknowledgedByEmail)
                .HasMaxLength(100)
                .HasComment("Email of user who acknowledged");

            entity.Property(e => e.ResolvedBy)
                .HasComment("User ID who resolved the alert");

            entity.Property(e => e.ResolvedByEmail)
                .HasMaxLength(100)
                .HasComment("Email of user who resolved");

            entity.Property(e => e.ResolutionNotes)
                .HasMaxLength(2000)
                .HasComment("Resolution notes");

            entity.Property(e => e.AssignedTo)
                .HasComment("Assigned to user ID");

            entity.Property(e => e.AssignedToEmail)
                .HasMaxLength(100)
                .HasComment("Assigned to user email");

            // Notification Tracking
            entity.Property(e => e.EmailSent)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Whether email notification was sent");

            entity.Property(e => e.EmailSentAt)
                .HasComment("Email sent timestamp");

            entity.Property(e => e.EmailRecipients)
                .HasMaxLength(1000)
                .HasComment("Email recipients (comma-separated)");

            entity.Property(e => e.SmsSent)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Whether SMS notification was sent");

            entity.Property(e => e.SmsSentAt)
                .HasComment("SMS sent timestamp");

            entity.Property(e => e.SmsRecipients)
                .HasMaxLength(500)
                .HasComment("SMS recipients (comma-separated)");

            entity.Property(e => e.SlackSent)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Whether Slack notification was sent");

            entity.Property(e => e.SlackSentAt)
                .HasComment("Slack sent timestamp");

            entity.Property(e => e.SlackChannels)
                .HasMaxLength(500)
                .HasComment("Slack channels notified");

            entity.Property(e => e.SiemSent)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Whether SIEM notification was sent");

            entity.Property(e => e.SiemSentAt)
                .HasComment("SIEM sent timestamp");

            entity.Property(e => e.SiemSystem)
                .HasMaxLength(100)
                .HasComment("SIEM system name");

            // Anomaly Detection Metadata
            entity.Property(e => e.DetectionRule)
                .HasColumnType("jsonb")
                .HasComment("Detection rule that triggered alert (JSON)");

            entity.Property(e => e.BaselineMetrics)
                .HasColumnType("jsonb")
                .HasComment("Baseline metrics (JSON)");

            entity.Property(e => e.CurrentMetrics)
                .HasColumnType("jsonb")
                .HasComment("Current metrics that triggered alert (JSON)");

            entity.Property(e => e.DeviationPercentage)
                .HasPrecision(5, 2)
                .HasComment("Deviation percentage from baseline");

            // Context & Metadata
            entity.Property(e => e.CorrelationId)
                .HasMaxLength(100)
                .HasComment("Correlation ID for distributed tracing");

            entity.Property(e => e.AdditionalMetadata)
                .HasColumnType("jsonb")
                .HasComment("Additional metadata (JSON)");

            entity.Property(e => e.Tags)
                .HasMaxLength(500)
                .HasComment("Tags for categorization");

            // Compliance & Audit
            entity.Property(e => e.ComplianceFrameworks)
                .HasMaxLength(200)
                .HasComment("Related compliance frameworks");

            entity.Property(e => e.RequiresEscalation)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Whether alert requires escalation");

            entity.Property(e => e.EscalatedTo)
                .HasMaxLength(200)
                .HasComment("Escalated to (email or system)");

            entity.Property(e => e.EscalatedAt)
                .HasComment("Escalation timestamp");

            // Soft Delete
            entity.Property(e => e.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Soft delete flag");

            entity.Property(e => e.DeletedAt)
                .HasComment("When alert was soft-deleted");

            // Indexes for Performance
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_SecurityAlerts_TenantId");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_SecurityAlerts_CreatedAt");

            entity.HasIndex(e => e.AlertType)
                .HasDatabaseName("IX_SecurityAlerts_AlertType");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_SecurityAlerts_Status");

            entity.HasIndex(e => e.Severity)
                .HasDatabaseName("IX_SecurityAlerts_Severity");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_SecurityAlerts_UserId");

            entity.HasIndex(e => e.AuditLogId)
                .HasDatabaseName("IX_SecurityAlerts_AuditLogId");

            // Composite indexes for common queries
            entity.HasIndex(e => new { e.TenantId, e.CreatedAt })
                .HasDatabaseName("IX_SecurityAlerts_TenantId_CreatedAt");

            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("IX_SecurityAlerts_Status_CreatedAt");

            entity.HasIndex(e => new { e.Severity, e.Status, e.CreatedAt })
                .HasDatabaseName("IX_SecurityAlerts_Severity_Status_CreatedAt");

            // Soft delete filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure DetectedAnomaly entity (Fortune 500 Compliance - Anomaly Detection)
        modelBuilder.Entity<DetectedAnomaly>(entity =>
        {
            entity.ToTable("DetectedAnomalies", schema: "master");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.AnomalyType).IsRequired();
            entity.Property(e => e.RiskLevel).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.RiskScore).IsRequired();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Evidence).HasColumnType("jsonb");
            entity.Property(e => e.DetectionRule).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UserEmail).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.Location).HasMaxLength(500);

            // Indexes for performance
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.DetectedAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RiskLevel);
            entity.HasIndex(e => e.AnomalyType);
            entity.HasIndex(e => new { e.TenantId, e.DetectedAt });
            entity.HasIndex(e => new { e.Status, e.DetectedAt });
        });

        // Configure LegalHold entity (Fortune 500 Compliance - eDiscovery & Legal Hold)
        modelBuilder.Entity<LegalHold>(entity =>
        {
            entity.ToTable("LegalHolds", schema: "master");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.CaseNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.UserIds).HasColumnType("jsonb");
            entity.Property(e => e.EntityTypes).HasColumnType("jsonb");
            entity.Property(e => e.RequestedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LegalRepresentative).HasMaxLength(200);
            entity.Property(e => e.CourtOrder).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);

            // Indexes for performance
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CaseNumber).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
        });

        // ============================================
        // PRODUCTION-GRADE: Yearly Subscription Management
        // ============================================

        // Configure SubscriptionPayment entity (Fortune 500 - Manual Payment Tracking)
        modelBuilder.Entity<SubscriptionPayment>(entity =>
        {
            entity.ToTable("SubscriptionPayments", schema: "master", tb =>
            {
                tb.HasComment("Production-grade yearly subscription payment history. " +
                             "IMMUTABLE - payments are historical records. " +
                             "Manual payment processing by SuperAdmin with full audit trail. " +
                             "Indexed on TenantId, PaymentDate, Status for fast queries.");
            });

            entity.HasKey(e => e.Id);

            // Foreign key to Tenant
            entity.Property(e => e.TenantId)
                .IsRequired()
                .HasComment("Foreign key to Tenant");

            // Payment period tracking
            entity.Property(e => e.PeriodStartDate)
                .IsRequired()
                .HasComment("Subscription period start date");

            entity.Property(e => e.PeriodEndDate)
                .IsRequired()
                .HasComment("Subscription period end date (usually +365 days)");

            entity.Property(e => e.DueDate)
                .IsRequired()
                .HasComment("Payment due date for grace period calculations");

            // Financial fields (MUR - Mauritian Rupees)
            entity.Property(e => e.AmountMUR)
                .IsRequired()
                .HasPrecision(18, 2)
                .HasComment("Amount in Mauritian Rupees (MUR) - decimal(18,2)");

            // Payment status and tracking
            entity.Property(e => e.Status)
                .IsRequired()
                .HasComment("Payment status (Pending, Paid, Overdue, Failed, etc.)");

            entity.Property(e => e.PaidDate)
                .HasComment("Date when payment was marked as paid");

            entity.Property(e => e.ProcessedBy)
                .HasMaxLength(100)
                .HasComment("SuperAdmin who confirmed the payment (audit trail)");

            entity.Property(e => e.PaymentReference)
                .HasMaxLength(200)
                .HasComment("Invoice, receipt, or bank transaction reference");

            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(100)
                .HasComment("Payment method (Bank Transfer, Cash, Cheque, etc.)");

            entity.Property(e => e.Notes)
                .HasMaxLength(1000)
                .HasComment("Additional notes about the payment");

            // Employee tier at time of payment (audit trail)
            entity.Property(e => e.EmployeeTier)
                .IsRequired()
                .HasComment("Employee tier at time of payment (audit trail)");

            // Indexes for performance
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_SubscriptionPayments_TenantId");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_SubscriptionPayments_Status");

            entity.HasIndex(e => e.DueDate)
                .HasDatabaseName("IX_SubscriptionPayments_DueDate");

            entity.HasIndex(e => e.PaidDate)
                .HasDatabaseName("IX_SubscriptionPayments_PaidDate");

            entity.HasIndex(e => new { e.TenantId, e.PeriodStartDate })
                .HasDatabaseName("IX_SubscriptionPayments_TenantId_PeriodStartDate");

            entity.HasIndex(e => new { e.TenantId, e.Status })
                .HasDatabaseName("IX_SubscriptionPayments_TenantId_Status");

            // Relationship to Tenant
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.SubscriptionPayments)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent accidental deletion

            // Soft delete filter (inherited from BaseEntity)
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Configure SubscriptionNotificationLog entity (Fortune 500 - Email Deduplication)
        modelBuilder.Entity<SubscriptionNotificationLog>(entity =>
        {
            entity.ToTable("SubscriptionNotificationLogs", schema: "master", tb =>
            {
                tb.HasComment("Production-grade subscription notification audit log. " +
                             "Prevents duplicate email sends (Stripe, Chargebee pattern). " +
                             "IMMUTABLE - logs are historical records for compliance. " +
                             "Indexed on TenantId, NotificationType, SentDate for fast duplicate checks.");
            });

            entity.HasKey(e => e.Id);

            // Foreign keys
            entity.Property(e => e.TenantId)
                .IsRequired()
                .HasComment("Foreign key to Tenant");

            entity.Property(e => e.SubscriptionPaymentId)
                .HasComment("Related subscription payment ID (if applicable)");

            // Notification details
            entity.Property(e => e.NotificationType)
                .IsRequired()
                .HasComment("Type of notification sent (30d, 15d, 7d, expiry, etc.)");

            entity.Property(e => e.SentDate)
                .IsRequired()
                .HasComment("Date/time when notification was sent (UTC)");

            entity.Property(e => e.RecipientEmail)
                .IsRequired()
                .HasMaxLength(100)
                .HasComment("Recipient email address (audit trail)");

            entity.Property(e => e.EmailSubject)
                .IsRequired()
                .HasMaxLength(500)
                .HasComment("Email subject line");

            // Delivery tracking
            entity.Property(e => e.DeliverySuccess)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Whether email was delivered successfully");

            entity.Property(e => e.DeliveryError)
                .HasMaxLength(2000)
                .HasComment("Error message if delivery failed");

            // Snapshot of subscription state at notification time
            entity.Property(e => e.SubscriptionEndDateAtNotification)
                .IsRequired()
                .HasComment("Subscription end date at time of notification (audit trail)");

            entity.Property(e => e.DaysUntilExpiryAtNotification)
                .IsRequired()
                .HasComment("Days until expiry at time of notification (audit trail)");

            // Follow-up tracking
            entity.Property(e => e.RequiresFollowUp)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Does this notification require follow-up action?");

            entity.Property(e => e.FollowUpCompletedDate)
                .HasComment("When follow-up action was completed");

            entity.Property(e => e.FollowUpNotes)
                .HasMaxLength(2000)
                .HasComment("Notes about follow-up action taken");

            // Indexes for performance and duplicate prevention
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_SubscriptionNotificationLogs_TenantId");

            entity.HasIndex(e => e.NotificationType)
                .HasDatabaseName("IX_SubscriptionNotificationLogs_NotificationType");

            entity.HasIndex(e => e.SentDate)
                .HasDatabaseName("IX_SubscriptionNotificationLogs_SentDate");

            entity.HasIndex(e => e.SubscriptionPaymentId)
                .HasDatabaseName("IX_SubscriptionNotificationLogs_SubscriptionPaymentId");

            // Composite index for duplicate checking
            entity.HasIndex(e => new { e.TenantId, e.NotificationType, e.SentDate })
                .HasDatabaseName("IX_SubscriptionNotificationLogs_TenantId_Type_SentDate");

            entity.HasIndex(e => new { e.TenantId, e.SentDate })
                .HasDatabaseName("IX_SubscriptionNotificationLogs_TenantId_SentDate");

            // Index for follow-up tracking
            entity.HasIndex(e => new { e.RequiresFollowUp, e.FollowUpCompletedDate })
                .HasDatabaseName("IX_SubscriptionNotificationLogs_RequiresFollowUp");

            // Relationships
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.SubscriptionNotificationLogs)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent accidental deletion

            entity.HasOne(e => e.SubscriptionPayment)
                .WithMany()
                .HasForeignKey(e => e.SubscriptionPaymentId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent accidental deletion

            // Soft delete filter (inherited from BaseEntity)
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Update Tenant entity configuration - add subscription field configurations
        modelBuilder.Entity<Tenant>(entity =>
        {
            // Yearly pricing in MUR
            entity.Property(e => e.YearlyPriceMUR)
                .HasPrecision(18, 2)
                .HasComment("Yearly subscription price in Mauritian Rupees (MUR)");

            // Notification tracking
            entity.Property(e => e.LastNotificationSent)
                .HasComment("Last subscription notification sent (timestamp)");

            entity.Property(e => e.LastNotificationType)
                .HasComment("Type of last notification sent (prevents duplicates)");

            entity.Property(e => e.GracePeriodStartDate)
                .HasComment("Grace period start date (when subscription expired)");

            // Index for subscription expiry monitoring
            entity.HasIndex(e => e.SubscriptionEndDate)
                .HasDatabaseName("IX_Tenants_SubscriptionEndDate");

            entity.HasIndex(e => new { e.Status, e.SubscriptionEndDate })
                .HasDatabaseName("IX_Tenants_Status_SubscriptionEndDate");
        });

        // ============================================
        // FORTUNE 500: Feature Flags Configuration
        // ============================================
        modelBuilder.Entity<FeatureFlag>(entity =>
        {
            entity.ToTable("FeatureFlags", schema: "master", tb =>
            {
                tb.HasComment("Fortune 500 feature flag system for per-tenant control. " +
                             "Enables canary deployment, gradual rollout, emergency rollback, A/B testing. " +
                             "NULL TenantId = global default, NON-NULL = tenant override.");
            });

            entity.HasKey(e => e.Id);

            // Foreign key to Tenant (nullable for global defaults)
            entity.Property(e => e.TenantId)
                .HasComment("Tenant ID (NULL = global default, NON-NULL = tenant override)");

            // Module identification
            entity.Property(e => e.Module)
                .IsRequired()
                .HasMaxLength(100)
                .HasComment("Module name (auth, dashboard, employees, payroll, etc.)");

            // Feature state
            entity.Property(e => e.IsEnabled)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Whether feature is enabled (default FALSE for safety)");

            // Rollout control
            entity.Property(e => e.RolloutPercentage)
                .IsRequired()
                .HasDefaultValue(0)
                .HasComment("Rollout percentage 0-100 (0=disabled, 100=fully enabled)");

            // Documentation
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasComment("Feature description for documentation");

            entity.Property(e => e.Tags)
                .HasMaxLength(200)
                .HasComment("Tags for categorization (comma-separated)");

            entity.Property(e => e.MinimumTier)
                .HasMaxLength(50)
                .HasComment("Minimum tier required (NULL = all tiers)");

            // Emergency rollback
            entity.Property(e => e.IsEmergencyDisabled)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Emergency rollback flag for quick disable");

            entity.Property(e => e.EmergencyDisabledReason)
                .HasMaxLength(1000)
                .HasComment("Reason for emergency rollback (audit trail)");

            entity.Property(e => e.EmergencyDisabledAt)
                .HasComment("Emergency rollback timestamp");

            entity.Property(e => e.EmergencyDisabledBy)
                .HasMaxLength(100)
                .HasComment("SuperAdmin who triggered emergency rollback");

            // Indexes for performance
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_FeatureFlags_TenantId");

            entity.HasIndex(e => e.Module)
                .HasDatabaseName("IX_FeatureFlags_Module");

            entity.HasIndex(e => new { e.TenantId, e.Module })
                .HasDatabaseName("IX_FeatureFlags_TenantId_Module")
                .IsUnique();

            entity.HasIndex(e => e.IsEnabled)
                .HasDatabaseName("IX_FeatureFlags_IsEnabled");

            entity.HasIndex(e => e.IsEmergencyDisabled)
                .HasDatabaseName("IX_FeatureFlags_IsEmergencyDisabled");

            // Relationship to Tenant
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Soft delete filter
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // ============================================
        // FORTUNE 500: Activation Resend Logs
        // ============================================
        modelBuilder.Entity<ActivationResendLog>(entity =>
        {
            entity.ToTable("ActivationResendLogs", schema: "master", tb =>
            {
                tb.HasComment("Fortune 500 activation resend audit log for multi-tenant SaaS. " +
                             "Enables rate limiting (max 3 per hour), security monitoring, and GDPR compliance. " +
                             "IMMUTABLE logs with cascade delete on tenant deletion.");
            });

            entity.HasKey(e => e.Id);

            // Foreign key to Tenant (required)
            entity.Property(e => e.TenantId)
                .IsRequired()
                .HasComment("Tenant ID (enables per-tenant rate limiting)");

            // Timestamp tracking
            entity.Property(e => e.RequestedAt)
                .IsRequired()
                .HasComment("When resend was requested (UTC) - used for sliding window rate limits");

            // Security tracking
            entity.Property(e => e.RequestedFromIp)
                .HasMaxLength(45) // IPv6 max length
                .HasComment("IP address of requester (IPv4 or IPv6) for security monitoring");

            entity.Property(e => e.RequestedByEmail)
                .HasMaxLength(255)
                .HasComment("Email address used in request (must match tenant email)");

            // Token tracking (partial - first 8 chars only)
            entity.Property(e => e.TokenGenerated)
                .HasMaxLength(32)
                .HasComment("New token generated (truncated for security - never full token!)");

            entity.Property(e => e.TokenExpiry)
                .IsRequired()
                .HasComment("Token expiration timestamp (UTC) - typically 24 hours from RequestedAt");

            // Success/failure tracking
            entity.Property(e => e.Success)
                .IsRequired()
                .HasDefaultValue(true)
                .HasComment("Was resend successful? (false if rate limited or email send failed)");

            entity.Property(e => e.FailureReason)
                .HasMaxLength(2000)
                .HasComment("Failure reason if Success=false (rate limit, email error, validation failure, etc.)");

            // Device and location tracking
            entity.Property(e => e.UserAgent)
                .HasMaxLength(500)
                .HasComment("User agent string for fraud detection");

            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(200)
                .HasComment("Parsed device info (Mobile/Desktop, browser, OS)");

            entity.Property(e => e.Geolocation)
                .HasMaxLength(500)
                .HasComment("City, country for fraud detection");

            // Email delivery tracking
            entity.Property(e => e.EmailDelivered)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Was activation email delivered successfully?");

            entity.Property(e => e.EmailSendError)
                .HasMaxLength(2000)
                .HasComment("SMTP error or bounce reason if delivery failed");

            // Rate limiting tracking
            entity.Property(e => e.ResendCountLastHour)
                .IsRequired()
                .HasDefaultValue(0)
                .HasComment("Number of resend attempts in last hour (real-time rate limit tracking)");

            entity.Property(e => e.WasRateLimited)
                .IsRequired()
                .HasDefaultValue(false)
                .HasComment("Was this request blocked by rate limiting?");

            // Indexes for performance
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_ActivationResendLogs_TenantId");

            entity.HasIndex(e => e.RequestedAt)
                .HasDatabaseName("IX_ActivationResendLogs_RequestedAt");

            entity.HasIndex(e => e.RequestedFromIp)
                .HasDatabaseName("IX_ActivationResendLogs_RequestedFromIp");

            // Composite index for rate limit queries (sliding window)
            entity.HasIndex(e => new { e.TenantId, e.RequestedAt })
                .HasDatabaseName("IX_ActivationResendLogs_TenantId_RequestedAt");

            // Index for IP-based rate limiting
            entity.HasIndex(e => new { e.RequestedFromIp, e.RequestedAt })
                .HasDatabaseName("IX_ActivationResendLogs_IP_RequestedAt");

            // Index for success/failure analytics
            entity.HasIndex(e => e.Success)
                .HasDatabaseName("IX_ActivationResendLogs_Success");

            // Relationship to Tenant (CASCADE delete for GDPR compliance)
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade); // Delete logs when tenant is deleted

            // Soft delete filter (inherited from BaseEntity)
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }
}
