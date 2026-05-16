namespace WorkshopAdmin.Application.Common.Interfaces;

using System.Data;
using WorkshopAdmin.Application.Features.Auth.Models;

public interface IUserRepository
{
    Task<AuthUser?> FindByEmailAsync(string email, IDbConnection connection, CancellationToken cancellationToken);

    Task<AuthUser?> FindByIdAsync(Guid userId, IDbConnection connection, CancellationToken cancellationToken);

    /// <summary>Distinct names of the (non-deleted) roles assigned to the user.</summary>
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, IDbConnection connection, CancellationToken cancellationToken);

    /// <summary>Distinct permission names granted to the user through their roles.</summary>
    Task<IReadOnlyList<string>> GetPermissionNamesAsync(Guid userId, IDbConnection connection, CancellationToken cancellationToken);
}
