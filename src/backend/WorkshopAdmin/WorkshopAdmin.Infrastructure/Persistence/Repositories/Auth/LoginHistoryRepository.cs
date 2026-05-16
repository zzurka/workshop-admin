namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Auth;

using System.Data;
using Dapper;
using WorkshopAdmin.Application.Common.Interfaces;

public sealed class LoginHistoryRepository : ILoginHistoryRepository
{
    public Task RecordAsync(
        Guid userId,
        string loginMethod,
        string? ipAddress,
        string? userAgent,
        bool success,
        string? failureReason,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO auth.login_history
                (user_id, login_method, ip_address, user_agent, success, failure_reason)
            VALUES
                (@UserId, @LoginMethod, CAST(@IpAddress AS inet), @UserAgent, @Success, @FailureReason)
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                UserId = userId,
                LoginMethod = loginMethod,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = success,
                FailureReason = failureReason
            },
            transaction,
            cancellationToken: cancellationToken));
    }
}
