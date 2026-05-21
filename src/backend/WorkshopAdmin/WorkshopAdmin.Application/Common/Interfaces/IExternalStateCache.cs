namespace WorkshopAdmin.Application.Common.Interfaces;

/// <summary>
/// Short-lived (~5 min) store for the per-request OAuth state opaque value.
/// Persists the PKCE verifier and the provider so the callback can validate
/// continuity. Single-use: <see cref="TakeAsync"/> removes the entry.
/// </summary>
public interface IExternalStateCache
{
    Task SetAsync(string state, ExternalStateEntry entry, CancellationToken cancellationToken);

    Task<ExternalStateEntry?> TakeAsync(string state, CancellationToken cancellationToken);
}

public sealed record ExternalStateEntry(string Provider, string CodeVerifier, string RedirectUri);
