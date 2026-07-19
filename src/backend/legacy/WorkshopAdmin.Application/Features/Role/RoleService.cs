namespace WorkshopAdmin.Application.Features.Role;

using FluentValidation;
using System.Data.Common;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Common.Persistence;
using WorkshopAdmin.Application.Features.Permission.Models;
using WorkshopAdmin.Application.Features.Role.AssignPermissions;
using WorkshopAdmin.Application.Features.Role.Assignable;
using WorkshopAdmin.Application.Features.Role.Create;
using WorkshopAdmin.Application.Features.Role.GetById;
using WorkshopAdmin.Application.Features.Role.List;
using WorkshopAdmin.Application.Features.Role.Models;
using WorkshopAdmin.Application.Features.Role.Update;
using WorkshopAdmin.Domain.Exceptions;

public sealed class RoleService(
    IDbConnectionFactory connectionFactory,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    ICurrentUserContext currentUser,
    ITenantContext tenantContext,
    IValidator<CreateRoleRequest> createValidator,
    IValidator<UpdateRoleRequest> updateValidator,
    IValidator<AssignPermissionsRequest> assignPermissionsValidator) : IRoleService
{
    private const string PlatformScope = "platform";
    private const string TenantScope = "tenant";
    private const string PlatformAdminRoleName = "platform_admin";

    public async Task<IReadOnlyList<AssignableRoleItem>> ListAssignableAsync(CancellationToken cancellationToken)
    {
        Guid tenantId = RequireTenant();

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await roleRepository.ListAssignableAsync(tenantId, connection, cancellationToken);
    }

    public async Task<IReadOnlyList<RoleListItem>> ListAsync(CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return tenantContext.TenantId is Guid tenantId
            ? await roleRepository.ListVisibleToTenantAsync(tenantId, connection, cancellationToken)
            : await roleRepository.ListGlobalAsync(connection, cancellationToken);
    }

    public async Task<RoleDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        RoleRecord role = await GetVisibleRoleAsync(id, connection, cancellationToken);
        IReadOnlyList<PermissionItem> permissions = await roleRepository.GetPermissionsAsync(id, connection, cancellationToken);

        return new RoleDetailResponse(
            role.Id, role.Name, role.Description, role.Scope,
            role.TenantId is null, role.IsSystem, role.CreatedAt, role.UpdatedAt, permissions);
    }

    public async Task<CreateRoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        Guid? tenantId = tenantContext.TenantId;
        string scope;
        if (tenantId is not null)
        {
            if (request.Scope == PlatformScope)
            {
                throw new BusinessRuleException("Only platform administrators can create platform-scoped roles.");
            }

            scope = TenantScope;
        }
        else
        {
            scope = request.Scope ?? TenantScope;
        }

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (await roleRepository.NameExistsAsync(tenantId, request.Name, null, connection, null, cancellationToken))
        {
            throw new ConflictException($"A role named '{request.Name}' already exists.");
        }

        Guid roleId = await roleRepository.CreateAsync(
            new NewRole(tenantId, request.Name, scope, request.Description),
            currentUser.UserId, connection, null, cancellationToken);

        return new CreateRoleResponse(roleId);
    }

    public async Task UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        RoleRecord role = await GetVisibleRoleAsync(id, connection, cancellationToken);
        RequireOwned(role);

        if (role.IsSystem)
        {
            throw new BusinessRuleException("System roles cannot be modified.");
        }

        if (!string.Equals(role.Name, request.Name, StringComparison.Ordinal)
            && await roleRepository.NameExistsAsync(role.TenantId, request.Name, id, connection, null, cancellationToken))
        {
            throw new ConflictException($"A role named '{request.Name}' already exists.");
        }

        await roleRepository.UpdateAsync(id, request.Name, request.Description, currentUser.UserId, connection, null, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        RoleRecord role = await GetVisibleRoleAsync(id, connection, cancellationToken);
        RequireOwned(role);

        if (role.IsSystem)
        {
            throw new BusinessRuleException("System roles cannot be deleted.");
        }

        if (await roleRepository.HasActiveAssignmentsAsync(id, connection, cancellationToken))
        {
            throw new BusinessRuleException("The role is assigned to one or more users. Remove it from those users first.");
        }

        await roleRepository.SoftDeleteAsync(id, currentUser.UserId, connection, null, cancellationToken);
    }

    public async Task AssignPermissionsAsync(Guid id, AssignPermissionsRequest request, CancellationToken cancellationToken)
    {
        await assignPermissionsValidator.ValidateAndThrowAsync(request, cancellationToken);

        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        RoleRecord role = await GetVisibleRoleAsync(id, connection, cancellationToken);
        RequireOwned(role);
        RequirePermissionsUnlocked(role);

        List<Guid> distinctIds = request.PermissionIds.Distinct().ToList();

        // Platform-scoped roles may hold permissions of any scope (platform_admin
        // holds tenant-scoped roles:* etc.); tenant-scoped roles only tenant ones.
        IReadOnlyList<Guid> grantable = await permissionRepository.GetGrantableIdsAsync(
            distinctIds, tenantScopeOnly: role.Scope == TenantScope, connection, null, cancellationToken);

        if (grantable.Count != distinctIds.Count)
        {
            throw new BusinessRuleException("One or more permissions cannot be assigned to this role.");
        }

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        
        try
        {
            foreach (Guid permissionId in distinctIds)
            {
                await roleRepository.AssignPermissionAsync(id, permissionId, currentUser.UserId, connection, transaction, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RemovePermissionAsync(Guid id, Guid permissionId, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        RoleRecord role = await GetVisibleRoleAsync(id, connection, cancellationToken);
        RequireOwned(role);
        RequirePermissionsUnlocked(role);

        await roleRepository.RemovePermissionAsync(id, permissionId, currentUser.UserId, connection, null, cancellationToken);
    }

    /// <summary>
    /// Loads a non-deleted role and applies the actor's visibility rule.
    /// Tenant actor: own-tenant roles plus tenant-scoped global roles. Platform
    /// actor: global roles only. Anything outside is NotFound (no existence leak).
    /// </summary>
    private async Task<RoleRecord> GetVisibleRoleAsync(Guid id, DbConnection connection, CancellationToken cancellationToken)
    {
        RoleRecord? role = await roleRepository.GetByIdAsync(id, connection, null, cancellationToken);

        bool visible = role is not null && (tenantContext.TenantId is Guid tenantId
            ? role.TenantId == tenantId || (role.TenantId is null && role.Scope == TenantScope)
            : role.TenantId is null);

        return visible ? role! : throw new NotFoundException("Role", id);
    }

    /// <summary>
    /// Tenant actors can see global roles but only platform actors may mutate
    /// them — visibility is intentional (pickers), so this is 403, not 404.
    /// </summary>
    private void RequireOwned(RoleRecord role)
    {
        if (tenantContext.TenantId is not null && role.TenantId is null)
        {
            throw new ForbiddenException("Global roles can only be managed by platform administrators.");
        }
    }

    /// <summary>
    /// The platform_admin role's permission set is immutable through the API —
    /// removing e.g. roles:assign_permissions from it would be self-lockout.
    /// Its matrix evolves via seed migrations only. Note: is_system alone does
    /// NOT lock permissions (built-in roles like mechanic stay tunable).
    /// </summary>
    private static void RequirePermissionsUnlocked(RoleRecord role)
    {
        if (role.IsSystem && role.Name == PlatformAdminRoleName)
        {
            throw new BusinessRuleException("The platform_admin role's permissions cannot be modified.");
        }
    }

    private Guid RequireTenant()
        => tenantContext.TenantId
           ?? throw new ForbiddenException("A tenant context is required to list assignable roles.");
}
