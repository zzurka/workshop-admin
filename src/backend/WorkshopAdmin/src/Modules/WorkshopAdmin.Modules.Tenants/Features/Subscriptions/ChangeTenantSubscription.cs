using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Tenants.Contracts;
using WorkshopAdmin.Modules.Tenants.Persistence;
using WorkshopAdmin.SharedKernel.Events;
using WorkshopAdmin.SharedKernel.Results;
using WorkshopAdmin.SharedKernel.Validation;

namespace WorkshopAdmin.Modules.Tenants.Features.Subscriptions;

/// <summary>POST /api/tenants/{id}/subscriptions — one transaction: closes the current
/// period, opens the new one and re-points tenants.subscription_plan_id (the table
/// comment's application rule). Raises <see cref="TenantSubscriptionChanged"/>.</summary>
internal static class ChangeTenantSubscription
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "tenants:update" (platform scope)
        group.MapPost("/{id:guid}/subscriptions", async (
                Guid id,
                ChangeTenantSubscriptionRequest request,
                ChangeTenantSubscriptionHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken))
                .ToCreatedResult(_ => $"/api/tenants/{id}/subscriptions"))
            .WithValidation<ChangeTenantSubscriptionRequest>()
            .WithSummary("Change subscription plan")
            .WithDescription("Closes the current period, opens a new one from the given date (default today) and re-points the tenant to the new plan — all in one transaction.");
}

/// <param name="ValidFrom">First day of the new period; defaults to today (UTC).</param>
internal sealed record ChangeTenantSubscriptionRequest(
    Guid SubscriptionPlanId, DateOnly? ValidFrom = null, string? Notes = null);

internal sealed class ChangeTenantSubscriptionRequestValidator : AbstractValidator<ChangeTenantSubscriptionRequest>
{
    public ChangeTenantSubscriptionRequestValidator()
    {
        RuleFor(r => r.SubscriptionPlanId).NotEmpty();
    }
}

internal sealed class ChangeTenantSubscriptionHandler(TenantsDbContext db, IDomainEventDispatcher events)
{
    public async Task<Result<TenantSubscriptionResponse>> HandleAsync(
        Guid tenantId, ChangeTenantSubscriptionRequest request, CancellationToken cancellationToken)
    {
        Tenant? tenant = await db.Tenants.SingleOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return Error.NotFound("tenant.not_found", $"Tenant {tenantId} does not exist.");
        }

        SubscriptionPlan? plan = await db.SubscriptionPlans
            .SingleOrDefaultAsync(p => p.Id == request.SubscriptionPlanId, cancellationToken);
        if (plan is null || !plan.IsActive)
        {
            return Error.Validation("tenant.unknown_plan",
                $"Subscription plan {request.SubscriptionPlanId} does not exist or is not active.");
        }

        DateOnly validFrom = request.ValidFrom ?? DateOnly.FromDateTime(DateTime.UtcNow);

        TenantSubscription? current = await db.TenantSubscriptions
            .SingleOrDefaultAsync(s => s.TenantId == tenantId && s.ValidTo == null, cancellationToken);
        if (current is not null)
        {
            if (validFrom < current.ValidFrom)
            {
                return Error.Validation("tenant.subscription_overlap",
                    $"The new period cannot start before the current one ({current.ValidFrom:yyyy-MM-dd}).");
            }

            // Close the day before the new period starts; a same-day change collapses the
            // current period to a single day (CHECK valid_to >= valid_from must hold).
            DateOnly closeOn = validFrom.AddDays(-1);
            current.ValidTo = closeOn < current.ValidFrom ? current.ValidFrom : closeOn;
        }

        Guid previousPlanId = tenant.SubscriptionPlanId;
        tenant.SubscriptionPlanId = plan.Id;

        TenantSubscription period = new()
        {
            TenantId = tenantId,
            SubscriptionPlanId = plan.Id,
            ValidFrom = validFrom,
            Notes = request.Notes
        };
        db.TenantSubscriptions.Add(period);

        await db.SaveChangesAsync(cancellationToken);
        await events.DispatchAsync(
            new TenantSubscriptionChanged(tenantId, previousPlanId, plan.Id, validFrom), cancellationToken);

        return new TenantSubscriptionResponse(
            period.Id, plan.Id, plan.Code, plan.Label,
            period.ValidFrom, period.ValidTo, period.TrialUntil, period.Notes);
    }
}
