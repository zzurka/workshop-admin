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
}
