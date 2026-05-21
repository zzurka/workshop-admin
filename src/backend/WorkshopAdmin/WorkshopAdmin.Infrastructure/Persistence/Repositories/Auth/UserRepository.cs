namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Auth;

using System.Data;
using Dapper;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Auth.Models;
using WorkshopAdmin.Application.Features.User.List;
using WorkshopAdmin.Application.Features.User.Models;

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

    public async Task<bool> UpdatePasswordHashAsync(
        Guid userId, string passwordHash, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.users
            SET password_hash = @PasswordHash,
                updated_at = NOW(),
                updated_by = @UserId
            WHERE id = @UserId AND is_deleted = FALSE
            """;

        int affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, PasswordHash = passwordHash },
            transaction,
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    public Task RemoveRoleAsync(Guid userId, Guid roleId, Guid updatedBy, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.user_roles
            SET is_deleted = TRUE, updated_at = NOW(), updated_by = @UpdatedBy
            WHERE user_id = @UserId AND role_id = @RoleId AND is_deleted = FALSE
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, RoleId = roleId, UpdatedBy = updatedBy },
            transaction,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<UserListItem>> ListByTenantAsync(
        Guid tenantId, string? search, bool? isActive, int offset, int limit, string sortBy, string sortDirection,
        IDbConnection connection, CancellationToken cancellationToken)
    {
        // sortBy / sortDirection are whitelisted by UserService — safe to interpolate.
        string sql = $"""
            SELECT id, email, first_name, last_name, is_active, created_at
            FROM auth.users
            WHERE tenant_id = @TenantId
              AND is_deleted = FALSE
              AND (@Search IS NULL OR email ILIKE @Pattern OR first_name ILIKE @Pattern OR last_name ILIKE @Pattern)
              AND (@IsActive IS NULL OR is_active = @IsActive)
            ORDER BY {sortBy} {sortDirection}
            OFFSET @Offset LIMIT @Limit
            """;

        var rows = await connection.QueryAsync<UserListItem>(new CommandDefinition(
            sql,
            new
            {
                TenantId = tenantId,
                Search = search,
                Pattern = $"%{search}%",
                IsActive = isActive,
                Offset = offset,
                Limit = limit
            },
            cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public Task<int> CountByTenantAsync(Guid tenantId, string? search, bool? isActive, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM auth.users
            WHERE tenant_id = @TenantId
              AND is_deleted = FALSE
              AND (@Search IS NULL OR email ILIKE @Pattern OR first_name ILIKE @Pattern OR last_name ILIKE @Pattern)
              AND (@IsActive IS NULL OR is_active = @IsActive)
            """;

        return connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new { TenantId = tenantId, Search = search, Pattern = $"%{search}%", IsActive = isActive },
            cancellationToken: cancellationToken));
    }

    public Task<UserRecord?> GetByIdInTenantAsync(Guid id, Guid tenantId, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, email, first_name, last_name, phone_number, is_active, created_at, updated_at
            FROM auth.users
            WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = FALSE
            """;

        return connection.QuerySingleOrDefaultAsync<UserRecord>(
            new CommandDefinition(sql, new { Id = id, TenantId = tenantId }, cancellationToken: cancellationToken));
    }

    public Task<bool> ExistsInTenantAsync(Guid id, Guid tenantId, IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM auth.users
                WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = FALSE
            )
            """;

        return connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Id = id, TenantId = tenantId }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateProfileAsync(
        Guid id, Guid tenantId, string firstName, string lastName, string? phoneNumber, Guid updatedBy,
        IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.users
            SET first_name   = @FirstName,
                last_name    = @LastName,
                phone_number = @PhoneNumber,
                updated_at   = NOW(),
                updated_by   = @UpdatedBy
            WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = FALSE
            """;

        int affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, TenantId = tenantId, FirstName = firstName, LastName = lastName, PhoneNumber = phoneNumber, UpdatedBy = updatedBy },
            transaction,
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<bool> SetActiveAsync(
        Guid id, Guid tenantId, bool isActive, Guid updatedBy,
        IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.users
            SET is_active  = @IsActive,
                updated_at = NOW(),
                updated_by = @UpdatedBy
            WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = FALSE
            """;

        int affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, TenantId = tenantId, IsActive = isActive, UpdatedBy = updatedBy },
            transaction,
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<bool> SoftDeleteAsync(
        Guid id, Guid tenantId, Guid updatedBy,
        IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.users
            SET is_deleted = TRUE,
                is_active  = FALSE,
                updated_at = NOW(),
                updated_by = @UpdatedBy
            WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = FALSE
            """;

        int affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, TenantId = tenantId, UpdatedBy = updatedBy },
            transaction,
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<bool> SetPasswordAsync(
        Guid id, Guid tenantId, string passwordHash, Guid updatedBy,
        IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE auth.users
            SET password_hash = @PasswordHash,
                updated_at    = NOW(),
                updated_by    = @UpdatedBy
            WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = FALSE
            """;

        int affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, TenantId = tenantId, PasswordHash = passwordHash, UpdatedBy = updatedBy },
            transaction,
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    public Task<int> CountActiveByRoleAsync(
        Guid tenantId, string roleName, Guid? excludingUserId,
        IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(DISTINCT u.id)
            FROM auth.users u
            JOIN auth.user_roles ur ON ur.user_id = u.id AND ur.is_deleted = FALSE
            JOIN auth.roles r ON r.id = ur.role_id AND r.is_deleted = FALSE
            WHERE u.tenant_id = @TenantId
              AND u.is_active = TRUE
              AND u.is_deleted = FALSE
              AND r.name = @RoleName
              AND (@ExcludingUserId IS NULL OR u.id <> @ExcludingUserId)
            """;

        return connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new { TenantId = tenantId, RoleName = roleName, ExcludingUserId = excludingUserId },
            transaction,
            cancellationToken: cancellationToken));
    }
}
