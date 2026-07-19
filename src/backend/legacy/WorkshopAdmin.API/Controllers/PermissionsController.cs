namespace WorkshopAdmin.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopAdmin.API.Authorization;
using WorkshopAdmin.Application.Features.Permission;
using WorkshopAdmin.Application.Features.Permission.Models;

// Tenant surface: authenticated + permission + service actor-scoping,
// consistent with RolesController. The catalog is read-only — permissions are
// defined by seed migrations, never through the API.
[ApiController]
[Route("api/permissions")]
[Authorize]
public sealed class PermissionsController(IPermissionService permissionService) : ControllerBase
{
    /// <summary>
    /// The permission catalog for role-permission pickers. Tenant actors see
    /// tenant-scoped permissions only; platform actors see all.
    /// </summary>
    [HttpGet]
    [HasPermission("roles:read")]
    public async Task<ActionResult<IReadOnlyList<PermissionItem>>> List(CancellationToken cancellationToken)
        => Ok(await permissionService.ListAsync(cancellationToken));
}
