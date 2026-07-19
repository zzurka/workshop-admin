using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using WorkshopAdmin.Modules.Tenants.Persistence;
using WorkshopAdmin.SharedKernel.Results;

namespace WorkshopAdmin.Modules.Tenants.Features.SubscriptionPlans;

/// <summary>POST /api/subscription-plans/{id}/activation — retire / reactivate a plan.
/// Existing tenants keep a retired plan; new tenants cannot select it.</summary>
internal static class SetSubscriptionPlanActivation
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "subscription_plans:update" (platform scope)
        group.MapPost("/{id:guid}/activation", async (
                Guid id,
                SetSubscriptionPlanActivationRequest request,
                SetSubscriptionPlanActivationHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult());
}

internal sealed record SetSubscriptionPlanActivationRequest(bool IsActive);

internal sealed class SetSubscriptionPlanActivationHandler(TenantsDbContext db)
{
    public async Task<Result> HandleAsync(
        Guid id, SetSubscriptionPlanActivationRequest request, CancellationToken cancellationToken)
    {
        SubscriptionPlan? plan = await db.SubscriptionPlans.FindAsync([id], cancellationToken);
        if (plan is null || plan.IsDeleted)
        {
            return Result.Failure(Error.NotFound("subscription_plan.not_found", $"Subscription plan {id} does not exist."));
        }

        plan.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
