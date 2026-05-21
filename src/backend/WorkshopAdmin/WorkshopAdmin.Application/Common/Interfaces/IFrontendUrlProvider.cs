namespace WorkshopAdmin.Application.Common.Interfaces;

/// <summary>
/// Builds fully-qualified frontend URLs (e.g. for links embedded in emails).
/// Infrastructure-backed by configuration (Frontend:BaseUrl).
/// </summary>
public interface IFrontendUrlProvider
{
    string LoginUrl { get; }
}
