namespace WorkshopAdmin.Infrastructure.Email;

using System.Data;
using Microsoft.Extensions.Options;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Common.Models;
using WorkshopAdmin.Domain.Exceptions;

public sealed class EmailOutbox(
    IEmailTemplateRepository templateRepository,
    IEmailOutboxRepository outboxRepository,
    ITemplateRenderer renderer,
    ICurrentUserContext currentUser,
    IOptions<EmailOptions> emailOptions) : IEmailOutbox
{
    private readonly EmailOptions _options = emailOptions.Value;

    public async Task EnqueueAsync(
        EmailMessage message,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        EmailTemplate template = await templateRepository.GetByCodeAsync(
            message.TemplateCode, connection, transaction, cancellationToken)
            ?? throw new BusinessRuleException($"Email template '{message.TemplateCode}' was not found or is inactive.");

        string locale = message.Locale ?? _options.DefaultLocale;

        string subjectTemplate = Pick(template.Subject, locale, message.TemplateCode, "subject");
        string bodyTextTemplate = Pick(template.BodyText, locale, message.TemplateCode, "body_text");
        string? bodyHtmlTemplate = template.BodyHtml is null
            ? null
            : PickOrNull(template.BodyHtml, locale);

        string subject = renderer.Render(subjectTemplate, message.Placeholders);
        string bodyText = renderer.Render(bodyTextTemplate, message.Placeholders);
        string? bodyHtml = bodyHtmlTemplate is null ? null : renderer.Render(bodyHtmlTemplate, message.Placeholders);

        await outboxRepository.InsertAsync(
            new EmailOutboxInsert(
                message.TenantId,
                message.To,
                message.ToName,
                subject,
                bodyText,
                bodyHtml,
                currentUser.UserId),
            connection, transaction, cancellationToken);
    }

    private string Pick(IReadOnlyDictionary<string, string> values, string locale, string templateCode, string field)
    {
        if (values.TryGetValue(locale, out string? exact) && !string.IsNullOrEmpty(exact))
        {
            return exact;
        }
        if (locale != _options.DefaultLocale &&
            values.TryGetValue(_options.DefaultLocale, out string? fallback) && !string.IsNullOrEmpty(fallback))
        {
            return fallback;
        }
        throw new BusinessRuleException(
            $"Email template '{templateCode}' is missing {field} for locale '{locale}' (and default '{_options.DefaultLocale}').");
    }

    private string? PickOrNull(IReadOnlyDictionary<string, string> values, string locale)
    {
        if (values.TryGetValue(locale, out string? exact) && !string.IsNullOrEmpty(exact))
        {
            return exact;
        }
        if (locale != _options.DefaultLocale &&
            values.TryGetValue(_options.DefaultLocale, out string? fallback) && !string.IsNullOrEmpty(fallback))
        {
            return fallback;
        }
        return null;
    }
}
