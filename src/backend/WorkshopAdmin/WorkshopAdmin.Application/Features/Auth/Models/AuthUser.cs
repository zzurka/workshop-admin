namespace WorkshopAdmin.Application.Features.Auth.Models;

/// <summary>
/// A user row loaded for authentication, joined with the activation state of
/// the user's tenant (NULL tenant = platform-level super admin).
/// </summary>
public sealed class AuthUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? PasswordHash { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid? TenantId { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// TRUE/FALSE when the user belongs to a tenant; NULL for platform admins
    /// (TenantId is NULL) or when the tenant row is soft-deleted.
    /// </summary>
    public bool? TenantIsActive { get; set; }
}
