namespace WorkshopAdmin.Application.Features.User;

using WorkshopAdmin.Application.Common.Models;
using WorkshopAdmin.Application.Features.User.AssignRoles;
using WorkshopAdmin.Application.Features.User.Create;
using WorkshopAdmin.Application.Features.User.GetById;
using WorkshopAdmin.Application.Features.User.List;
using WorkshopAdmin.Application.Features.User.ResetPassword;
using WorkshopAdmin.Application.Features.User.Update;

/// <summary>
/// Tenant-scoped user administration. Every operation is bounded to the
/// caller's tenant (resolved from the token); cross-tenant ids are treated as
/// not found.
/// </summary>
public interface IUserService
{
    Task<PagedResponse<UserListItem>> ListAsync(ListUsersRequest request, CancellationToken cancellationToken);

    Task<UserDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CreateUserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);

    Task SetActivationAsync(Guid id, bool isActive, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken);

    Task AssignRolesAsync(Guid id, AssignRolesRequest request, CancellationToken cancellationToken);

    Task RemoveRoleAsync(Guid id, Guid roleId, CancellationToken cancellationToken);
}
