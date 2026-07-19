namespace WorkshopAdmin.API.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Succeeds when the authenticated principal carries a <c>permission</c> claim
/// whose value matches the required permission. Permission claims are embedded
/// in the access token at login (see JwtTokenService).
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    public const string PermissionClaimType = "permission";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        bool hasPermission = context.User.Claims.Any(c =>
            c.Type == PermissionClaimType &&
            string.Equals(c.Value, requirement.Permission, StringComparison.Ordinal));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
