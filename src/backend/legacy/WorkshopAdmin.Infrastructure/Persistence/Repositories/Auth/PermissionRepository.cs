namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Auth;

using Dapper;
using System.Data;
using WorkshopAdmin.Application.Common.Persistence;
using WorkshopAdmin.Application.Features.Permission.Models;

public sealed class PermissionRepository : IPermissionRepository
{
    public async Task<IReadOnlyList<PermissionItem>> ListAsync(
        bool tenantScopeOnly,
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, name, resource, action, scope, description
            FROM auth.permissions
            WHERE is_deleted = FALSE
              AND (@TenantScopeOnly = FALSE OR scope = 'tenant')
            ORDER BY name
            """;

        var rows = await connection.QueryAsync<PermissionItem>(
            new CommandDefinition(sql, new { TenantScopeOnly = tenantScopeOnly }, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Guid>> GetGrantableIdsAsync(
        IReadOnlyCollection<Guid> permissionIds,
        bool tenantScopeOnly,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (permissionIds.Count == 0)
        {
            return [];
        }

        const string sql = """
            SELECT id
            FROM auth.permissions
            WHERE id = ANY(@PermissionIds)
              AND is_deleted = FALSE
              AND (@TenantScopeOnly = FALSE OR scope = 'tenant')
            """;

        var rows = await connection.QueryAsync<Guid>(new CommandDefinition(
            sql,
            new { PermissionIds = permissionIds.ToArray(), TenantScopeOnly = tenantScopeOnly },
            transaction,
            cancellationToken: cancellationToken));
        return rows.AsList();
    }
}
