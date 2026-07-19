using Microsoft.Extensions.Options;
using Npgsql;
using WorkshopAdmin.IntegrationTests.Postgres;
using WorkshopAdmin.SharedKernel.Database;
using Xunit;

namespace WorkshopAdmin.IntegrationTests.Rls;

/// <summary>
/// Proves the IDbSession + RLS contract on the real migrated schema (backend plan §6):
/// no context → no rows (fail-closed), tenant context → only that tenant's rows,
/// platform admin → everything, cross-tenant writes rejected, dispose without commit → rollback.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class RlsIsolationTests(PostgresFixture fixture) : IAsyncLifetime
{
    private Guid _tenantA;
    private Guid _tenantB;

    public async Task InitializeAsync()
    {
        (_tenantA, _tenantB) = await SeedTwoTenantsWithSuppliersAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private DbSession CreateSession(Guid? tenantId = null, bool isPlatformAdmin = false) =>
        new(Options.Create(fixture.AppDatabaseOptions), new TestCurrentUser(tenantId, isPlatformAdmin));

    [Fact]
    public async Task WithoutTenantContext_SeesNoRows()
    {
        await using DbSession session = CreateSession();
        NpgsqlConnection connection = await session.GetOpenConnectionAsync();

        long count = await CountSuppliersAsync(connection, session);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task WithTenantContext_SeesOnlyOwnRows()
    {
        await using DbSession session = CreateSession(_tenantA);
        NpgsqlConnection connection = await session.GetOpenConnectionAsync();

        await using NpgsqlCommand command = new(
            "SELECT DISTINCT tenant_id FROM workshop.suppliers", connection, session.Transaction);
        List<Guid> tenantIds = [];
        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tenantIds.Add(reader.GetGuid(0));
            }
        }

        Guid tenantId = Assert.Single(tenantIds);
        Assert.Equal(_tenantA, tenantId);
    }

    [Fact]
    public async Task PlatformAdmin_SeesAllTenants()
    {
        await using DbSession session = CreateSession(isPlatformAdmin: true);
        NpgsqlConnection connection = await session.GetOpenConnectionAsync();

        await using NpgsqlCommand command = new(
            "SELECT COUNT(*) FROM workshop.suppliers WHERE tenant_id = ANY($1)",
            connection, session.Transaction);
        command.Parameters.AddWithValue(new[] { _tenantA, _tenantB });
        long count = (long)(await command.ExecuteScalarAsync())!;

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CrossTenantWrite_IsRejected()
    {
        await using DbSession session = CreateSession(_tenantA);
        NpgsqlConnection connection = await session.GetOpenConnectionAsync();

        await using NpgsqlCommand command = new(
            "INSERT INTO workshop.suppliers (tenant_id, name) VALUES ($1, $2)",
            connection, session.Transaction);
        command.Parameters.AddWithValue(_tenantB);
        command.Parameters.AddWithValue($"smuggled-{Guid.NewGuid():N}");

        PostgresException ex = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, ex.SqlState);
    }

    [Fact]
    public async Task DisposeWithoutCommit_RollsBack()
    {
        string name = $"uncommitted-{Guid.NewGuid():N}";

        await using (DbSession session = CreateSession(_tenantA))
        {
            NpgsqlConnection connection = await session.GetOpenConnectionAsync();
            await using NpgsqlCommand insert = new(
                "INSERT INTO workshop.suppliers (tenant_id, name) VALUES ($1, $2)",
                connection, session.Transaction);
            insert.Parameters.AddWithValue(_tenantA);
            insert.Parameters.AddWithValue(name);
            await insert.ExecuteNonQueryAsync();
            // No CommitAsync — disposing must roll the insert back.
        }

        await using DbSession verify = CreateSession(_tenantA);
        NpgsqlConnection verifyConnection = await verify.GetOpenConnectionAsync();
        await using NpgsqlCommand count = new(
            "SELECT COUNT(*) FROM workshop.suppliers WHERE name = $1",
            verifyConnection, verify.Transaction);
        count.Parameters.AddWithValue(name);

        Assert.Equal(0L, await count.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Commit_PersistsWithinTenant()
    {
        string name = $"committed-{Guid.NewGuid():N}";

        await using (DbSession session = CreateSession(_tenantA))
        {
            NpgsqlConnection connection = await session.GetOpenConnectionAsync();
            await using NpgsqlCommand insert = new(
                "INSERT INTO workshop.suppliers (tenant_id, name) VALUES ($1, $2)",
                connection, session.Transaction);
            insert.Parameters.AddWithValue(_tenantA);
            insert.Parameters.AddWithValue(name);
            await insert.ExecuteNonQueryAsync();
            await session.CommitAsync();
        }

        await using DbSession verify = CreateSession(_tenantA);
        NpgsqlConnection verifyConnection = await verify.GetOpenConnectionAsync();
        await using NpgsqlCommand count = new(
            "SELECT COUNT(*) FROM workshop.suppliers WHERE name = $1",
            verifyConnection, verify.Transaction);
        count.Parameters.AddWithValue(name);

        Assert.Equal(1L, await count.ExecuteScalarAsync());
    }

    private async Task<long> CountSuppliersAsync(NpgsqlConnection connection, DbSession session)
    {
        await using NpgsqlCommand command = new(
            "SELECT COUNT(*) FROM workshop.suppliers", connection, session.Transaction);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>Seeds two tenants with one supplier each, as the RLS-bypassing admin user.</summary>
    private async Task<(Guid TenantA, Guid TenantB)> SeedTwoTenantsWithSuppliersAsync()
    {
        await using NpgsqlConnection admin = new(fixture.AdminConnectionString);
        await admin.OpenAsync();

        Guid[] tenants = new Guid[2];
        for (int i = 0; i < 2; i++)
        {
            await using NpgsqlCommand createTenant = new(
                """
                INSERT INTO tenant.tenants (name, slug, subscription_plan_id, default_currency_id)
                VALUES ($1, $2,
                        (SELECT id FROM tenant.subscription_plans ORDER BY sort_order LIMIT 1),
                        (SELECT id FROM codebook.currencies ORDER BY id LIMIT 1))
                RETURNING id
                """, admin);
            string slug = $"rls-test-{Guid.NewGuid():N}";
            createTenant.Parameters.AddWithValue($"RLS Test {slug}");
            createTenant.Parameters.AddWithValue(slug);
            tenants[i] = (Guid)(await createTenant.ExecuteScalarAsync())!;

            await using NpgsqlCommand createSupplier = new(
                "INSERT INTO workshop.suppliers (tenant_id, name) VALUES ($1, $2)", admin);
            createSupplier.Parameters.AddWithValue(tenants[i]);
            createSupplier.Parameters.AddWithValue($"Supplier {slug}");
            await createSupplier.ExecuteNonQueryAsync();
        }

        return (tenants[0], tenants[1]);
    }
}
