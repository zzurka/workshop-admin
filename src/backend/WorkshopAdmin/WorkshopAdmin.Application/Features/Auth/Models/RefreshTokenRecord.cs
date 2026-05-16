namespace WorkshopAdmin.Application.Features.Auth.Models;

/// <summary>
/// A persisted refresh token. Only the hash is stored; the raw token is never
/// retained server-side.
/// </summary>
public sealed class RefreshTokenRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
}
