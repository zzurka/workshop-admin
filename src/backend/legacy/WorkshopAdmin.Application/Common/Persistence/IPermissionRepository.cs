namespace WorkshopAdmin.Application.Common.Persistence;

using System.Data;
using WorkshopAdmin.Application.Features.Permission.Models;

public interface IPermissionRepository
{
    /// <summary>
    /// Lists non-deleted permissions, ordered by name. With
    /// <paramref name="tenantScopeOnly"/> only scope='tenant' rows are returned
    /// (the catalog a tenant actor may see and grant).
    /// </summary>
    Task<IReadOnlyList<PermissionItem>> ListAsync(
        bool tenantScopeOnly,
        IDbConnection connection,
        CancellationToken cancellationToken);

    /// <summary>
    /// Of the given permission ids, returns those that exist (non-deleted) and —
    /// with <paramref name="tenantScopeOnly"/> — have scope='tenant'. Used to
    /// validate a grant batch against the target role's scope.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetGrantableIdsAsync(
        IReadOnlyCollection<Guid> permissionIds,
        bool tenantScopeOnly,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);
}
