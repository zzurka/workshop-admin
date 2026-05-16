namespace WorkshopAdmin.Application.Features.Auth.Models;

/// <summary>A signed JWT access token and its absolute expiry (UTC).</summary>
public sealed record AccessToken(string Token, DateTime ExpiresAt);

/// <summary>
/// A freshly generated refresh token. <see cref="RawToken"/> is returned to the
/// client exactly once; only <see cref="TokenHash"/> is persisted.
/// </summary>
public sealed record GeneratedRefreshToken(string RawToken, string TokenHash, DateTime ExpiresAt);
