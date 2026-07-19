namespace WorkshopAdmin.API.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Declarative permission gate. Combine with a controller-level
/// <c>[Authorize(Roles = ...)]</c> for the double-gate model (role boundary +
/// fine-grained permission).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(PermissionPolicyProvider.PolicyPrefix + permission)
{
}
