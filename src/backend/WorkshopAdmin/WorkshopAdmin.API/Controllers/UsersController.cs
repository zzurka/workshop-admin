namespace WorkshopAdmin.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopAdmin.API.Authorization;
using WorkshopAdmin.Application.Common.Models;
using WorkshopAdmin.Application.Features.User;
using WorkshopAdmin.Application.Features.User.Activation;
using WorkshopAdmin.Application.Features.User.AssignRoles;
using WorkshopAdmin.Application.Features.User.Create;
using WorkshopAdmin.Application.Features.User.GetById;
using WorkshopAdmin.Application.Features.User.List;
using WorkshopAdmin.Application.Features.User.ResetPassword;
using WorkshopAdmin.Application.Features.User.Update;

// Authenticated only at the controller level: users:* are tenant-scoped, so the
// boundary is the permission + service tenant-scoping, not a fixed role name
// (custom tenant roles may legitimately hold these permissions).
[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [HasPermission("users:read")]
    public async Task<ActionResult<PagedResponse<UserListItem>>> List([FromQuery] ListUsersRequest request, CancellationToken cancellationToken)
        => Ok(await userService.ListAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission("users:read")]
    public async Task<ActionResult<UserDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await userService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [HasPermission("users:create")]
    public async Task<ActionResult<CreateUserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        CreateUserResponse result = await userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.UserId }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("users:update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await userService.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activation")]
    [HasPermission("users:deactivate")]
    public async Task<IActionResult> SetActivation(Guid id, [FromBody] SetUserActivationRequest request, CancellationToken cancellationToken)
    {
        await userService.SetActivationAsync(id, request.IsActive, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("users:delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await userService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    [HasPermission("users:reset_password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await userService.ResetPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/roles")]
    [HasPermission("users:assign_roles")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesRequest request, CancellationToken cancellationToken)
    {
        await userService.AssignRolesAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [HasPermission("users:assign_roles")]
    public async Task<IActionResult> RemoveRole(Guid id, Guid roleId, CancellationToken cancellationToken)
    {
        await userService.RemoveRoleAsync(id, roleId, cancellationToken);
        return NoContent();
    }
}
