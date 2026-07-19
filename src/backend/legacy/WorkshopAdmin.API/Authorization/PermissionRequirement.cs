namespace WorkshopAdmin.API.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>Requires the caller to hold a specific permission (e.g. <c>tenants:create</c>).</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
