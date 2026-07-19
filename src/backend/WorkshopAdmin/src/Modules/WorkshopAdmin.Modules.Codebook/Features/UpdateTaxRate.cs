using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Codebook.Contracts;
using WorkshopAdmin.Modules.Codebook.Persistence;
using WorkshopAdmin.SharedKernel.Results;
using WorkshopAdmin.SharedKernel.Validation;

namespace WorkshopAdmin.Modules.Codebook.Features;

/// <summary>PUT /api/codebook/tax_rates/{id} — dedicated slice (O2). Changing the rate
/// never affects existing invoices — invoice lines snapshot the value.</summary>
internal static class UpdateTaxRate
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "codebook:manage"
        group.MapPut($"/{CodebookTypes.TaxRates}/{{id}}", async (
                short id,
                UpdateTaxRateRequest request,
                UpdateTaxRateHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
            .WithValidation<UpdateTaxRateRequest>();
}

internal sealed record UpdateTaxRateRequest(Dictionary<string, string> Label, decimal Rate, short SortOrder = 0);

internal sealed class UpdateTaxRateRequestValidator : AbstractValidator<UpdateTaxRateRequest>
{
    public UpdateTaxRateRequestValidator()
    {
        RuleFor(r => r.Label).ApplyCodebookLabelRules();
        RuleFor(r => r.Rate).InclusiveBetween(0, 100);
    }
}

internal sealed class UpdateTaxRateHandler(CodebookCache cache, CodebookDbContext db)
{
    public async Task<Result<TaxRateResponse>> HandleAsync(
        short id, UpdateTaxRateRequest request, CancellationToken cancellationToken)
    {
        TaxRate? rate = await db.Set<TaxRate>().SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (rate is null)
        {
            return Error.NotFound("codebook.entry_not_found", $"No entry {id} in '{CodebookTypes.TaxRates}'.");
        }

        rate.Label = request.Label;
        rate.Rate = request.Rate;
        rate.SortOrder = request.SortOrder;

        await db.SaveChangesAsync(cancellationToken);
        cache.Invalidate(CodebookTypes.TaxRates);

        return new TaxRateResponse(rate.Id, rate.Code, rate.Label, rate.Rate, rate.SortOrder, rate.IsActive);
    }
}
