namespace WorkshopAdmin.Application.Common.Interfaces;

using System.Data;
using WorkshopAdmin.Application.Common.Models;

public interface IEmailOutboxRepository
{
    Task<Guid> InsertAsync(
        EmailOutboxInsert row,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically claim a batch of due rows by moving them from 'pending' to
    /// 'sending' and incrementing <c>attempts</c>. Uses
    /// <c>FOR UPDATE SKIP LOCKED</c> so multiple dispatcher instances do not
    /// fight over the same rows.
    /// </summary>
    Task<IReadOnlyList<EmailOutboxRecord>> ClaimBatchAsync(
        int batchSize,
        IDbConnection connection,
        CancellationToken cancellationToken);

    Task MarkSentAsync(Guid id, IDbConnection connection, CancellationToken cancellationToken);

    /// <summary>
    /// Mark a claimed row as either failed-permanent (<c>status='failed'</c>) or
    /// retry-later (back to <c>status='pending'</c> with <paramref name="nextAttemptAt"/>).
    /// </summary>
    Task MarkFailureAsync(
        Guid id,
        string error,
        DateTime? nextAttemptAt,
        IDbConnection connection,
        CancellationToken cancellationToken);
}
