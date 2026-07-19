namespace WorkshopAdmin.Application.Common.Models;

/// <summary>
/// A transactional email to enqueue. The outbox resolves <see cref="TemplateCode"/>,
/// renders subject and body for <see cref="Locale"/> with <see cref="Placeholders"/>,
/// and persists the rendered content on the outbox row.
/// </summary>
public sealed record EmailMessage(
    string TemplateCode,
    string To,
    IReadOnlyDictionary<string, string> Placeholders,
    string? ToName = null,
    Guid? TenantId = null,
    string? Locale = null);
