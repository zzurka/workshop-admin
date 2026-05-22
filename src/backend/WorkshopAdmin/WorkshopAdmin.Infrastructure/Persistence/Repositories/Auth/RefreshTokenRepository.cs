namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Auth;

using Dapper;
using System.Data;
using WorkshopAdmin.Application.Common.Persistence;
using WorkshopAdmin.Application.Features.Auth.Models;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    public Task<Guid> InsertAsync(RefreshTokenRecord token, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO auth.refresh_tokens (user_id, token_hash, expires_at)
            VALUES (@UserId, @TokenHash, @ExpiresAt)
            RETURNING id
            """;

        return connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql,
            new { token.UserId, token.TokenHash, token.ExpiresAt },
            transaction,
            cancellationToken: cancellationToken));
    }

    public Task<RefreshTokenRecord?> FindByHashAsync(string tokenHash, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id,
                   user_id,
                   token_hash,
                   expires_at,
                   revoked_at,
                   replaced_by_token_id
            FROM auth.refresh_tokens
            WHERE token_hash = @TokenHash
            """;

        return connection.QuerySingleOrDefaultAsync<RefreshTokenRecord>(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken));
    }

    public Task RevokeAsync(Guid id, Guid? replacedByTokenId, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.refresh_tokens
            SET revoked_at = NOW(),
                replaced_by_token_id = @ReplacedByTokenId
            WHERE id = @Id
              AND revoked_at IS NULL
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, ReplacedByTokenId = replacedByTokenId },
            transaction,
            cancellationToken: cancellationToken));
    }

    public Task RevokeAllForUserAsync(Guid userId, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.refresh_tokens
            SET revoked_at = NOW()
            WHERE user_id = @UserId
              AND revoked_at IS NULL
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId },
            transaction,
            cancellationToken: cancellationToken));
    }
}
