using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace WorkshopAdmin.SharedKernel.Auth;

/// <summary>
/// Reads the caller identity from the JWT claims of the current HTTP request.
/// Claim names must match what the Auth module issues (see backend plan §7).
/// </summary>
public sealed class ClaimsCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public const string TenantIdClaim = "tenant_id";
    public const string IsPlatformAdminClaim = "is_platform_admin";
    public const string PermissionClaim = "permission";

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => ParseGuid(Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub"));

    public Guid? TenantId => ParseGuid(Principal?.FindFirstValue(TenantIdClaim));

    public bool IsPlatformAdmin => Principal?.FindFirstValue(IsPlatformAdminClaim) == "true";

    public IReadOnlySet<string> Permissions =>
        Principal?.FindAll(PermissionClaim).Select(c => c.Value).ToHashSet() ?? [];

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out Guid parsed) ? parsed : null;
}
