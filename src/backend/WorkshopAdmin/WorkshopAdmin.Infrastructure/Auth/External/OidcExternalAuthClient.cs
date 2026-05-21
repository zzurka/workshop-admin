namespace WorkshopAdmin.Infrastructure.Auth.External;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Auth.Models;
using WorkshopAdmin.Domain.Exceptions;

// ConfigurationManager<T> from Microsoft.IdentityModel.Protocols caches and
// refreshes the OIDC discovery document + JWKS automatically.

/// <summary>
/// OIDC-compliant external auth client. One instance per configured provider
/// (Google, Microsoft Entra, or any provider that publishes a discovery
/// document at {Authority}/.well-known/openid-configuration). Validates the
/// id_token using JWKS keys retrieved from discovery; only validated, verified
/// identities are returned to the matcher.
/// </summary>
public sealed class OidcExternalAuthClient(
    ExternalProviderOptions options,
    IHttpClientFactory httpClientFactory,
    ConfigurationManager<OpenIdConnectConfiguration> configurationManager) : IExternalAuthClient
{
    public string ProviderCode => options.Code;
    public string DisplayName => options.DisplayName;

    public async Task<string> BuildAuthorizeUrlAsync(
        string state, string codeChallenge, string redirectUri, CancellationToken cancellationToken)
    {
        OpenIdConnectConfiguration config = await configurationManager.GetConfigurationAsync(cancellationToken);

        string query = string.Join("&",
        [
            "response_type=code",
            "client_id=" + Uri.EscapeDataString(options.ClientId),
            "redirect_uri=" + Uri.EscapeDataString(redirectUri),
            "scope=" + Uri.EscapeDataString(string.Join(' ', options.Scopes)),
            "state=" + Uri.EscapeDataString(state),
            "code_challenge=" + Uri.EscapeDataString(codeChallenge),
            "code_challenge_method=S256"
        ]);

        char separator = config.AuthorizationEndpoint.Contains('?') ? '&' : '?';
        return config.AuthorizationEndpoint + separator + query;
    }

    public async Task<ExternalIdentity> ExchangeCodeAsync(
        string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
    {
        OpenIdConnectConfiguration config = await configurationManager.GetConfigurationAsync(cancellationToken);

        using HttpClient client = httpClientFactory.CreateClient(nameof(OidcExternalAuthClient));

        Dictionary<string, string> form = new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = options.ClientId,
            ["code_verifier"] = codeVerifier
        };
        if (!string.IsNullOrEmpty(options.ClientSecret))
        {
            form["client_secret"] = options.ClientSecret;
        }

        using HttpRequestMessage request = new(HttpMethod.Post, config.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new UnauthorizedException($"Token exchange with provider '{options.Code}' failed ({(int)response.StatusCode}): {body}");
        }

        TokenResponse? tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (tokens is null || string.IsNullOrEmpty(tokens.IdToken))
        {
            throw new UnauthorizedException($"Provider '{options.Code}' did not return an id_token.");
        }

        ClaimsPrincipal principal = ValidateIdToken(tokens.IdToken, config);

        string? subject = principal.FindFirst("sub")?.Value;
        string? email = principal.FindFirst("email")?.Value ?? principal.FindFirst(ClaimTypes.Email)?.Value;
        bool emailVerified = string.Equals(principal.FindFirst("email_verified")?.Value, "true", StringComparison.OrdinalIgnoreCase);
        string? firstName = principal.FindFirst("given_name")?.Value ?? principal.FindFirst(ClaimTypes.GivenName)?.Value;
        string? lastName = principal.FindFirst("family_name")?.Value ?? principal.FindFirst(ClaimTypes.Surname)?.Value;

        if (string.IsNullOrEmpty(subject))
        {
            throw new UnauthorizedException($"Provider '{options.Code}' id_token has no 'sub' claim.");
        }
        if (string.IsNullOrEmpty(email))
        {
            throw new UnauthorizedException($"Provider '{options.Code}' id_token has no 'email' claim.");
        }

        return new ExternalIdentity(options.Code, subject, email, emailVerified, firstName, lastName);
    }

    private ClaimsPrincipal ValidateIdToken(string idToken, OpenIdConnectConfiguration config)
    {
        JwtSecurityTokenHandler handler = new() { MapInboundClaims = false };

        TokenValidationParameters parameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = config.Issuer,
            ValidateAudience = true,
            ValidAudience = options.ClientId,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = config.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        try
        {
            return handler.ValidateToken(idToken, parameters, out _);
        }
        catch (Exception ex)
        {
            throw new UnauthorizedException($"Provider '{options.Code}' id_token failed validation: {ex.Message}");
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("id_token")] string? IdToken,
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("token_type")] string? TokenType,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn);
}
