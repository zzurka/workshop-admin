namespace WorkshopAdmin.Application.Features.Permission.Models;

/// <summary>
/// A permission as exposed to clients — in the catalog picker and on the role
/// detail. <see cref="Scope"/> 'platform' permissions can only be held by
/// platform-scoped roles.
/// </summary>
public sealed record PermissionItem(
    Guid Id,
    string Name,
    string Resource,
    string Action,
    string Scope,
    string? Description);
