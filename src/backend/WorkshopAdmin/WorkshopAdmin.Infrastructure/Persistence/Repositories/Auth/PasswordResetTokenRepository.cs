namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Auth;

using System.Data;
using Dapper;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Auth.Models;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    public Task<Guid> InsertAsync(
        Guid userId, string tokenHash, DateTime expiresAt,
        IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO auth.password_reset_tokens (user_id, token_hash, expires_at)
            VALUES (@UserId, @TokenHash, @ExpiresAt)
            RETURNING id
            """;

        return connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql,
            new { UserId = userId, TokenHash = tokenHash, ExpiresAt = expiresAt },
            transaction,
            cancellationToken: cancellationToken));
    }

    public Task<PasswordResetTokenRecord?> FindByHashAsync(
        string tokenHash, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, user_id AS UserId, token_hash AS TokenHash, expires_at AS ExpiresAt, used_at AS UsedAt
            FROM auth.password_reset_tokens
            WHERE token_hash = @TokenHash
            """;

        return connection.QuerySingleOrDefaultAsync<PasswordResetTokenRecord?>(new CommandDefinition(
            sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken));
    }

    public Task MarkUsedAsync(Guid id, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.password_reset_tokens
            SET used_at = NOW()
            WHERE id = @Id AND used_at IS NULL
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id }, transaction, cancellationToken: cancellationToken));
    }
}
