namespace WorkshopAdmin.Infrastructure.Email;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

public sealed class MailKitSmtpClient(IOptions<EmailOptions> emailOptions) : ISmtpClient
{
    private readonly EmailOptions _options = emailOptions.Value;

    public async Task SendAsync(
        string toAddress,
        string? toName,
        string subject,
        string bodyText,
        string? bodyHtml,
        CancellationToken cancellationToken)
    {
        MimeMessage message = new();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(toName ?? string.Empty, toAddress));
        message.Subject = subject;

        BodyBuilder builder = new() { TextBody = bodyText };
        if (!string.IsNullOrEmpty(bodyHtml))
        {
            builder.HtmlBody = bodyHtml;
        }
        message.Body = builder.ToMessageBody();

        using SmtpClient client = new();
        SecureSocketOptions socketOptions = _options.Smtp.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await client.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrEmpty(_options.Smtp.Username))
        {
            await client.AuthenticateAsync(_options.Smtp.Username, _options.Smtp.Password ?? string.Empty, cancellationToken);
        }

        try
        {
            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
