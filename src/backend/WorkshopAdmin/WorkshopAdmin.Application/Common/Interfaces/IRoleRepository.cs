namespace WorkshopAdmin.Application.Common.Interfaces;

using System.Data;

public interface IRoleRepository
{
    /// <summary>
    /// Resolves a global role (tenant_id IS NULL, not deleted) id by its stable
    /// name, or null if none matches.
    /// </summary>
    Task<Guid?> GetGlobalIdByNameAsync(
        string roleName,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);

    /// <summary>
    /// Of the given role ids, returns those a tenant actor may assign: tenant-scoped
    /// roles that are either global (tenant_id IS NULL) or owned by the actor's
    /// tenant. Platform-scoped roles (e.g. platform_admin) are never returned.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAssignableIdsAsync(
        IReadOnlyCollection<Guid> roleIds,
        Guid tenantId,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);
}
