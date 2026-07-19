namespace WorkshopAdmin.Application.Features.Auth.Models;

public sealed record PasswordResetTokenRecord(
    Guid Id,
    Guid UserId,
    string TokenHash,
    DateTime ExpiresAt,
    DateTime? UsedAt);
