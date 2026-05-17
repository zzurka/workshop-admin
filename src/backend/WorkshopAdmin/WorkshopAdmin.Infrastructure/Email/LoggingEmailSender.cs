namespace WorkshopAdmin.Infrastructure.Email;

using Microsoft.Extensions.Logging;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Common.Models;

/// <summary>
/// Placeholder email sender: logs the message instead of delivering it. Lets
/// the rest of the system depend on <see cref="IEmailSender"/> until a real
/// SMTP / provider implementation is wired up (see TODO.txt). Never throws.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[EMAIL not sent — no provider configured] To: {To} | Subject: {Subject}",
            message.To, message.Subject);

        return Task.CompletedTask;
    }
}
