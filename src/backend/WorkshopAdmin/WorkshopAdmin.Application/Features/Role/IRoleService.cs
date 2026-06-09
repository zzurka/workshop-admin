namespace WorkshopAdmin.Application.Features.Role;

using WorkshopAdmin.Application.Features.Role.Assignable;
using WorkshopAdmin.Application.Features.Role.Create;
using WorkshopAdmin.Application.Features.Role.GetById;
using WorkshopAdmin.Application.Features.Role.List;
using WorkshopAdmin.Application.Features.Role.Update;

public interface IRoleService
{
    /// <summary>
    /// Roles the calling tenant actor may assign to users: tenant-scoped roles
    /// that are global or owned by the actor's tenant. Ordered by name.
    /// </summary>
    Task<IReadOnlyList<AssignableRoleItem>> ListAssignableAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Roles visible to the calling actor for management. Tenant actor:
    /// tenant-scoped global roles plus own custom roles. Platform actor: all
    /// global roles. Ordered by name.
    /// </summary>
    Task<IReadOnlyList<RoleListItem>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Role detail incl. read-only permission names. NotFound outside the actor's visibility.</summary>
    Task<RoleDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a role. Tenant actor: custom role in own tenant, scope forced to
    /// 'tenant'. Platform actor: global role, scope from the request
    /// (default 'tenant').
    /// </summary>
    Task<CreateRoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken);

    /// <summary>Updates name/description. System roles and roles the actor does not own are rejected.</summary>
    Task UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken);

    /// <summary>Soft-deletes a role. Rejected for system roles and roles still assigned to active users.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
