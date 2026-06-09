namespace WorkshopAdmin.Application.Features.Role.Update;

/// <summary>
/// Updates a role's name and description. Scope and tenant ownership are
/// immutable after creation.
/// </summary>
public sealed record UpdateRoleRequest(
    string Name,
    string? Description);
