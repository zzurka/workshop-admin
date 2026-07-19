using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using WorkshopAdmin.IntegrationTests.Postgres;
using WorkshopAdmin.Modules.Auth;
using WorkshopAdmin.Modules.Codebook;
using WorkshopAdmin.Modules.Customers;
using WorkshopAdmin.Modules.Hr;
using WorkshopAdmin.Modules.Notifications;
using WorkshopAdmin.Modules.Tenants;
using WorkshopAdmin.Modules.Warehouse;
using WorkshopAdmin.Modules.Workshop;
using WorkshopAdmin.SharedKernel.Database;
using WorkshopAdmin.SharedKernel.Persistence;
using Xunit;

namespace WorkshopAdmin.IntegrationTests.Ef;

/// <summary>
/// Drift guard for the hand-maintained EF model (DB-first, no EF migrations): runs a
/// trivial query against every DbSet of every ModuleDbContext on the freshly migrated
/// database. A renamed/dropped column or table fails here immediately. Discovers
/// contexts by reflection over all module assemblies, so F2+ contexts are picked up
/// with no changes to this test.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class EfDriftTests(PostgresFixture fixture)
{
    private static readonly MethodInfo QueryOneMethod =
        typeof(EfDriftTests).GetMethod(nameof(QueryOneAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static TheoryData<Type> ModuleDbContextTypes()
    {
        Assembly[] assemblies =
        [
            typeof(TenantsModule).Assembly,
            typeof(AuthModule).Assembly,
            typeof(CodebookModule).Assembly,
            typeof(CustomersModule).Assembly,
            typeof(HrModule).Assembly,
            typeof(WorkshopModule).Assembly,
            typeof(WarehouseModule).Assembly,
            typeof(NotificationsModule).Assembly,
            typeof(EfDriftTests).Assembly
        ];

        TheoryData<Type> data = [];
        foreach (Type type in assemblies
                     .SelectMany(a => a.GetTypes())
                     .Where(t => t.IsSubclassOf(typeof(ModuleDbContext)) && !t.IsAbstract))
        {
            data.Add(type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ModuleDbContextTypes))]
    public async Task EveryDbSet_MatchesTheMigratedSchema(Type contextType)
    {
        // Platform-admin context: RLS lets the queries through everywhere; row contents
        // are irrelevant — a single Take(1) round-trip validates table and column mapping.
        await using DbSession session = new(
            Options.Create(fixture.AppDatabaseOptions), new TestCurrentUser(isPlatformAdmin: true));

        DbContextOptionsBuilder builder = (DbContextOptionsBuilder)Activator.CreateInstance(
            typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType))!;
        builder.UseNpgsql(session.GetOpenConnection()).UseSnakeCaseNamingConvention();

        await using ModuleDbContext context =
            (ModuleDbContext)Activator.CreateInstance(contextType, builder.Options, session)!;

        foreach (IEntityType entityType in context.Model.GetEntityTypes().Where(t => !t.IsOwned()))
        {
            await (Task)QueryOneMethod.MakeGenericMethod(entityType.ClrType).Invoke(null, [context])!;
        }
    }

    private static async Task QueryOneAsync<TEntity>(DbContext context) where TEntity : class =>
        await context.Set<TEntity>().IgnoreQueryFilters().Take(1).ToListAsync();
}
