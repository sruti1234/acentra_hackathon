using Microsoft.EntityFrameworkCore;
using MultiTenantInventory.Api.Interfaces;
using MultiTenantInventory.Api.Models;

namespace MultiTenantInventory.Api.Data;

public class ApplicationDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    // ITenantContext is Scoped, so each request's DbContext gets that request's tenant.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserTenant> UserTenants => Set<UserTenant>();
    public DbSet<Product> Products => Set<Product>();

    // Used by the global query filter. EF Core reads this property from the
    // current DbContext instance every time a query runs.
    public int? CurrentTenantId => _tenantContext.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Subdomain).IsRequired().HasMaxLength(63);
            entity.HasIndex(t => t.Subdomain).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<UserTenant>(entity =>
        {
            entity.HasKey(ut => new { ut.UserId, ut.TenantId });

            entity.HasOne(ut => ut.User)
                  .WithMany(u => u.UserTenants)
                  .HasForeignKey(ut => ut.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ut => ut.Tenant)
                  .WithMany(t => t.UserTenants)
                  .HasForeignKey(ut => ut.TenantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.ImageKey).HasMaxLength(1024);

            entity.HasOne(p => p.Tenant)
                  .WithMany(t => t.Products)
                  .HasForeignKey(p => p.TenantId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);

            // ===== TENANT ISOLATION =====
            // Every query on Products automatically gets
            //   WHERE "TenantId" = <current request's tenant>
            // If no tenant is set, no products are returned at all (safe default).
            entity.HasQueryFilter(p => CurrentTenantId != null && p.TenantId == CurrentTenantId);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Second line of defence for writes: inside a tenant request, a product can
    // only be created/changed for the current tenant, and TenantId can never change.
    private void ApplyTenantRules()
    {
        foreach (var entry in ChangeTracker.Entries<Product>())
        {
            if (entry.State == EntityState.Modified)
            {
                if (entry.Property(p => p.TenantId).IsModified)
                {
                    throw new InvalidOperationException("A product cannot be moved to another tenant.");
                }

                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }

            if (CurrentTenantId.HasValue
                && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                && entry.Entity.TenantId != CurrentTenantId.Value)
            {
                throw new InvalidOperationException("Cross-tenant write blocked.");
            }
        }
    }
}
