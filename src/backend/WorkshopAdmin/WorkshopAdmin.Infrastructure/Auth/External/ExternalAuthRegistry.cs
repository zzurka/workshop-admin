namespace WorkshopAdmin.Infrastructure.Auth.External;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using WorkshopAdmin.Application.Common.Interfaces;

/// <summary>
/// Builds and exposes one <see cref="IExternalAuthClient"/> per enabled
/// provider in configuration. Discovery <see cref="ConfigurationManager{T}"/>
/// instances are created once and reused so JWKS and the discovery document
/// are cached (and auto-refreshed).
/// </summary>
public sealed class ExternalAuthRegistry : IExternalAuthRegistry
{
    private readonly Dictionary<string, IExternalAuthClient> _clients;

    public ExternalAuthRegistry(IOptions<ExternalAuthOptions> options, IHttpClientFactory httpClientFactory)
    {
        _clients = new Dictionary<string, IExternalAuthClient>(StringComparer.OrdinalIgnoreCase);

        foreach (ExternalProviderOptions provider in options.Value.ExternalProviders)
        {
            if (!provider.Enabled || string.IsNullOrEmpty(provider.Code) || string.IsNullOrEmpty(provider.Authority) || string.IsNullOrEmpty(provider.ClientId))
            {
                continue;
            }

            ConfigurationManager<OpenIdConnectConfiguration> configurationManager = new(
                provider.Authority.TrimEnd('/') + "/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(httpClientFactory.CreateClient(nameof(ExternalAuthRegistry))) { RequireHttps = true });

            _clients[provider.Code] = new OidcExternalAuthClient(provider, httpClientFactory, configurationManager);
        }

        All = [.. _clients.Values];
    }

    public IExternalAuthClient? Get(string providerCode)
        => _clients.TryGetValue(providerCode, out IExternalAuthClient? client) ? client : null;

    public IReadOnlyList<IExternalAuthClient> All { get; }
}
