namespace WorkshopAdmin.Application.Common.Persistence;

using System.Data;
using WorkshopAdmin.Application.Common.Codebooks;

public interface ICodebookRepository
{
    /// <summary>
    /// Resolves an active codebook entry's id by its stable code, or null if
    /// no active row matches.
    /// </summary>
    Task<short?> ResolveIdByCodeAsync(
        CodebookTable table,
        string code,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);
}
