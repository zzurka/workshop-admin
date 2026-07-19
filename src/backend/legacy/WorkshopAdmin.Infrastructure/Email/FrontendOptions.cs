namespace WorkshopAdmin.Infrastructure.Email;

public sealed class FrontendOptions
{
    /// <summary>Base URL of the Angular app, e.g. https://app.workshopadmin.example. Used to build links embedded in emails.</summary>
    public string BaseUrl { get; init; } = string.Empty;
}
