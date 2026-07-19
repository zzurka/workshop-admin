using Microsoft.Extensions.Options;
using Npgsql;
using WorkshopAdmin.SharedKernel.Auth;

namespace WorkshopAdmin.SharedKernel.Database;

public sealed class DbSession(IOptions<DatabaseOptions> options, ICurrentUser currentUser) : IDbSession
{
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;
    private bool _completed;

    public NpgsqlTransaction? Transaction => _transaction;

    public bool IsActive => _transaction is not null;

    public async Task<NpgsqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is not null && _transaction is not null)
        {
            return _connection;
        }

        if (_completed)
        {
            throw new InvalidOperationException("The DB session has already been committed or rolled back.");
        }

        NpgsqlConnection connection = new(options.Value.BuildConnectionString());
        await connection.OpenAsync(cancellationToken);
        try
        {
            NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
            await ApplyTenantContextAsync(connection, transaction, cancellationToken);
            _connection = connection;
            _transaction = transaction;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }

        return _connection;
    }

    public NpgsqlConnection GetOpenConnection() =>
        GetOpenConnectionAsync().GetAwaiter().GetResult();

    private async Task ApplyTenantContextAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        // set_config(..., is_local => true) scopes the setting to this transaction (SET LOCAL).
        // Values travel as parameters — never concatenated into the SQL text.
        if (currentUser.IsPlatformAdmin)
        {
            await using NpgsqlCommand command = new(
                "SELECT set_config('app.is_platform_admin', 'true', true)", connection, transaction);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        else if (currentUser.TenantId is Guid tenantId)
        {
            await using NpgsqlCommand command = new(
                "SELECT set_config('app.current_tenant_id', $1, true)", connection, transaction);
            command.Parameters.AddWithValue(tenantId.ToString());
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // No tenant claim and not platform admin (pre-login auth flows): no context is set.
        // RLS policies are fail-closed, so tenant-scoped tables simply return no rows.
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.CommitAsync(cancellationToken);
        await CleanUpAsync();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        await CleanUpAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            // Not committed → the transaction rolls back on dispose.
            await _transaction.DisposeAsync();
        }

        await CleanUpAsync();
    }

    private async ValueTask CleanUpAsync()
    {
        _transaction = null;
        _completed = true;
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
