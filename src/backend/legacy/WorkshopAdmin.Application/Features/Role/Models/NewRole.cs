namespace WorkshopAdmin.Application.Features.Role.Models;

/// <summary>
/// Columns for a new role row. <see cref="TenantId"/> is null for global roles
/// (platform actor); set for tenant custom roles. Custom roles are always
/// scope='tenant' — the service enforces this before constructing the model.
/// </summary>
public sealed record NewRole(
    Guid? TenantId,
    string Name,
    string Scope,
    string? Description);
