using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.SharedKernel.Database;
using WorkshopAdmin.SharedKernel.Persistence;

namespace WorkshopAdmin.Modules.Tenants.Persistence;

/// <summary>Maps the <c>tenant</c> schema only. Codebook references (currency, billing
/// period) stay plain FK ids — validated through ICodebookLookup, never joined here.</summary>
internal sealed class TenantsDbContext(DbContextOptions<TenantsDbContext> options, IDbSession session)
    : ModuleDbContext(options, session)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants", "tenant");
            entity.HasKey(t => t.Id);
            entity.HasOne(t => t.SubscriptionPlan).WithMany().HasForeignKey(t => t.SubscriptionPlanId);
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("subscription_plans", "tenant");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Label).HasJsonbLabel();
            entity.Property(p => p.Description!).HasJsonbLabel();
            entity.Property(p => p.Features).HasColumnType("jsonb");
        });

        modelBuilder.Entity<TenantSubscription>(entity =>
        {
            entity.ToTable("tenant_subscriptions", "tenant");
            entity.HasKey(s => s.Id);
            entity.HasOne(s => s.Tenant).WithMany().HasForeignKey(s => s.TenantId);
            entity.HasOne(s => s.SubscriptionPlan).WithMany().HasForeignKey(s => s.SubscriptionPlanId);
        });
    }
}
