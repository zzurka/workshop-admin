namespace WorkshopAdmin.Application.Common.Interfaces;

using WorkshopAdmin.Application.Features.Auth.Models;

/// <summary>
/// Per-provider OIDC client. The Application owns the abstraction; the
/// concrete OIDC discovery + token exchange + id_token validation lives in
/// Infrastructure.
/// </summary>
public interface IExternalAuthClient
{
    /// <summary>Stable provider code (e.g. ''google'', ''microsoft'', ''oidc:okta'').</summary>
    string ProviderCode { get; }

    /// <summary>Human-readable name for the UI's provider picker.</summary>
    string DisplayName { get; }

    Task<string> BuildAuthorizeUrlAsync(
        string state,
        string codeChallenge,
        string redirectUri,
        CancellationToken cancellationToken);

    Task<ExternalIdentity> ExchangeCodeAsync(
        string code,
        string codeVerifier,
        string redirectUri,
        CancellationToken cancellationToken);
}
