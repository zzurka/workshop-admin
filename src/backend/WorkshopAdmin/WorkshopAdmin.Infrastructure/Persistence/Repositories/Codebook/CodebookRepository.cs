namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Codebook;

using Dapper;
using System.Data;
using WorkshopAdmin.Application.Common.Codebooks;
using WorkshopAdmin.Application.Common.Persistence;

/// <summary>
/// Generic read access to the uniform <c>codebook</c> schema. The
/// <see cref="CodebookTable"/> → table-name map below is the SQL whitelist:
/// only these tables can ever be queried, so the (non-parameterizable) table
/// name is always a known constant.
/// </summary>
public sealed class CodebookRepository : ICodebookRepository
{
    private static readonly IReadOnlyDictionary<CodebookTable, string> Tables =
        new Dictionary<CodebookTable, string>
        {
            [CodebookTable.Currencies] = "codebook.currencies"
        };

    public Task<short?> ResolveIdByCodeAsync(
        CodebookTable table,
        string code,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (!Tables.TryGetValue(table, out string? tableName))
        {
            throw new InvalidOperationException($"Codebook table '{table}' is not whitelisted.");
        }

        string sql = $"""
            SELECT id FROM {tableName}
            WHERE code = @Code AND is_active = TRUE
            """;

        return connection.ExecuteScalarAsync<short?>(
            new CommandDefinition(sql, new { Code = code }, transaction, cancellationToken: cancellationToken));
    }
}
