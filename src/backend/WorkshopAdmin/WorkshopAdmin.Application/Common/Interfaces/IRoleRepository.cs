namespace WorkshopAdmin.Application.Common.Interfaces;

using System.Data;

public interface IRoleRepository
{
    /// <summary>
    /// Resolves a global role (tenant_id IS NULL, not deleted) id by its stable
    /// name, or null if none matches.
    /// </summary>
    Task<Guid?> GetGlobalIdByNameAsync(
        string roleName,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);
}
