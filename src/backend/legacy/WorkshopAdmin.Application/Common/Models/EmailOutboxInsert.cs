namespace WorkshopAdmin.Application.Common.Models;

/// <summary>Rendered email ready to insert into <c>notification.email_outbox</c>.</summary>
public sealed record EmailOutboxInsert(
    Guid? TenantId,
    string ToAddress,
    string? ToName,
    string Subject,
    string BodyText,
    string? BodyHtml,
    Guid? CreatedBy);
