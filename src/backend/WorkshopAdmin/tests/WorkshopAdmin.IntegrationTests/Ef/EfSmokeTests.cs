using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorkshopAdmin.IntegrationTests.Postgres;
using WorkshopAdmin.SharedKernel.Database;
using WorkshopAdmin.SharedKernel.Persistence;
using Xunit;

namespace WorkshopAdmin.IntegrationTests.Ef;

/// <summary>
/// Proves the SharedKernel EF base on the real schema: snake_case mapping, JSONB labels,
/// DB-generated defaults (uuidv7/NOW), audit interceptor and the soft-delete filter —
/// all through the shared IDbSession connection and transaction.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class EfSmokeTests(PostgresFixture fixture)
{
    private DbSession CreateSession(Guid? tenantId = null, Guid? userId = null) =>
        new(Options.Create(fixture.AppDatabaseOptions), new TestCurrentUser(tenantId, userId: userId));

    private static TContext CreateContext<TContext>(DbSession session, TestCurrentUser? currentUser = null)
        where TContext : ModuleDbContext
    {
        DbContextOptionsBuilder<TContext> builder = new();
        builder
            .UseNpgsql(session.GetOpenConnection())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new AuditSaveChangesInterceptor(currentUser ?? new TestCurrentUser()));
        return (TContext)Activator.CreateInstance(typeof(TContext), builder.Options, session)!;
    }

    [Fact]
    public async Task Currencies_SnakeCaseAndJsonbLabels_Map()
    {
        // codebook is shared-read and outside RLS — no tenant context needed.
        await using DbSession session = CreateSession();
        await using TestCodebookDbContext context = CreateContext<TestCodebookDbContext>(session);

        List<CodebookCurrency> currencies = await context.Currencies.ToListAsync();

        Assert.NotEmpty(currencies);
        Assert.All(currencies, c => Assert.False(string.IsNullOrEmpty(c.Code)));
        Assert.Contains(currencies, c => c.Label.ContainsKey("en"));
    }

    [Fact]
    public async Task Insert_DbGeneratesIdAndCreatedAt()
    {
        Guid tenantId = await TestSeed.CreateTenantAsync(fixture);

        await using DbSession session = CreateSession(tenantId);
        await using TestSupplierDbContext context = CreateContext<TestSupplierDbContext>(session);

        TestSupplier supplier = new() { TenantId = tenantId, Name = $"EF {Guid.NewGuid():N}" };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, supplier.Id);
        Assert.NotEqual(default, supplier.CreatedAt);
        Assert.Null(supplier.UpdatedAt);
    }

    [Fact]
    public async Task Update_AuditInterceptor_SetsUpdatedAt()
    {
        Guid tenantId = await TestSeed.CreateTenantAsync(fixture);

        await using DbSession session = CreateSession(tenantId);
        await using TestSupplierDbContext context = CreateContext<TestSupplierDbContext>(session);

        TestSupplier supplier = new() { TenantId = tenantId, Name = $"EF {Guid.NewGuid():N}" };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        supplier.Name += " (renamed)";
        await context.SaveChangesAsync();

        Assert.NotNull(supplier.UpdatedAt);
    }

    [Fact]
    public async Task SoftDelete_GlobalQueryFilter_HidesRow()
    {
        Guid tenantId = await TestSeed.CreateTenantAsync(fixture);

        await using DbSession session = CreateSession(tenantId);
        await using TestSupplierDbContext context = CreateContext<TestSupplierDbContext>(session);

        TestSupplier supplier = new() { TenantId = tenantId, Name = $"EF {Guid.NewGuid():N}" };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        supplier.IsDeleted = true;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Null(await context.Suppliers.SingleOrDefaultAsync(s => s.Id == supplier.Id));
        Assert.NotNull(await context.Suppliers.IgnoreQueryFilters()
            .SingleOrDefaultAsync(s => s.Id == supplier.Id));
    }

    [Fact]
    public async Task EfAndRawSql_ShareTransactionAndRlsContext()
    {
        Guid tenantId = await TestSeed.CreateTenantAsync(fixture);

        await using DbSession session = CreateSession(tenantId);
        await using TestSupplierDbContext context = CreateContext<TestSupplierDbContext>(session);

        // Uncommitted EF insert must be visible to raw SQL on the same session.
        TestSupplier supplier = new() { TenantId = tenantId, Name = $"EF {Guid.NewGuid():N}" };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        await using Npgsql.NpgsqlCommand command = new(
            "SELECT COUNT(*) FROM workshop.suppliers WHERE id = $1",
            await session.GetOpenConnectionAsync(), session.Transaction);
        command.Parameters.AddWithValue(supplier.Id);

        Assert.Equal(1L, await command.ExecuteScalarAsync());
    }
}
