namespace WorkshopAdmin.Infrastructure.Persistence.Repositories.Notification;

using Dapper;
using System.Data;
using WorkshopAdmin.Application.Common.Models;
using WorkshopAdmin.Application.Common.Persistence;

public sealed class EmailOutboxRepository : IEmailOutboxRepository
{
    public Task<Guid> InsertAsync(
        EmailOutboxInsert row,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO notification.email_outbox
                (tenant_id, to_address, to_name, subject, body_text, body_html, created_by)
            VALUES
                (@TenantId, @ToAddress, @ToName, @Subject, @BodyText, @BodyHtml, @CreatedBy)
            RETURNING id
            """;

        return connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql,
            new
            {
                row.TenantId,
                row.ToAddress,
                row.ToName,
                row.Subject,
                row.BodyText,
                row.BodyHtml,
                row.CreatedBy
            },
            transaction,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<EmailOutboxRecord>> ClaimBatchAsync(
        int batchSize,
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE notification.email_outbox
            SET status = 'sending',
                attempts = attempts + 1
            WHERE id IN (
                SELECT id
                FROM notification.email_outbox
                WHERE status = 'pending'
                  AND next_attempt_at <= NOW()
                ORDER BY next_attempt_at
                LIMIT @BatchSize
                FOR UPDATE SKIP LOCKED
            )
            RETURNING id,
                      to_address AS ToAddress,
                      to_name    AS ToName,
                      subject,
                      body_text  AS BodyText,
                      body_html  AS BodyHtml,
                      attempts,
                      max_attempts AS MaxAttempts
            """;

        IEnumerable<EmailOutboxRecord> rows = await connection.QueryAsync<EmailOutboxRecord>(new CommandDefinition(
            sql,
            new { BatchSize = batchSize },
            cancellationToken: cancellationToken));

        return [.. rows];
    }

    public Task MarkSentAsync(Guid id, IDbConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE notification.email_outbox
            SET status = 'sent',
                sent_at = NOW(),
                last_error = NULL
            WHERE id = @Id
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    public Task MarkFailureAsync(
        Guid id,
        string error,
        DateTime? nextAttemptAt,
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        // nextAttemptAt == null => permanent failure (status='failed').
        // Otherwise => requeue as 'pending' with the scheduled retry time.
        const string sql = """
            UPDATE notification.email_outbox
            SET status = CASE WHEN @NextAttemptAt IS NULL THEN 'failed' ELSE 'pending' END,
                last_error = @Error,
                next_attempt_at = COALESCE(@NextAttemptAt, next_attempt_at)
            WHERE id = @Id
            """;

        return connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, Error = error, NextAttemptAt = nextAttemptAt },
            cancellationToken: cancellationToken));
    }
}
