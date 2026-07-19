using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using WorkshopAdmin.IntegrationTests.Postgres;
using WorkshopAdmin.SharedKernel.Database;
using WorkshopAdmin.SharedKernel.Events;
using Xunit;

namespace WorkshopAdmin.IntegrationTests.Events;

/// <summary>
/// Proves backend plan §8.3: event handlers run inside the publisher's transaction —
/// rolling the session back undoes the handler's writes too (outbox precondition).
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class DomainEventTransactionTests(PostgresFixture fixture)
{
    private sealed record SupplierRequested(Guid TenantId, string Name) : IDomainEvent;

    private sealed class InsertSupplierHandler(IDbSession session) : IDomainEventHandler<SupplierRequested>
    {
        public async Task HandleAsync(SupplierRequested domainEvent, CancellationToken cancellationToken)
        {
            NpgsqlConnection connection = await session.GetOpenConnectionAsync(cancellationToken);
            await using NpgsqlCommand command = new(
                "INSERT INTO workshop.suppliers (tenant_id, name) VALUES ($1, $2)",
                connection, session.Transaction);
            command.Parameters.AddWithValue(domainEvent.TenantId);
            command.Parameters.AddWithValue(domainEvent.Name);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task HandlerWrites_ShareThePublisherTransaction_AndRollBackWithIt()
    {
        Guid tenantId = await TestSeed.CreateTenantAsync(fixture);
        string name = $"event-{Guid.NewGuid():N}";

        ServiceCollection services = new();
        services.AddScoped<IDbSession>(_ => new DbSession(
            Options.Create(fixture.AppDatabaseOptions), new TestCurrentUser(tenantId)));
        services.AddScoped<IDomainEventHandler<SupplierRequested>, InsertSupplierHandler>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        await using ServiceProvider provider = services.BuildServiceProvider();

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            IDomainEventDispatcher dispatcher =
                scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
            await dispatcher.DispatchAsync(new SupplierRequested(tenantId, name));

            // Handler's write is visible inside the shared transaction…
            IDbSession session = scope.ServiceProvider.GetRequiredService<IDbSession>();
            Assert.Equal(1L, await CountAsync(session, name));

            await session.RollbackAsync();
        }

        // …and gone after rollback.
        await using DbSession verify = new(
            Options.Create(fixture.AppDatabaseOptions), new TestCurrentUser(tenantId));
        Assert.Equal(0L, await CountAsync(verify, name));
    }

    private static async Task<long> CountAsync(IDbSession session, string name)
    {
        NpgsqlConnection connection = await session.GetOpenConnectionAsync();
        await using NpgsqlCommand command = new(
            "SELECT COUNT(*) FROM workshop.suppliers WHERE name = $1", connection, session.Transaction);
        command.Parameters.AddWithValue(name);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
