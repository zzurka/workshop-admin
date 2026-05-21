namespace WorkshopAdmin.Application.Common.Interfaces;

using System.Data;
using WorkshopAdmin.Application.Features.Auth.Models;

public interface IPasswordResetTokenRepository
{
    Task<Guid> InsertAsync(
        Guid userId, string tokenHash, DateTime expiresAt,
        IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);

    Task<PasswordResetTokenRecord?> FindByHashAsync(
        string tokenHash, IDbConnection connection, CancellationToken cancellationToken);

    Task MarkUsedAsync(Guid id, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);
}
