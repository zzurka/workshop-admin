namespace WorkshopAdmin.Application.Features.User.AssignRoles;

/// <summary>Grants the listed roles to the user (additive, idempotent). Roles must be assignable by the caller.</summary>
public sealed record AssignRolesRequest(IReadOnlyList<Guid> RoleIds);
