namespace WorkshopAdmin.Application.Common.Interfaces;

public interface IExternalAuthRegistry
{
    /// <summary>Returns the configured client for <paramref name="providerCode"/>, or null if the provider is not enabled.</summary>
    IExternalAuthClient? Get(string providerCode);

    /// <summary>Enumerates the currently enabled providers (for a UI picker).</summary>
    IReadOnlyList<IExternalAuthClient> All { get; }
}
