namespace WorkshopAdmin.Application.Features.Role.GetById;

/// <summary>
/// Role detail. <see cref="Permissions"/> is the read-only list of permission
/// names granted to the role — managing them is a separate surface
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
    IReadOnlyList<string> Permissions);
