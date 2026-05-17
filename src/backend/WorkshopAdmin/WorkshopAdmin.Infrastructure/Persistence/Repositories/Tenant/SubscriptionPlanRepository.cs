namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Tenant;

using System.Data;
using Dapper;
using WorkshopAdmin.Application.Common.Interfaces;

public sealed class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    public Task<Guid?> ResolveActiveIdByCodeAsync(
        string code,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id FROM tenant.subscription_plans
            WHERE code = @Code AND is_active = TRUE AND is_deleted = FALSE
            """;

        return connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition(sql, new { Code = code }, transaction, cancellationToken: cancellationToken));
    }
}
