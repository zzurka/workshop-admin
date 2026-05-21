namespace WorkshopAdmin.Application.Common.Interfaces;

using System.Data;
using WorkshopAdmin.Application.Common.Models;

/// <summary>
/// Transactional email enqueue. The write participates in the caller's
/// transaction, so the email is dispatched only if the surrounding business
/// operation commits. A background dispatcher delivers queued rows.
/// </summary>
public interface IEmailOutbox
{
    Task EnqueueAsync(
        EmailMessage message,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken cancellationToken);
}
