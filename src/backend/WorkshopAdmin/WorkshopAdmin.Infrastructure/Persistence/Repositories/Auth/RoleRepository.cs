namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Auth;

using System.Data;
using Dapper;
using WorkshopAdmin.Application.Common.Interfaces;

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
}
