namespace WorkshopAdmin.Application.Common.Persistence;

using System.Data;

public interface ISubscriptionPlanRepository
{
    /// <summary>
    /// Resolves an active, non-deleted subscription plan's id by its stable
    /// code, or null if none matches.
    /// </summary>
    Task<Guid?> ResolveActiveIdByCodeAsync(
        string code,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);
}
