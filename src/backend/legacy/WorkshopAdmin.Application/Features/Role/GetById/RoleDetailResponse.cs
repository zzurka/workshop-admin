namespace WorkshopAdmin.Application.Features.Role.GetById;

using WorkshopAdmin.Application.Features.Permission.Models;

/// <summary>
/// Role detail. <see cref="Permissions"/> are the permissions granted to the
/// role; they are managed via the role-permission endpoints
/// (roles:assign_permissions).
/// </summary>
public sealed record RoleDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    string Scope,
    bool IsGlobal,
    bool IsSystem,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<PermissionItem> Permissions);
