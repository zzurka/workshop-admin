namespace WorkshopAdmin.Application.Features.Auth.Models;

public sealed record ExternalLoginRecord(
    Guid Id,
    Guid UserId,
    string Provider,
    string Subject,
    string? Email);
