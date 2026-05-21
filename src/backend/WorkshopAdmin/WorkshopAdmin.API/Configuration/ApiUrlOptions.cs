namespace WorkshopAdmin.API.Configuration;

/// <summary>
/// Public-facing base URL of this API. Used to build provider redirect URIs
/// (which must match the value registered with the OIDC provider exactly).
/// </summary>
public sealed class ApiUrlOptions
{
    public string BaseUrl { get; init; } = string.Empty;
}
