using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using WorkshopAdmin.Modules.Codebook.Persistence;
using WorkshopAdmin.SharedKernel.Results;
using WorkshopAdmin.SharedKernel.Validation;

namespace WorkshopAdmin.Modules.Codebook.Features;

/// <summary>PUT /api/codebook/{type}/{id} — update label and sort order. The code is
/// immutable (it is the stable machine-readable key).</summary>
internal static class UpdateCodebookEntry
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "codebook:manage"
        group.MapPut("/{type}/{id}", async (
                string type,
                short id,
                UpdateCodebookEntryRequest request,
                UpdateCodebookEntryHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(type, id, request, cancellationToken)).ToHttpResult())
            .WithValidation<UpdateCodebookEntryRequest>();
}

internal sealed record UpdateCodebookEntryRequest(Dictionary<string, string> Label, short SortOrder = 0);

internal sealed class UpdateCodebookEntryRequestValidator : AbstractValidator<UpdateCodebookEntryRequest>
{
    public UpdateCodebookEntryRequestValidator()
    {
        RuleFor(r => r.Label).ApplyCodebookLabelRules();
    }
}

internal sealed class UpdateCodebookEntryHandler(CodebookRegistry registry, CodebookCache cache, CodebookDbContext db)
{
    public async Task<Result<CodebookEntryItem>> HandleAsync(
        string type, short id, UpdateCodebookEntryRequest request, CancellationToken cancellationToken)
    {
        if (!registry.TryGet(type, out CodebookType codebookType))
        {
            return Error.NotFound("codebook.type_not_found", $"Unknown codebook type '{type}'.");
        }

        CodebookEntry? entry = await codebookType.FindAsync(db, id, cancellationToken);
        if (entry is null)
        {
            return Error.NotFound("codebook.entry_not_found", $"No entry {id} in '{type}'.");
        }

        entry.Label = request.Label;
        entry.SortOrder = request.SortOrder;

        await db.SaveChangesAsync(cancellationToken);
        cache.Invalidate(type);

        return new CodebookEntryItem(entry.Id, entry.Code, entry.Label, entry.SortOrder, entry.IsActive);
    }
}
