namespace WorkshopAdmin.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopAdmin.API.Authorization;
using WorkshopAdmin.Application.Features.Role;
using WorkshopAdmin.Application.Features.Role.Assignable;
using WorkshopAdmin.Application.Features.Role.Create;
using WorkshopAdmin.Application.Features.Role.GetById;
using WorkshopAdmin.Application.Features.Role.List;
using WorkshopAdmin.Application.Features.Role.Update;

// Tenant surface: authenticated + permission + service actor-scoping (no
// role-name gate), consistent with UsersController. Platform actors (no
// tenant) manage global roles; tenant actors manage their own custom roles.
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
    public async Task<ActionResult<IReadOnlyList<AssignableRoleItem>>> ListAssignable(CancellationToken cancellationToken)
        => Ok(await roleService.ListAssignableAsync(cancellationToken));

    [HttpGet]
    [HasPermission("roles:read")]
    public async Task<ActionResult<IReadOnlyList<RoleListItem>>> List(CancellationToken cancellationToken)
        => Ok(await roleService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission("roles:read")]
    public async Task<ActionResult<RoleDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await roleService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission("roles:create")]
    public async Task<ActionResult<CreateRoleResponse>> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        CreateRoleResponse result = await roleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.RoleId }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("roles:update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        await roleService.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("roles:delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await roleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
