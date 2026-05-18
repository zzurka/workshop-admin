namespace WorkshopAdmin.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopAdmin.API.Authorization;
using WorkshopAdmin.Application.Features.Role;
using WorkshopAdmin.Application.Features.Role.List;

// Tenant surface: authenticated + permission + service tenant-scoping (no
// role-name gate), consistent with UsersController.
[ApiController]
[Route("api/roles")]
[Authorize]
public sealed class RolesController(IRoleService roleService) : ControllerBase
{
    /// <summary>
    /// Roles the caller may assign to users (for create-user / assign-roles
    /// pickers): tenant-scoped global roles plus the caller's own tenant roles.
    /// </summary>
    [HttpGet("assignable")]
    [HasPermission("roles:read")]
    public async Task<ActionResult<IReadOnlyList<RoleListItem>>> ListAssignable(CancellationToken cancellationToken)
        => Ok(await roleService.ListAssignableAsync(cancellationToken));
}
