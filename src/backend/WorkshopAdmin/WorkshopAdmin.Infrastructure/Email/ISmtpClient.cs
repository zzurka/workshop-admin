namespace WorkshopAdmin.Infrastructure.Email;

/// <summary>
/// Sends a single email over SMTP. Infrastructure-internal abstraction so the
/// dispatcher can be unit-tested without a real SMTP server.
/// </summary>
public interface ISmtpClient
{
    Task SendAsync(
        string toAddress,
        string? toName,
        string subject,
        string bodyText,
        string? bodyHtml,
        CancellationToken cancellationToken);
}
