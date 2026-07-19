using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Tenants.Persistence;

namespace WorkshopAdmin.Modules.Tenants.Features.SubscriptionPlans;

/// <summary>GET /api/subscription-plans — publicly readable (onboarding shows the offer);
/// includeInactive is for the platform-admin UI.</summary>
internal static class ListSubscriptionPlans
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", async (
                TenantsDbContext db,
                CancellationToken cancellationToken,
                bool includeInactive = false) =>
            {
                List<SubscriptionPlan> plans = await db.SubscriptionPlans
                    .Where(p => includeInactive || p.IsActive)
                    .OrderBy(p => p.SortOrder).ThenBy(p => p.Code)
                    .ToListAsync(cancellationToken);

                return TypedResults.Ok(plans.Select(SubscriptionPlanResponse.From).ToList());
            })
            .WithSummary("List subscription plans")
            .WithDescription("Returns active plans sorted for the pricing table. Publicly readable — onboarding shows the offer before signup. includeInactive=true adds retired plans (admin UI).");
}
