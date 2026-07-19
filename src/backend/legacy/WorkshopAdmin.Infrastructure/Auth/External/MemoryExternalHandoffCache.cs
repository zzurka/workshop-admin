namespace WorkshopAdmin.Infrastructure.Auth.External;

using Microsoft.Extensions.Caching.Memory;
using WorkshopAdmin.Application.Common.Interfaces;
using WorkshopAdmin.Application.Features.Auth.Login;

/// <summary>
/// In-memory store for the rendered LoginResponse keyed by handoff code.
/// 60-second TTL: the SPA exchange happens immediately after the callback
/// redirect. Single-use.
/// </summary>
public sealed class MemoryExternalHandoffCache(IMemoryCache cache) : IExternalHandoffCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public Task SetAsync(string handoffCode, LoginResponse payload, CancellationToken cancellationToken)
    {
        cache.Set(Key(handoffCode), payload, Ttl);
        return Task.CompletedTask;
    }

    public Task<LoginResponse?> TakeAsync(string handoffCode, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(Key(handoffCode), out LoginResponse? payload))
        {
            cache.Remove(Key(handoffCode));
            return Task.FromResult(payload);
        }
        return Task.FromResult<LoginResponse?>(null);
    }

    private static string Key(string handoffCode) => "ext-handoff:" + handoffCode;
}
