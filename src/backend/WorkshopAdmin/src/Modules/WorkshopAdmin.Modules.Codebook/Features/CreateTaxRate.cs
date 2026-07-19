using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Codebook.Contracts;
using WorkshopAdmin.Modules.Codebook.Persistence;
using WorkshopAdmin.SharedKernel.Results;
using WorkshopAdmin.SharedKernel.Validation;

namespace WorkshopAdmin.Modules.Codebook.Features;

/// <summary>POST /api/codebook/tax_rates — dedicated slice (O2): tax rates additionally
/// carry the VAT percentage. The literal route wins over the generic /{type} template.</summary>
internal static class CreateTaxRate
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "codebook:manage"
        group.MapPost($"/{CodebookTypes.TaxRates}", async (
                CreateTaxRateRequest request,
                CreateTaxRateHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(request, cancellationToken))
                .ToCreatedResult(rate => $"/api/codebook/{CodebookTypes.TaxRates}/{rate.Id}"))
            .WithValidation<CreateTaxRateRequest>();
}

internal sealed record CreateTaxRateRequest(string Code, Dictionary<string, string> Label, decimal Rate, short SortOrder = 0);

internal sealed record TaxRateResponse(short Id, string Code, Dictionary<string, string> Label, decimal Rate, short SortOrder, bool IsActive);

internal sealed class CreateTaxRateRequestValidator : AbstractValidator<CreateTaxRateRequest>
{
    public CreateTaxRateRequestValidator()
    {
        RuleFor(r => r.Code).ApplyCodebookCodeRules();
        RuleFor(r => r.Label).ApplyCodebookLabelRules();
        RuleFor(r => r.Rate).InclusiveBetween(0, 100);
    }
}

internal sealed class CreateTaxRateHandler(CodebookCache cache, CodebookDbContext db)
{
    public async Task<Result<TaxRateResponse>> HandleAsync(
        CreateTaxRateRequest request, CancellationToken cancellationToken)
    {
        if (await db.Set<TaxRate>().AnyAsync(t => t.Code == request.Code, cancellationToken))
        {
            return Error.Conflict("codebook.duplicate_code",
                $"Code '{request.Code}' already exists in '{CodebookTypes.TaxRates}'.");
        }

        TaxRate rate = new()
        {
            Code = request.Code,
            Label = request.Label,
            Rate = request.Rate,
            SortOrder = request.SortOrder
        };

        db.Add(rate);
        await db.SaveChangesAsync(cancellationToken);
        cache.Invalidate(CodebookTypes.TaxRates);

        return new TaxRateResponse(rate.Id, rate.Code, rate.Label, rate.Rate, rate.SortOrder, rate.IsActive);
    }
}
