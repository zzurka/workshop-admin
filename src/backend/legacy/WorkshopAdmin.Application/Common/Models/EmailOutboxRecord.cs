namespace WorkshopAdmin.Application.Common.Models;

/// <summary>
/// Row claimed by the dispatcher from <c>notification.email_outbox</c>.
/// </summary>
public sealed record EmailOutboxRecord(
    Guid Id,
    string ToAddress,
    string? ToName,
    string Subject,
    string BodyText,
    string? BodyHtml,
    int Attempts,
    int MaxAttempts);
