namespace WorkshopAdmin.Application.Common.Interfaces;

/// <summary>
/// Builds fully-qualified frontend URLs (e.g. for links embedded in emails).
/// Infrastructure-backed by configuration (Frontend:BaseUrl).
/// </summary>
public interface IFrontendUrlProvider
{
    string LoginUrl { get; }

    /// <summary>The SPA page that finishes an external login by POSTing the handoff code back to the API.</summary>
    string ExternalCompleteUrl(string handoffCode);
}
