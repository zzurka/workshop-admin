using Npgsql;

namespace WorkshopAdmin.SharedKernel.Database;

/// <summary>
/// The single place where a DB connection, transaction and tenant (RLS) context are
/// established for a request. Request-scoped and lazy: nothing is opened until the
/// first <see cref="GetOpenConnectionAsync"/> call. All data access in a request —
/// every module's DbContext and any raw SQL — shares this connection and
/// transaction (backend plan §6, D6).
/// </summary>
public interface IDbSession : IAsyncDisposable
{
    /// <summary>
    /// Opens the connection on first call: BEGIN + <c>set_config('app.current_tenant_id', …)</c>
    /// from the active tenant claim (or <c>app.is_platform_admin</c> for platform scope).
    /// No claim = no context: RLS is fail-closed and domain tables return no rows.
    /// </summary>
    Task<NpgsqlConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronous variant for contexts where async is not available — the EF Core
    /// DbContext DI factory. Prefer <see cref="GetOpenConnectionAsync"/> everywhere else.
    /// </summary>
    NpgsqlConnection GetOpenConnection();

    /// <summary>Null until the session is opened; null again after commit/rollback.</summary>
    NpgsqlTransaction? Transaction { get; }

    bool IsActive { get; }

    /// <summary>Commits the request transaction. No-op if the session was never opened.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back the request transaction. No-op if the session was never opened.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
