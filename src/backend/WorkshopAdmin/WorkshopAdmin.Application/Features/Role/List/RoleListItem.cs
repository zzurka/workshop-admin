namespace WorkshopAdmin.Application.Features.Role.List;

/// <summary>
/// A role visible to the calling actor on the management surface. Tenant actors
/// see tenant-scoped global roles (read-only for them) plus their own custom
/// roles; platform actors see all global roles.
/// </summary>
public sealed record RoleListItem(
    Guid Id,
    string Name,
    string? Description,
    string Scope,
    bool IsGlobal,
    bool IsSystem);
