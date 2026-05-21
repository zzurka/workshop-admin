namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Auth;

using System.Data;
using Dapper;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Auth.Models;

public sealed class ExternalLoginRepository : IExternalLoginRepository
{
    public Task<ExternalLoginRecord?> FindAsync(
        string provider, string subject, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, user_id AS UserId, provider, subject, email
            FROM auth.external_logins
            WHERE provider = @Provider
              AND subject = @Subject
            """;

        return connection.QuerySingleOrDefaultAsync<ExternalLoginRecord?>(new CommandDefinition(
            sql, new { Provider = provider, Subject = subject }, cancellationToken: cancellationToken));
    }

    public Task<Guid> InsertAsync(
        Guid userId, string provider, string subject, string? email,
        IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO auth.external_logins (user_id, provider, subject, email, last_login_at, created_by)
            VALUES (@UserId, @Provider, @Subject, @Email, NOW(), @UserId)
            RETURNING id
            """;

        return connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql,
            new { UserId = userId, Provider = provider, Subject = subject, Email = email },
            transaction, cancellationToken: cancellationToken));
    }

    public Task UpdateLastLoginAsync(Guid id, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.external_logins
            SET last_login_at = NOW()
            WHERE id = @Id
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id }, transaction, cancellationToken: cancellationToken));
    }
}
