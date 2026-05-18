namespace WorkshopAdmin.Application.Features.Role;

using WorkshopAdmin.Application.Features.Role.List;

public interface IRoleService
{
    /// <summary>
    /// Roles the calling tenant actor may assign to users: tenant-scoped roles
    /// that are global or owned by the actor's tenant. Ordered by name.
    /// </summary>
    Task<IReadOnlyList<RoleListItem>> ListAssignableAsync(CancellationToken cancellationToken);
}
