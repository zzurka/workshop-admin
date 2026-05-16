namespace WorkshopAdmin.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;

    /// <summary>HMAC-SHA256 signing key. Must be at least 32 bytes. Supplied via user-secrets / environment, never committed.</summary>
    public string SigningKey { get; set; } = null!;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}
