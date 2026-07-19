namespace WorkshopAdmin.Application.Features.Permission;

using WorkshopAdmin.Application.Features.Permission.Models;

public interface IPermissionService
{
    /// <summary>
    /// The permission catalog visible to the calling actor (for role-permission
    /// pickers): tenant actors see scope='tenant' permissions only; platform
    /// actors see all. Ordered by name.
    /// </summary>
    Task<IReadOnlyList<PermissionItem>> ListAsync(CancellationToken cancellationToken);
}
