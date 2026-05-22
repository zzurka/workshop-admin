namespace WorkshopAdmin.Application.Common.Persistence;

using System.Data;
using WorkshopAdmin.Application.Features.Auth.Models;
using WorkshopAdmin.Application.Features.User.List;
using WorkshopAdmin.Application.Features.User.Models;

public interface IUserRepository
{
    Task<AuthUser?> FindByEmailAsync(string email, IDbConnection connection, CancellationToken cancellationToken);

    Task<AuthUser?> FindByIdAsync(Guid userId, IDbConnection connection, CancellationToken cancellationToken);

    /// <summary>Distinct names of the (non-deleted) roles assigned to the user.</summary>
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, IDbConnection connection, CancellationToken cancellationToken);

    /// <summary>Distinct permission names granted to the user through their roles.</summary>
    Task<IReadOnlyList<string>> GetPermissionNamesAsync(Guid userId, IDbConnection connection, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);

    /// <summary>Inserts a user and returns the generated id.</summary>
    Task<Guid> CreateAsync(NewUser user, Guid createdBy, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);

    /// <summary>Assigns a role to a user (idempotent; re-activates a soft-deleted assignment).</summary>
    Task AssignRoleAsync(Guid userId, Guid roleId, Guid createdBy, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);

    /// <summary>
    /// Sets password_hash for an arbitrary (non-deleted) user, independent of tenant scope.
    /// Used by self-service password reset where the actor IS the user.
    /// </summary>
    Task<bool> UpdatePasswordHashAsync(Guid userId, string passwordHash, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);

    /// <summary>Soft-deletes a user's role assignment (idempotent; no-op if not currently assigned).</summary>
    Task RemoveRoleAsync(Guid userId, Guid roleId, Guid updatedBy, IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);

    // --- Tenant-scoped user administration (all bounded to the caller's tenant) ---

    Task<IReadOnlyList<UserListItem>> ListByTenantAsync(
        Guid tenantId, string? search, bool? isActive, int offset, int limit, string sortBy, string sortDirection,
        IDbConnection connection, CancellationToken cancellationToken);

    Task<int> CountByTenantAsync(Guid tenantId, string? search, bool? isActive, IDbConnection connection, CancellationToken cancellationToken);

    Task<UserRecord?> GetByIdInTenantAsync(Guid id, Guid tenantId, IDbConnection connection, CancellationToken cancellationToken);

    Task<bool> ExistsInTenantAsync(Guid id, Guid tenantId, IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken);

    Task<bool> UpdateProfileAsync(Guid id, Guid tenantId, string firstName, string lastName, string? phoneNumber, Guid updatedBy, IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken);

    Task<bool> SetActiveAsync(Guid id, Guid tenantId, bool isActive, Guid updatedBy, IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken);

    Task<bool> SoftDeleteAsync(Guid id, Guid tenantId, Guid updatedBy, IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken);

    Task<bool> SetPasswordAsync(Guid id, Guid tenantId, string passwordHash, Guid updatedBy, IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken);

    /// <summary>Count of active, non-deleted users in the tenant holding the named role, optionally excluding one user.</summary>
    Task<int> CountActiveByRoleAsync(Guid tenantId, string roleName, Guid? excludingUserId, IDbConnection connection, IDbTransaction? transaction, CancellationToken cancellationToken);
}
