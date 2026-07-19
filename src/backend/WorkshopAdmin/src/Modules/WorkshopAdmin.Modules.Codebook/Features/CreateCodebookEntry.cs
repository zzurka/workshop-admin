using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkshopAdmin.Modules.Codebook.Persistence;
using WorkshopAdmin.SharedKernel.Results;
using WorkshopAdmin.SharedKernel.Validation;

namespace WorkshopAdmin.Modules.Codebook.Features;

/// <summary>POST /api/codebook/{type} — add an entry to a codebook. The two typed
/// codebooks (tax_rates, service_types) have dedicated slices on literal routes,
/// which ASP.NET routing prefers over this template.</summary>
internal static class CreateCodebookEntry
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "codebook:manage"
        group.MapPost("/{type}", async (
                string type,
                CreateCodebookEntryRequest request,
                CreateCodebookEntryHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(type, request, cancellationToken))
                .ToCreatedResult(entry => $"/api/codebook/{type}/{entry.Id}"))
            .WithValidation<CreateCodebookEntryRequest>()
            .WithSummary("Create entry")
            .WithDescription("Adds an entry to a codebook. The code is the stable machine-readable key (lowercase, digits, underscores) and cannot be changed later. Label must contain at least an 'en' translation.");
}

internal sealed record CreateCodebookEntryRequest(string Code, Dictionary<string, string> Label, short SortOrder = 0);

internal sealed class CreateCodebookEntryRequestValidator : AbstractValidator<CreateCodebookEntryRequest>
{
    public CreateCodebookEntryRequestValidator()
    {
        RuleFor(r => r.Code).ApplyCodebookCodeRules();
        RuleFor(r => r.Label).ApplyCodebookLabelRules();
    }
}

internal static class CodebookRules
{
    public static void ApplyCodebookCodeRules<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.NotEmpty().MaximumLength(50).Matches("^[a-z0-9_]+$")
            .WithMessage("Code must contain only lowercase letters, digits and underscores.");

    public static void ApplyCodebookLabelRules<T>(this IRuleBuilderInitial<T, Dictionary<string, string>> rule) =>
        rule.NotNull().Must(label => label.TryGetValue("en", out string? en) && !string.IsNullOrWhiteSpace(en))
            .WithMessage("Label must contain a non-empty 'en' entry.");
}

internal sealed class CreateCodebookEntryHandler(CodebookRegistry registry, CodebookCache cache, CodebookDbContext db)
{
    public async Task<Result<CodebookEntryItem>> HandleAsync(
        string type, CreateCodebookEntryRequest request, CancellationToken cancellationToken)
    {
        if (!registry.TryGet(type, out CodebookType codebookType))
        {
            return Error.NotFound("codebook.type_not_found", $"Unknown codebook type '{type}'.");
        }

        if (await codebookType.CodeExistsAsync(db, request.Code, cancellationToken))
        {
            return Error.Conflict("codebook.duplicate_code", $"Code '{request.Code}' already exists in '{type}'.");
        }

        CodebookEntry entry = codebookType.CreateInstance();
        entry.Code = request.Code;
        entry.Label = request.Label;
        entry.SortOrder = request.SortOrder;

        db.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        cache.Invalidate(type);

        return new CodebookEntryItem(entry.Id, entry.Code, entry.Label, entry.SortOrder, entry.IsActive);
    }
}
