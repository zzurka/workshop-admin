namespace WorkshopAdmin.Application.Common.Interfaces;

using WorkshopAdmin.Application.Common.Models;

/// <summary>
/// Sends transactional email. The current implementation logs instead of
/// delivering; a real SMTP / provider implementation is the planned next step
/// (see TODO.txt). Callers should treat sending as best-effort and not let an
/// email failure abort the surrounding operation.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
