namespace WorkshopAdmin.Infrastructure.Auth.External;

public sealed class ExternalAuthOptions
{
    public List<ExternalProviderOptions> ExternalProviders { get; init; } = [];
}

public sealed class ExternalProviderOptions
{
    /// <summary>Stable code used in URLs and DB rows (e.g. "google", "microsoft", "oidc:okta").</summary>
    public string Code { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool Enabled { get; init; } = true;

    /// <summary>OIDC authority (issuer). Discovery document at {Authority}/.well-known/openid-configuration.</summary>
    public string Authority { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string? ClientSecret { get; init; }

    public List<string> Scopes { get; init; } = ["openid", "email", "profile"];
}
