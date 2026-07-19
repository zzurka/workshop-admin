namespace WorkshopAdmin.Application.Common.Persistence;

using System.Data;

public interface ILoginHistoryRepository
{
    /// <summary>
    /// Appends an immutable login-attempt record. Only called when a user id is
    /// known (auth.login_history.user_id is NOT NULL — attempts for unknown
    /// emails cannot be, and are not, recorded).
    /// </summary>
    Task RecordAsync(
        Guid userId,
        string loginMethod,
        string? ipAddress,
        string? userAgent,
        bool success,
        string? failureReason,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken cancellationToken);
}
