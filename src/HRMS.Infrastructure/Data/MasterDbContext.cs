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

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.PerformedAt);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PerformedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
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
