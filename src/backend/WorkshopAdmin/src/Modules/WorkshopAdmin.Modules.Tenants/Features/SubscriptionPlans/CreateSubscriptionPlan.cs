using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Codebook.Contracts;
using WorkshopAdmin.Modules.Tenants.Persistence;
using WorkshopAdmin.SharedKernel.Results;
using WorkshopAdmin.SharedKernel.Validation;

namespace WorkshopAdmin.Modules.Tenants.Features.SubscriptionPlans;

/// <summary>POST /api/subscription-plans — platform-admin CRUD. Currency and billing
/// period are validated through ICodebookLookup (cross-module contract).</summary>
internal static class CreateSubscriptionPlan
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "subscription_plans:create" (platform scope)
        group.MapPost("/", async (
                CreateSubscriptionPlanRequest request,
                CreateSubscriptionPlanHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(request, cancellationToken))
                .ToCreatedResult(plan => $"/api/subscription-plans/{plan.Id}"))
            .WithValidation<CreateSubscriptionPlanRequest>();
}

internal sealed record CreateSubscriptionPlanRequest(
    string Code,
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

internal sealed class CreateSubscriptionPlanRequestValidator : AbstractValidator<CreateSubscriptionPlanRequest>
{
    public CreateSubscriptionPlanRequestValidator()
    {
        RuleFor(r => r.Code).NotEmpty().MaximumLength(50).Matches("^[a-z0-9_]+$")
            .WithMessage("Code must contain only lowercase letters, digits and underscores.");
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

internal static class PlanRules
{
    public static void ApplyLabelRules<T>(this IRuleBuilderInitial<T, Dictionary<string, string>> rule) =>
        rule.NotNull().Must(label => label.TryGetValue("en", out string? en) && !string.IsNullOrWhiteSpace(en))
            .WithMessage("Label must contain a non-empty 'en' entry.");

    public static void ApplyFeaturesRules<T>(this IRuleBuilderInitial<T, JsonElement?> rule) =>
        rule.Must(features => features is null || features.Value.ValueKind == JsonValueKind.Object)
            .WithMessage("Features must be a JSON object.");

    public static string ToFeaturesJson(this JsonElement? features) =>
        features is null ? "{}" : features.Value.GetRawText();
}

internal sealed class CreateSubscriptionPlanHandler(TenantsDbContext db, ICodebookLookup codebook)
{
    public async Task<Result<SubscriptionPlanResponse>> HandleAsync(
        CreateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        if (await db.SubscriptionPlans.AnyAsync(p => p.Code == request.Code, cancellationToken))
        {
            return Error.Conflict("subscription_plan.duplicate_code", $"Plan code '{request.Code}' already exists.");
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

        SubscriptionPlan plan = new()
        {
            Code = request.Code,
            Label = request.Label,
            Description = request.Description,
            Price = request.Price,
            CurrencyId = request.CurrencyId,
            BillingPeriodId = request.BillingPeriodId,
            TrialDays = request.TrialDays,
            MaxUsers = request.MaxUsers,
            MaxVehicles = request.MaxVehicles,
            MaxWorkOrdersPerMonth = request.MaxWorkOrdersPerMonth,
            MaxStorageMb = request.MaxStorageMb,
            Features = request.Features.ToFeaturesJson(),
            IsPublic = request.IsPublic,
            SortOrder = request.SortOrder
        };

        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync(cancellationToken);

        return SubscriptionPlanResponse.From(plan);
    }
}
