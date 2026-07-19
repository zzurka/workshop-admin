using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Codebook.Contracts;
using WorkshopAdmin.Modules.Tenants.Persistence;
using WorkshopAdmin.SharedKernel.Results;
using WorkshopAdmin.SharedKernel.Validation;

namespace WorkshopAdmin.Modules.Tenants.Features.Tenants;

/// <summary>POST /api/tenants — creates the tenant and its initial subscription period
/// in the same request transaction (shared-transaction proof for the F2 pattern).</summary>
internal static class CreateTenant
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "tenants:create" (platform scope)
        group.MapPost("/", async (
                CreateTenantRequest request,
                CreateTenantHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(request, cancellationToken))
                .ToCreatedResult(tenant => $"/api/tenants/{tenant.Id}"))
            .WithValidation<CreateTenantRequest>();
}

internal sealed record CreateTenantRequest(
    string Name,
    string Slug,
    Guid SubscriptionPlanId,
    short DefaultCurrencyId,
    string? ContactEmail = null,
    string? ContactPhone = null,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? City = null,
    string? PostalCode = null,
    string? Country = null);

internal sealed class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(255);
        RuleFor(r => r.Slug).NotEmpty().MaximumLength(100).Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, digits and hyphens.");
        RuleFor(r => r.ContactEmail).EmailAddress().When(r => !string.IsNullOrEmpty(r.ContactEmail));
    }
}

internal sealed class CreateTenantHandler(TenantsDbContext db, ICodebookLookup codebook)
{
    public async Task<Result<TenantResponse>> HandleAsync(
        CreateTenantRequest request, CancellationToken cancellationToken)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Slug == request.Slug, cancellationToken))
        {
            return Error.Conflict("tenant.duplicate_slug", $"Slug '{request.Slug}' is already taken.");
        }

        SubscriptionPlan? plan = await db.SubscriptionPlans
            .SingleOrDefaultAsync(p => p.Id == request.SubscriptionPlanId, cancellationToken);
        if (plan is null || !plan.IsActive)
        {
            return Error.Validation("tenant.unknown_plan",
                $"Subscription plan {request.SubscriptionPlanId} does not exist or is not active.");
        }

        CodebookEntryRef? currency = await codebook.GetByIdAsync(
            CodebookTypes.Currencies, request.DefaultCurrencyId, cancellationToken);
        if (currency is null || !currency.IsActive)
        {
            return Error.Validation("tenant.unknown_currency",
                $"Currency {request.DefaultCurrencyId} does not exist or is inactive.");
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        Tenant tenant = new()
        {
            Name = request.Name,
            Slug = request.Slug,
            SubscriptionPlanId = plan.Id,
            DefaultCurrencyId = request.DefaultCurrencyId,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,
            SubscriptionPlan = plan
        };

        db.Tenants.Add(tenant);
        db.TenantSubscriptions.Add(new TenantSubscription
        {
            Tenant = tenant,
            SubscriptionPlanId = plan.Id,
            ValidFrom = today,
            TrialUntil = plan.TrialDays > 0 ? today.AddDays(plan.TrialDays) : null
        });

        await db.SaveChangesAsync(cancellationToken);

        return TenantResponse.From(tenant);
    }
}
