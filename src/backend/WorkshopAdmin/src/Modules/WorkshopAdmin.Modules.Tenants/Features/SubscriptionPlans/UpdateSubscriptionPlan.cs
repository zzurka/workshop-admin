using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkshopAdmin.Modules.Codebook.Contracts;
using WorkshopAdmin.Modules.Tenants.Persistence;
using WorkshopAdmin.SharedKernel.Results;
using WorkshopAdmin.SharedKernel.Validation;

namespace WorkshopAdmin.Modules.Tenants.Features.SubscriptionPlans;

/// <summary>PUT /api/subscription-plans/{id} — the code is immutable.</summary>
internal static class UpdateSubscriptionPlan
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "subscription_plans:update" (platform scope)
        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateSubscriptionPlanRequest request,
                UpdateSubscriptionPlanHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
            .WithValidation<UpdateSubscriptionPlanRequest>()
            .WithSummary("Update subscription plan")
            .WithDescription("Updates a plan's pricing, limits and features. The code is immutable. Changes affect what the plan offers going forward; tenant subscription history is untouched.");
}

internal sealed record UpdateSubscriptionPlanRequest(
    Dictionary<string, string> Label,
    Dictionary<string, string>? Description,
    decimal Price,
    short CurrencyId,
    short BillingPeriodId,
    short TrialDays = 0,
    int? MaxUsers = null,
    int? MaxVehicles = null,
    int? MaxWorkOrdersPerMonth = null,
    int? MaxStorageMb = null,
    JsonElement? Features = null,
    bool IsPublic = true,
    short SortOrder = 0);

internal sealed class UpdateSubscriptionPlanRequestValidator : AbstractValidator<UpdateSubscriptionPlanRequest>
{
    public UpdateSubscriptionPlanRequestValidator()
    {
        RuleFor(r => r.Label).ApplyLabelRules();
        RuleFor(r => r.Price).GreaterThanOrEqualTo(0);
        RuleFor(r => r.TrialDays).GreaterThanOrEqualTo((short)0);
        RuleFor(r => r.MaxUsers).GreaterThan(0).When(r => r.MaxUsers is not null);
        RuleFor(r => r.MaxVehicles).GreaterThan(0).When(r => r.MaxVehicles is not null);
        RuleFor(r => r.MaxWorkOrdersPerMonth).GreaterThan(0).When(r => r.MaxWorkOrdersPerMonth is not null);
        RuleFor(r => r.MaxStorageMb).GreaterThan(0).When(r => r.MaxStorageMb is not null);
        RuleFor(r => r.Features).ApplyFeaturesRules();
    }
}

internal sealed class UpdateSubscriptionPlanHandler(TenantsDbContext db, ICodebookLookup codebook)
{
    public async Task<Result<SubscriptionPlanResponse>> HandleAsync(
        Guid id, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        SubscriptionPlan? plan = await db.SubscriptionPlans.FindAsync([id], cancellationToken);
        if (plan is null || plan.IsDeleted)
        {
            return Error.NotFound("subscription_plan.not_found", $"Subscription plan {id} does not exist.");
        }

        CodebookEntryRef? currency = await codebook.GetByIdAsync(CodebookTypes.Currencies, request.CurrencyId, cancellationToken);
        if (currency is null || !currency.IsActive)
        {
            return Error.Validation("subscription_plan.unknown_currency", $"Currency {request.CurrencyId} does not exist or is inactive.");
        }

        CodebookEntryRef? billingPeriod = await codebook.GetByIdAsync(CodebookTypes.BillingPeriods, request.BillingPeriodId, cancellationToken);
        if (billingPeriod is null || !billingPeriod.IsActive)
        {
            return Error.Validation("subscription_plan.unknown_billing_period", $"Billing period {request.BillingPeriodId} does not exist or is inactive.");
        }

        plan.Label = request.Label;
        plan.Description = request.Description;
        plan.Price = request.Price;
        plan.CurrencyId = request.CurrencyId;
        plan.BillingPeriodId = request.BillingPeriodId;
        plan.TrialDays = request.TrialDays;
        plan.MaxUsers = request.MaxUsers;
        plan.MaxVehicles = request.MaxVehicles;
        plan.MaxWorkOrdersPerMonth = request.MaxWorkOrdersPerMonth;
        plan.MaxStorageMb = request.MaxStorageMb;
        plan.Features = request.Features.ToFeaturesJson();
        plan.IsPublic = request.IsPublic;
        plan.SortOrder = request.SortOrder;

        await db.SaveChangesAsync(cancellationToken);

        return SubscriptionPlanResponse.From(plan);
    }
}
