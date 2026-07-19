namespace WorkshopAdmin.Application.Features.Role.AssignPermissions;

/// <summary>
/// Grants the listed permissions to the role (additive, idempotent). Every
/// permission must exist and match the role's scope rule: tenant-scoped roles
/// only hold scope='tenant' permissions.
/// </summary>
public sealed record AssignPermissionsRequest(IReadOnlyList<Guid> PermissionIds);
