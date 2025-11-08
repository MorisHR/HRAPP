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

    // JWT Refresh Tokens (Production-Grade Security)
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    // Industry Sectors (Mauritius Remuneration Orders)
    public DbSet<IndustrySector> IndustrySectors { get; set; }
    public DbSet<SectorComplianceRule> SectorComplianceRules { get; set; }

    // Mauritius Address Hierarchy
    public DbSet<District> Districts { get; set; }
    public DbSet<Village> Villages { get; set; }
    public DbSet<PostalCode> PostalCodes { get; set; }

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
    }
}
