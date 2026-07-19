namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Auth;

using Dapper;
using System.Data;
using WorkshopAdmin.Application.Common.Persistence;
using WorkshopAdmin.Application.Features.Permission.Models;
using WorkshopAdmin.Application.Features.Role.Assignable;
using WorkshopAdmin.Application.Features.Role.List;
using WorkshopAdmin.Application.Features.Role.Models;

public sealed class RoleRepository : IRoleRepository
{
    public Task<Guid?> GetGlobalIdByNameAsync(
        string roleName,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id FROM auth.roles
            WHERE name = @RoleName AND tenant_id IS NULL AND is_deleted = FALSE
            """;

        return connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(sql, new { RoleName = roleName }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Guid>> GetAssignableIdsAsync(
        IReadOnlyCollection<Guid> roleIds,
        Guid tenantId,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        // Assignable by a tenant actor: tenant-scoped roles that are global
        // (tenant_id IS NULL) or owned by the actor's tenant. Platform-scoped
        // roles (e.g. platform_admin) are excluded by scope = 'tenant'.
        const string sql = """
            SELECT id
            FROM auth.roles
            WHERE id = ANY(@RoleIds)
              AND scope = 'tenant'
              AND (tenant_id IS NULL OR tenant_id = @TenantId)
              AND is_deleted = FALSE
            """;

        var rows = await connection.QueryAsync<Guid>(new CommandDefinition(
            sql,
            new { RoleIds = roleIds.ToArray(), TenantId = tenantId },
            transaction,
            cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<AssignableRoleItem>> ListAssignableAsync(
        Guid tenantId,
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id,
                   name,
                   description,
                   (tenant_id IS NULL) AS is_global
            FROM auth.roles
            WHERE scope = 'tenant'
              AND (tenant_id IS NULL OR tenant_id = @TenantId)
              AND is_deleted = FALSE
            ORDER BY name
            """;

        var rows = await connection.QueryAsync<AssignableRoleItem>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<RoleListItem>> ListVisibleToTenantAsync(
        Guid tenantId,
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        // Management view for a tenant actor: own custom roles plus the
        // tenant-scoped global roles (read-only for them). Platform-scoped
        // global roles (e.g. platform_admin) stay invisible.
        const string sql = """
            SELECT id,
                   name,
                   description,
                   scope,
                   (tenant_id IS NULL) AS is_global,
                   is_system
            FROM auth.roles
            WHERE (tenant_id = @TenantId OR (tenant_id IS NULL AND scope = 'tenant'))
              AND is_deleted = FALSE
            ORDER BY name
            """;

        var rows = await connection.QueryAsync<RoleListItem>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<RoleListItem>> ListGlobalAsync(
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id,
                   name,
                   description,
                   scope,
                   (tenant_id IS NULL) AS is_global,
                   is_system
            FROM auth.roles
            WHERE tenant_id IS NULL
              AND is_deleted = FALSE
            ORDER BY name
            """;

        var rows = await connection.QueryAsync<RoleListItem>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public Task<RoleRecord?> GetByIdAsync(
        Guid id,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, tenant_id, name, scope, description, is_system, created_at, updated_at
            FROM auth.roles
            WHERE id = @Id AND is_deleted = FALSE
            """;

        return connection.QuerySingleOrDefaultAsync<RoleRecord>(
            new CommandDefinition(sql, new { Id = id }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<PermissionItem>> GetPermissionsAsync(
        Guid roleId,
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT p.id, p.name, p.resource, p.action, p.scope, p.description
            FROM auth.role_permissions rp
            JOIN auth.permissions p ON p.id = rp.permission_id AND p.is_deleted = FALSE
            WHERE rp.role_id = @RoleId
              AND rp.is_deleted = FALSE
            ORDER BY p.name
            """;

        var rows = await connection.QueryAsync<PermissionItem>(
            new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public Task AssignPermissionAsync(
        Guid roleId,
        Guid permissionId,
        Guid createdBy,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        // Idempotent grant, mirrors UserRepository.AssignRoleAsync: an active
        // row is untouched, a soft-deleted row is revived.
        const string sql = """
            INSERT INTO auth.role_permissions (role_id, permission_id, created_by)
            VALUES (@RoleId, @PermissionId, @CreatedBy)
            ON CONFLICT (role_id, permission_id)
            DO UPDATE SET is_deleted = FALSE, updated_at = NOW(), updated_by = @CreatedBy
            WHERE auth.role_permissions.is_deleted = TRUE
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { RoleId = roleId, PermissionId = permissionId, CreatedBy = createdBy },
            transaction,
            cancellationToken: cancellationToken));
    }

    public Task RemovePermissionAsync(
        Guid roleId,
        Guid permissionId,
        Guid updatedBy,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.role_permissions
            SET is_deleted = TRUE, updated_at = NOW(), updated_by = @UpdatedBy
            WHERE role_id = @RoleId AND permission_id = @PermissionId AND is_deleted = FALSE
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { RoleId = roleId, PermissionId = permissionId, UpdatedBy = updatedBy },
            transaction,
            cancellationToken: cancellationToken));
    }

    public Task<bool> NameExistsAsync(
        Guid? tenantId,
        string name,
        Guid? excludeRoleId,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        // IS NOT DISTINCT FROM matches the NULLS NOT DISTINCT unique index:
        // tenantId null checks the global namespace.
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM auth.roles
                WHERE tenant_id IS NOT DISTINCT FROM @TenantId
                  AND name = @Name
                  AND is_deleted = FALSE
                  AND (@ExcludeRoleId IS NULL OR id <> @ExcludeRoleId)
            )
            """;

        return connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql,
            new { TenantId = tenantId, Name = name, ExcludeRoleId = excludeRoleId },
            transaction,
            cancellationToken: cancellationToken));
    }

    public Task<Guid> CreateAsync(
        NewRole role,
        Guid createdBy,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO auth.roles (tenant_id, name, scope, description, created_by)
            VALUES (@TenantId, @Name, @Scope, @Description, @CreatedBy)
            RETURNING id
            """;

        return connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql,
            new { role.TenantId, role.Name, role.Scope, role.Description, CreatedBy = createdBy },
            transaction,
            cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        string name,
        string? description,
        Guid updatedBy,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.roles
            SET name = @Name,
                description = @Description,
                updated_at = NOW(),
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = FALSE
            """;

        int affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, Name = name, Description = description, UpdatedBy = updatedBy },
            transaction,
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<bool> SoftDeleteAsync(
        Guid id,
        Guid updatedBy,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.roles
            SET is_deleted = TRUE,
                updated_at = NOW(),
                updated_by = @UpdatedBy
            WHERE id = @Id AND is_deleted = FALSE
            """;

        int affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, UpdatedBy = updatedBy },
            transaction,
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    public Task<bool> HasActiveAssignmentsAsync(
        Guid roleId,
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        // Assignments held by soft-deleted users don't count — they can't act
        // on the role and shouldn't block its deletion.
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM auth.user_roles ur
                JOIN auth.users u ON u.id = ur.user_id AND u.is_deleted = FALSE
                WHERE ur.role_id = @RoleId
                  AND ur.is_deleted = FALSE
            )
            """;

        return connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: cancellationToken));
    }
}
