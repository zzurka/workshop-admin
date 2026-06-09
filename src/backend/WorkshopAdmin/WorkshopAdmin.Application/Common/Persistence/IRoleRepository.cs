namespace WorkshopAdmin.Application.Common.Persistence;

using System.Data;
using WorkshopAdmin.Application.Features.Role.Assignable;
using WorkshopAdmin.Application.Features.Role.List;
using WorkshopAdmin.Application.Features.Role.Models;

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

    /// <summary>
    /// Lists the roles a tenant actor may assign to users (tenant-scoped, global
    /// or own-tenant), ordered by name.
    /// </summary>
    Task<IReadOnlyList<AssignableRoleItem>> ListAssignableAsync(
        Guid tenantId,
        IDbConnection connection,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists roles visible to a tenant actor on the management surface:
    /// tenant-scoped global roles plus the tenant's own custom roles. Ordered by name.
    /// </summary>
    Task<IReadOnlyList<RoleListItem>> ListVisibleToTenantAsync(
        Guid tenantId,
        IDbConnection connection,
        CancellationToken cancellationToken);

    /// <summary>Lists all global roles (tenant_id IS NULL), ordered by name. Platform surface.</summary>
    Task<IReadOnlyList<RoleListItem>> ListGlobalAsync(
        IDbConnection connection,
        CancellationToken cancellationToken);

    /// <summary>Loads a non-deleted role by id, or null. Visibility is enforced by the caller.</summary>
    Task<RoleRecord?> GetByIdAsync(
        Guid id,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);

    /// <summary>Names of the non-deleted permissions granted to the role, ordered by name.</summary>
    Task<IReadOnlyList<string>> GetPermissionNamesAsync(
        Guid roleId,
        IDbConnection connection,
        CancellationToken cancellationToken);

    /// <summary>
    /// True when a non-deleted role with the name exists for the owner
    /// (<paramref name="tenantId"/> null = global). <paramref name="excludeRoleId"/>
    /// skips the role being renamed.
    /// </summary>
    Task<bool> NameExistsAsync(
        Guid? tenantId,
        string name,
        Guid? excludeRoleId,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);

    /// <summary>Inserts a role and returns its id.</summary>
    Task<Guid> CreateAsync(
        NewRole role,
        Guid createdBy,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);

    /// <summary>Updates name/description of a non-deleted role. False when no row matched.</summary>
    Task<bool> UpdateAsync(
        Guid id,
        string name,
        string? description,
        Guid updatedBy,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);

    /// <summary>Soft-deletes a role. False when no row matched.</summary>
    Task<bool> SoftDeleteAsync(
        Guid id,
        Guid updatedBy,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);

    /// <summary>True when the role is actively assigned (non-deleted user_roles row on a non-deleted user).</summary>
    Task<bool> HasActiveAssignmentsAsync(
        Guid roleId,
        IDbConnection connection,
        CancellationToken cancellationToken);
}
