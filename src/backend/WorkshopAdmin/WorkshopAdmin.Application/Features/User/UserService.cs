namespace WorkshopAdmin.Application.Features.User;

using System.Data;
using System.Data.Common;
using FluentValidation;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Common.Models;
using WorkshopAdmin.Application.Features.Auth.Models;
using WorkshopAdmin.Application.Features.User.AssignRoles;
using WorkshopAdmin.Application.Features.User.Create;
using WorkshopAdmin.Application.Features.User.GetById;
using WorkshopAdmin.Application.Features.User.List;
using WorkshopAdmin.Application.Features.User.Models;
using WorkshopAdmin.Application.Features.User.ResetPassword;
using WorkshopAdmin.Application.Features.User.Update;
using WorkshopAdmin.Domain.Exceptions;

public sealed class UserService(
    IDbConnectionFactory connectionFactory,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    ICurrentUserContext currentUser,
    ITenantContext tenantContext,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRequest> updateValidator,
    IValidator<ResetPasswordRequest> resetPasswordValidator,
    IValidator<AssignRolesRequest> assignRolesValidator) : IUserService
{
    private const string TenantAdminRoleName = "tenant_admin";

    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["email"] = "email",
        ["first_name"] = "first_name",
        ["last_name"] = "last_name",
        ["created_at"] = "created_at",
        ["is_active"] = "is_active"
    };

    public async Task<PagedResponse<UserListItem>> ListAsync(ListUsersRequest request, CancellationToken cancellationToken)
    {
        Guid tenantId = RequireTenant();

        int limit = request.Limit is > 0 and <= 100 ? request.Limit : 25;
        int offset = request.Offset >= 0 ? request.Offset : 0;
        string sortBy = request.SortBy is not null && SortColumns.TryGetValue(request.SortBy, out string? column)
            ? column
            : "created_at";
        string sortDirection = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        IReadOnlyList<UserListItem> items = await userRepository.ListByTenantAsync(
            tenantId, request.Search, request.IsActive, offset, limit, sortBy, sortDirection, connection, cancellationToken);
        int total = await userRepository.CountByTenantAsync(tenantId, request.Search, request.IsActive, connection, cancellationToken);

        return new PagedResponse<UserListItem>
        {
            Items = items,
            TotalCount = total,
            Offset = offset,
            Limit = limit
        };
    }

    public async Task<UserDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Guid tenantId = RequireTenant();

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        UserRecord user = await userRepository.GetByIdInTenantAsync(id, tenantId, connection, cancellationToken)
            ?? throw new NotFoundException("User", id);

        IReadOnlyList<string> roles = await userRepository.GetRoleNamesAsync(id, connection, cancellationToken);

        return new UserDetailResponse(
            user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber,
            user.IsActive, user.CreatedAt, user.UpdatedAt, roles);
    }

    public async Task<CreateUserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        Guid tenantId = RequireTenant();
        Guid actingUserId = currentUser.UserId;
        string passwordHash = passwordHasher.Hash(request.Password);

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        Guid userId;
        try
        {
            if (await userRepository.EmailExistsAsync(request.Email, connection, transaction, cancellationToken))
            {
                throw new ConflictException($"A user with email '{request.Email}' already exists.");
            }

            userId = await userRepository.CreateAsync(
                new NewUser(request.Email, passwordHash, request.FirstName, request.LastName, tenantId),
                actingUserId, connection, transaction, cancellationToken);

            if (request.RoleIds is { Count: > 0 })
            {
                await AssignValidatedRolesAsync(userId, request.RoleIds, tenantId, actingUserId, connection, transaction, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new CreateUserResponse(userId);
    }

    public async Task UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        Guid tenantId = RequireTenant();

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        bool updated = await userRepository.UpdateProfileAsync(
            id, tenantId, request.FirstName, request.LastName, request.PhoneNumber,
            currentUser.UserId, connection, null, cancellationToken);

        if (!updated)
        {
            throw new NotFoundException("User", id);
        }
    }

    public async Task SetActivationAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        Guid tenantId = RequireTenant();
        Guid actingUserId = currentUser.UserId;

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (!await userRepository.ExistsInTenantAsync(id, tenantId, connection, null, cancellationToken))
        {
            throw new NotFoundException("User", id);
        }

        if (!isActive)
        {
            if (id == actingUserId)
            {
                throw new BusinessRuleException("You cannot deactivate your own account.");
            }

            await GuardLastTenantAdminAsync(id, tenantId, connection, cancellationToken);
        }

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await userRepository.SetActiveAsync(id, tenantId, isActive, actingUserId, connection, transaction, cancellationToken);

            if (!isActive)
            {
                await refreshTokenRepository.RevokeAllForUserAsync(id, connection, transaction, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Guid tenantId = RequireTenant();
        Guid actingUserId = currentUser.UserId;

        if (id == actingUserId)
        {
            throw new BusinessRuleException("You cannot delete your own account.");
        }

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (!await userRepository.ExistsInTenantAsync(id, tenantId, connection, null, cancellationToken))
        {
            throw new NotFoundException("User", id);
        }

        await GuardLastTenantAdminAsync(id, tenantId, connection, cancellationToken);

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await userRepository.SoftDeleteAsync(id, tenantId, actingUserId, connection, transaction, cancellationToken);
            await refreshTokenRepository.RevokeAllForUserAsync(id, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await resetPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);

        Guid tenantId = RequireTenant();
        Guid actingUserId = currentUser.UserId;
        string passwordHash = passwordHasher.Hash(request.NewPassword);

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (!await userRepository.ExistsInTenantAsync(id, tenantId, connection, null, cancellationToken))
        {
            throw new NotFoundException("User", id);
        }

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await userRepository.SetPasswordAsync(id, tenantId, passwordHash, actingUserId, connection, transaction, cancellationToken);
            // Force re-authentication everywhere after an admin password reset.
            await refreshTokenRepository.RevokeAllForUserAsync(id, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task AssignRolesAsync(Guid id, AssignRolesRequest request, CancellationToken cancellationToken)
    {
        await assignRolesValidator.ValidateAndThrowAsync(request, cancellationToken);

        Guid tenantId = RequireTenant();
        Guid actingUserId = currentUser.UserId;

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (!await userRepository.ExistsInTenantAsync(id, tenantId, connection, null, cancellationToken))
        {
            throw new NotFoundException("User", id);
        }

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await AssignValidatedRolesAsync(id, request.RoleIds, tenantId, actingUserId, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RemoveRoleAsync(Guid id, Guid roleId, CancellationToken cancellationToken)
    {
        Guid tenantId = RequireTenant();
        Guid actingUserId = currentUser.UserId;

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (!await userRepository.ExistsInTenantAsync(id, tenantId, connection, null, cancellationToken))
        {
            throw new NotFoundException("User", id);
        }

        // An actor may only remove a role they could also assign.
        IReadOnlyList<Guid> assignable = await roleRepository.GetAssignableIdsAsync(
            [roleId], tenantId, connection, null, cancellationToken);
        if (assignable.Count == 0)
        {
            throw new BusinessRuleException("That role is not assignable/removable by you in this tenant.");
        }

        // Removing tenant_admin from the last active tenant administrator would
        // lock the tenant out — same guard as deactivate/delete. (Self-demotion
        // is allowed as long as another active admin remains.)
        Guid? tenantAdminRoleId = await roleRepository.GetGlobalIdByNameAsync(
            TenantAdminRoleName, connection, null, cancellationToken);
        if (tenantAdminRoleId == roleId)
        {
            await GuardLastTenantAdminAsync(id, tenantId, connection, cancellationToken);
        }

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await userRepository.RemoveRoleAsync(id, roleId, actingUserId, connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Validates that every requested role is assignable by a tenant actor
    /// (tenant-scoped, global or own-tenant) and grants them. Throws if any
    /// requested role is not assignable.
    /// </summary>
    private async Task AssignValidatedRolesAsync(
        Guid userId, IReadOnlyList<Guid> requestedRoleIds, Guid tenantId, Guid actingUserId,
        IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        List<Guid> distinctIds = requestedRoleIds.Distinct().ToList();

        IReadOnlyList<Guid> assignable = await roleRepository.GetAssignableIdsAsync(
            distinctIds, tenantId, connection, transaction, cancellationToken);

        if (assignable.Count != distinctIds.Count)
        {
            throw new BusinessRuleException("One or more roles are not assignable to users in this tenant.");
        }

        foreach (Guid roleId in distinctIds)
        {
            await userRepository.AssignRoleAsync(userId, roleId, actingUserId, connection, transaction, cancellationToken);
        }
    }

    private async Task GuardLastTenantAdminAsync(Guid targetUserId, Guid tenantId, IDbConnection connection, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> targetRoles = await userRepository.GetRoleNamesAsync(targetUserId, connection, cancellationToken);
        if (!targetRoles.Contains(TenantAdminRoleName))
        {
            return;
        }

        int otherActiveAdmins = await userRepository.CountActiveByRoleAsync(
            tenantId, TenantAdminRoleName, targetUserId, connection, null, cancellationToken);

        if (otherActiveAdmins == 0)
        {
            throw new BusinessRuleException("Cannot deactivate or delete the last active tenant administrator.");
        }
    }

    private Guid RequireTenant()
        => tenantContext.TenantId
           ?? throw new ForbiddenException("A tenant context is required for user administration.");
}
