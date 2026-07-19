using Microsoft.Extensions.Caching.Memory;
using WorkshopAdmin.Modules.Codebook.Persistence;

namespace WorkshopAdmin.Modules.Codebook;

/// <summary>
/// Per-type cache of full codebook lists (active and inactive), no expiry — codebooks
/// change rarely and every write slice invalidates its type. Cached items are immutable
/// snapshots, safe to share across requests.
/// </summary>
internal sealed class CodebookCache(IMemoryCache cache)
{
    public async Task<IReadOnlyList<CodebookEntryItem>> GetOrLoadAsync(
        CodebookType type, CodebookDbContext db, CancellationToken cancellationToken)
    {
        IReadOnlyList<CodebookEntryItem>? entries = await cache.GetOrCreateAsync<IReadOnlyList<CodebookEntryItem>>(
            Key(type.Slug),
            async _ => await type.LoadAllAsync(db, cancellationToken));
        return entries!;
    }

    /// <summary>
    /// Called by write slices right after SaveChanges. The request transaction commits
    /// moments later in DbSessionMiddleware; the tiny window in which a concurrent read
    /// could re-cache pre-commit data is an accepted trade-off for rarely-written lookups.
    /// </summary>
    public void Invalidate(string slug) => cache.Remove(Key(slug));

    private static string Key(string slug) => $"codebook:{slug}";
}
