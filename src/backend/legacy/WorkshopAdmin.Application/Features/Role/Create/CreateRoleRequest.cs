namespace WorkshopAdmin.Application.Features.Role.Create;

/// <summary>
/// Creates a role. Tenant actors create custom roles in their own tenant and
/// must leave <see cref="Scope"/> null or 'tenant'. Platform actors create
/// global roles and may pass 'platform' for a platform-scoped role (defaults
/// to 'tenant' when omitted).
/// </summary>
public sealed record CreateRoleRequest(
    string Name,
    string? Description,
    string? Scope);
