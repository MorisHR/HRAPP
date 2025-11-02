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

    // Industry Sectors (Mauritius Remuneration Orders)
    public DbSet<IndustrySector> IndustrySectors { get; set; }
    public DbSet<SectorComplianceRule> SectorComplianceRules { get; set; }

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
    }
}
