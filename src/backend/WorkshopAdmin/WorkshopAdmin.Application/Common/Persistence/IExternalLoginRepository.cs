namespace WorkshopAdmin.Application.Common.Persistence;

using System.Data;
using WorkshopAdmin.Application.Features.Auth.Models;

public interface IExternalLoginRepository
{
    Task<ExternalLoginRecord?> FindAsync(
        string provider, string subject, IDbConnection connection, CancellationToken cancellationToken);

    /// <summary>Inserts a (user, provider, subject) link. Caller has already ensured no row exists for the user-provider pair.</summary>
    Task<Guid> InsertAsync(
        Guid userId, string provider, string subject, string? email,
        IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);

    Task UpdateLastLoginAsync(Guid id, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);
}
