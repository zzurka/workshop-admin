namespace WorkshopAdmin.Application.Common.Interfaces;

using System.Data;
using WorkshopAdmin.Application.Features.Auth.Models;

public interface IRefreshTokenRepository
{
    /// <summary>Inserts a new refresh token row and returns its generated id.</summary>
    Task<Guid> InsertAsync(RefreshTokenRecord token, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);

    Task<RefreshTokenRecord?> FindByHashAsync(string tokenHash, IDbConnection connection, CancellationToken cancellationToken);

    /// <summary>Marks a single active token revoked, optionally linking the token that replaced it.</summary>
    Task RevokeAsync(Guid id, Guid? replacedByTokenId, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);

    /// <summary>Revokes every active refresh token for a user (used on reuse / theft detection).</summary>
    Task RevokeAllForUserAsync(Guid userId, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);
}
