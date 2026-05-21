namespace WorkshopAdmin.Application.Common.Models;

/// <summary>
/// Stored email template. Subject and bodies are JSONB maps keyed by locale,
/// e.g. <c>{"en":"...","sr":"..."}</c>. The renderer picks the requested locale
/// (falling back to a default) and applies placeholder substitution before the
/// rendered text is persisted on the outbox row.
/// </summary>
public sealed record EmailTemplate(
    Guid Id,
    string Code,
    Dictionary<string, string> Subject,
    Dictionary<string, string> BodyText,
    Dictionary<string, string>? BodyHtml);
