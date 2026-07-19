using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.SharedKernel.Database;
using WorkshopAdmin.SharedKernel.Persistence;

namespace WorkshopAdmin.IntegrationTests.Ef;

// Hand-written EF models over real tables, standing in for module DbContexts until F2.
// They double as the first subjects of the EF drift test.

public sealed class CodebookCurrency
{
    public short Id { get; set; }
    public string Code { get; set; } = "";
    public Dictionary<string, string> Label { get; set; } = [];
    public short SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class TestCodebookDbContext(DbContextOptions<TestCodebookDbContext> options, IDbSession session)
    : ModuleDbContext(options, session)
{
    public DbSet<CodebookCurrency> Currencies => Set<CodebookCurrency>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CodebookCurrency>(entity =>
        {
            entity.ToTable("currencies", "codebook");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Label).HasJsonbLabel();
        });
    }
}

public sealed class TestSupplier : AuditableEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class TestSupplierDbContext(DbContextOptions<TestSupplierDbContext> options, IDbSession session)
    : ModuleDbContext(options, session)
{
    public DbSet<TestSupplier> Suppliers => Set<TestSupplier>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestSupplier>(entity =>
        {
            entity.ToTable("suppliers", "workshop");
            entity.HasKey(s => s.Id);
        });
    }
}
