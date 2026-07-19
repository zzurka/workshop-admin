namespace WorkshopAdmin.Application.Features.Role.Assignable;

/// <summary>
/// A role the calling actor may assign to users. <see cref="IsGlobal"/> is true
/// for built-in global roles (tenant_id IS NULL), false for the tenant's own
/// custom roles — lets the UI group them.
/// </summary>
public sealed record AssignableRoleItem(
    Guid Id,
    string Name,
    string? Description,
    bool IsGlobal);
