using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Tenants.Persistence;
using WorkshopAdmin.SharedKernel.Results;

namespace WorkshopAdmin.Modules.Tenants.Features.Subscriptions;

/// <summary>GET /api/tenants/{id}/subscriptions — subscription period history, newest first.</summary>
internal static class ListTenantSubscriptions
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "tenants:read" (platform scope)
        group.MapGet("/{id:guid}/subscriptions", async (
                Guid id,
                TenantsDbContext db,
                CancellationToken cancellationToken) =>
            {
                if (!await db.Tenants.AnyAsync(t => t.Id == id, cancellationToken))
                {
                    return ResultHttpExtensions.ToProblem(
                        Error.NotFound("tenant.not_found", $"Tenant {id} does not exist."));
                }

                List<TenantSubscriptionResponse> history = await db.TenantSubscriptions
                    .Where(s => s.TenantId == id)
                    .OrderByDescending(s => s.ValidFrom).ThenByDescending(s => s.CreatedAt)
                    .Select(s => new TenantSubscriptionResponse(
                        s.Id, s.SubscriptionPlanId, s.SubscriptionPlan!.Code, s.SubscriptionPlan!.Label,
                        s.ValidFrom, s.ValidTo, s.TrialUntil, s.Notes))
                    .ToListAsync(cancellationToken);

                return TypedResults.Ok(history);
            });
}

internal sealed record TenantSubscriptionResponse(
    Guid Id,
    Guid SubscriptionPlanId,
    string PlanCode,
    Dictionary<string, string> PlanLabel,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    DateOnly? TrialUntil,
    string? Notes);
