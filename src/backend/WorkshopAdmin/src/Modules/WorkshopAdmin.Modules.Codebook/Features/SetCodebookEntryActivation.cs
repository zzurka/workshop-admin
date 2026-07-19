using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using WorkshopAdmin.Modules.Codebook.Persistence;
using WorkshopAdmin.SharedKernel.Results;

namespace WorkshopAdmin.Modules.Codebook.Features;

/// <summary>POST /api/codebook/{type}/{id}/activation — codebook entries are never
/// deleted (historical rows reference them), only deactivated.</summary>
internal static class SetCodebookEntryActivation
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "codebook:manage"
        group.MapPost("/{type}/{id}/activation", async (
                string type,
                short id,
                SetCodebookEntryActivationRequest request,
                SetCodebookEntryActivationHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(type, id, request, cancellationToken)).ToHttpResult());
}

internal sealed record SetCodebookEntryActivationRequest(bool IsActive);

internal sealed class SetCodebookEntryActivationHandler(
    CodebookRegistry registry, CodebookCache cache, CodebookDbContext db)
{
    public async Task<Result> HandleAsync(
        string type, short id, SetCodebookEntryActivationRequest request, CancellationToken cancellationToken)
    {
        if (!registry.TryGet(type, out CodebookType codebookType))
        {
            return Result.Failure(Error.NotFound("codebook.type_not_found", $"Unknown codebook type '{type}'."));
        }

        CodebookEntry? entry = await codebookType.FindAsync(db, id, cancellationToken);
        if (entry is null)
        {
            return Result.Failure(Error.NotFound("codebook.entry_not_found", $"No entry {id} in '{type}'."));
        }

        entry.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        cache.Invalidate(type);

        return Result.Success();
    }
}
