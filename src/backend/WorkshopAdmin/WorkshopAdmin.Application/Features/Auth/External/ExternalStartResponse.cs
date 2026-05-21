namespace WorkshopAdmin.Application.Features.Auth.External;

/// <summary>The URL the SPA should navigate to in order to start the OAuth flow.</summary>
public sealed record ExternalStartResponse(string AuthorizeUrl);
