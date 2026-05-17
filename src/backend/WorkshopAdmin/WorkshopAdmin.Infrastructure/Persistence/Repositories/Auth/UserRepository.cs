namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Auth;

using System.Data;
using Dapper;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Auth.Models;

public sealed class UserRepository : IUserRepository
{
    private const string SelectAuthUser = """
        SELECT u.id,
               u.email,
               u.password_hash,
               u.first_name,
               u.last_name,
               u.tenant_id,
               u.is_active,
               t.is_active AS tenant_is_active
        FROM auth.users u
        LEFT JOIN tenant.tenants t ON t.id = u.tenant_id AND t.is_deleted = FALSE
        """;

    public Task<AuthUser?> FindByEmailAsync(string email, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = SelectAuthUser + """

            WHERE LOWER(u.email) = LOWER(@Email)
              AND u.is_deleted = FALSE
            """;

        return connection.QuerySingleOrDefaultAsync<AuthUser>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    public Task<AuthUser?> FindByIdAsync(Guid userId, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = SelectAuthUser + """

            WHERE u.id = @UserId
              AND u.is_deleted = FALSE
            """;

        return connection.QuerySingleOrDefaultAsync<AuthUser>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT DISTINCT r.name
            FROM auth.user_roles ur
            JOIN auth.roles r ON r.id = ur.role_id AND r.is_deleted = FALSE
            WHERE ur.user_id = @UserId
              AND ur.is_deleted = FALSE
            ORDER BY r.name
            """;

        var rows = await connection.QueryAsync<string>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<string>> GetPermissionNamesAsync(Guid userId, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT DISTINCT p.name
            FROM auth.user_roles ur
            JOIN auth.roles r ON r.id = ur.role_id AND r.is_deleted = FALSE
            JOIN auth.role_permissions rp ON rp.role_id = r.id AND rp.is_deleted = FALSE
            JOIN auth.permissions p ON p.id = rp.permission_id AND p.is_deleted = FALSE
            WHERE ur.user_id = @UserId
              AND ur.is_deleted = FALSE
            ORDER BY p.name
            """;

        var rows = await connection.QueryAsync<string>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public Task<bool> EmailExistsAsync(string email, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM auth.users
                WHERE LOWER(email) = LOWER(@Email) AND is_deleted = FALSE
            )
            """;

        return connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Email = email }, transaction, cancellationToken: cancellationToken));
    }

    public Task<Guid> CreateAsync(NewUser user, Guid createdBy, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO auth.users (email, password_hash, first_name, last_name, tenant_id, created_by)
            VALUES (@Email, @PasswordHash, @FirstName, @LastName, @TenantId, @CreatedBy)
            RETURNING id
            """;

        return connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql,
            new
            {
                user.Email,
                user.PasswordHash,
                user.FirstName,
                user.LastName,
                user.TenantId,
                CreatedBy = createdBy
            },
            transaction,
            cancellationToken: cancellationToken));
    }

    public Task AssignRoleAsync(Guid userId, Guid roleId, Guid createdBy, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO auth.user_roles (user_id, role_id, created_by)
            VALUES (@UserId, @RoleId, @CreatedBy)
            ON CONFLICT (user_id, role_id)
            DO UPDATE SET is_deleted = FALSE, updated_at = NOW(), updated_by = @CreatedBy
            WHERE auth.user_roles.is_deleted = TRUE
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, RoleId = roleId, CreatedBy = createdBy },
            transaction,
            cancellationToken: cancellationToken));
    }
}
