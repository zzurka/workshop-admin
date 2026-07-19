using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkshopAdmin.Modules.Codebook.Persistence;
using WorkshopAdmin.SharedKernel.Results;

namespace WorkshopAdmin.Modules.Codebook.Features;

/// <summary>GET /api/codebook — the available codebook types, and
/// GET /api/codebook/{type} — one codebook's entries (cached).</summary>
internal static class ListCodebook
{
    public static void Map(RouteGroupBuilder group)
    {
        // TODO(F3): permission "codebook:read"
        group.MapGet("/", (CodebookRegistry registry) => TypedResults.Ok(registry.Slugs))
            .WithSummary("List codebook types")
            .WithDescription("Returns the slugs of all available codebook types, for the admin UI type picker.");

        // TODO(F3): permission "codebook:read"
        group.MapGet("/{type}", async (
                string type,
                ListCodebookHandler handler,
                CancellationToken cancellationToken,
                bool includeInactive = false) =>
            (await handler.HandleAsync(type, includeInactive, cancellationToken)).ToHttpResult())
            .WithSummary("List entries")
            .WithDescription("Returns one codebook's entries (cached), sorted by sort order then code. Inactive entries are hidden unless includeInactive=true.");
    }
}

internal sealed class ListCodebookHandler(CodebookRegistry registry, CodebookCache cache, CodebookDbContext db)
{
    public async Task<Result<IReadOnlyList<CodebookEntryItem>>> HandleAsync(
        string type, bool includeInactive, CancellationToken cancellationToken)
    {
        if (!registry.TryGet(type, out CodebookType codebookType))
        {
            return Error.NotFound("codebook.type_not_found", $"Unknown codebook type '{type}'.");
        }

        IReadOnlyList<CodebookEntryItem> entries = await cache.GetOrLoadAsync(codebookType, db, cancellationToken);
        return Result.Success(includeInactive
            ? entries
            : (IReadOnlyList<CodebookEntryItem>)entries.Where(e => e.IsActive).ToList());
    }
}
