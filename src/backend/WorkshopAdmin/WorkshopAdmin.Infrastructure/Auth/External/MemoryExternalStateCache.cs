namespace WorkshopAdmin.Infrastructure.Auth.External;

using Microsoft.Extensions.Caching.Memory;
using WorkshopAdmin.Application.Common.Interfaces;

/// <summary>
/// In-memory store for the per-flow OAuth state (PKCE verifier + provider +
/// redirect_uri). 5-minute TTL is enough for a user to be redirected, sign in,
/// and come back; longer doesn't make the flow safer. Single-use:
/// <see cref="TakeAsync"/> removes the entry to prevent replay.
/// </summary>
public sealed class MemoryExternalStateCache(IMemoryCache cache) : IExternalStateCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public Task SetAsync(string state, ExternalStateEntry entry, CancellationToken cancellationToken)
    {
        cache.Set(Key(state), entry, Ttl);
        return Task.CompletedTask;
    }

    public Task<ExternalStateEntry?> TakeAsync(string state, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(Key(state), out ExternalStateEntry? entry))
        {
            cache.Remove(Key(state));
            return Task.FromResult(entry);
        }
        return Task.FromResult<ExternalStateEntry?>(null);
    }

    private static string Key(string state) => "ext-state:" + state;
}
