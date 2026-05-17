namespace WorkshopAdmin.Application.Common.Models;

/// <summary>A transactional email. <see cref="TextBody"/> is an optional plain-text fallback.</summary>
public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? TextBody = null);
