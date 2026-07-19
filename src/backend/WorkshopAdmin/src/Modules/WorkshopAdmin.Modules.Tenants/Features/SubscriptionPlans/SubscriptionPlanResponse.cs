using System.Text.Json;
using WorkshopAdmin.Modules.Tenants.Persistence;

namespace WorkshopAdmin.Modules.Tenants.Features.SubscriptionPlans;

internal sealed record SubscriptionPlanResponse(
    Guid Id,
    string Code,
    Dictionary<string, string> Label,
    Dictionary<string, string>? Description,
    decimal Price,
    short CurrencyId,
    short BillingPeriodId,
    short TrialDays,
    int? MaxUsers,
    int? MaxVehicles,
    int? MaxWorkOrdersPerMonth,
    int? MaxStorageMb,
    JsonElement Features,
    bool IsPublic,
    short SortOrder,
    bool IsActive)
{
    public static SubscriptionPlanResponse From(SubscriptionPlan plan)
    {
        using JsonDocument features = JsonDocument.Parse(plan.Features);
        return new SubscriptionPlanResponse(
            plan.Id, plan.Code, plan.Label, plan.Description, plan.Price,
            plan.CurrencyId, plan.BillingPeriodId, plan.TrialDays,
            plan.MaxUsers, plan.MaxVehicles, plan.MaxWorkOrdersPerMonth, plan.MaxStorageMb,
            features.RootElement.Clone(), plan.IsPublic, plan.SortOrder, plan.IsActive);
    }
}
