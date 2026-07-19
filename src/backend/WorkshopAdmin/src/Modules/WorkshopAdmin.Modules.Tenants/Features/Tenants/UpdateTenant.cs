using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkshopAdmin.Modules.Codebook.Contracts;
using WorkshopAdmin.Modules.Tenants.Persistence;
using WorkshopAdmin.SharedKernel.Results;
using WorkshopAdmin.SharedKernel.Validation;

namespace WorkshopAdmin.Modules.Tenants.Features.Tenants;

/// <summary>PUT /api/tenants/{id} — contact, address and fiscal data plus operational
/// settings. The slug is immutable; the plan changes only through the subscriptions slice.</summary>
internal static class UpdateTenant
{
    public static void Map(RouteGroupBuilder group) =>
        // TODO(F3): permission "tenants:update"
        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateTenantRequest request,
                UpdateTenantHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult())
            .WithValidation<UpdateTenantRequest>()
            .WithSummary("Update tenant")
            .WithDescription("Updates contact, address, fiscal data (PIB, VAT status, bank account) and operational settings (default labor rate, arrival-reminder lead days). Slug and plan are not changed here.");
}

internal sealed record UpdateTenantRequest(
    string Name,
    short DefaultCurrencyId,
    string? ContactEmail = null,
    string? ContactPhone = null,
    string? TaxId = null,
    string? CompanyRegistrationNumber = null,
    bool IsVatRegistered = false,
    string? BankAccountNumber = null,
    decimal? DefaultLaborRate = null,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? City = null,
    string? PostalCode = null,
    string? Country = null,
    short? ArrivalReminderLeadDays = null);

internal sealed class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(255);
        RuleFor(r => r.ContactEmail).EmailAddress().When(r => !string.IsNullOrEmpty(r.ContactEmail));
        RuleFor(r => r.TaxId).Matches("^[0-9]{9}$").When(r => !string.IsNullOrEmpty(r.TaxId))
            .WithMessage("PIB must be exactly 9 digits.");
        RuleFor(r => r.DefaultLaborRate).GreaterThanOrEqualTo(0).When(r => r.DefaultLaborRate is not null);
        RuleFor(r => r.ArrivalReminderLeadDays).InclusiveBetween((short)0, (short)30)
            .When(r => r.ArrivalReminderLeadDays is not null);
    }
}

internal sealed class UpdateTenantHandler(TenantsDbContext db, ICodebookLookup codebook)
{
    public async Task<Result<TenantResponse>> HandleAsync(
        Guid id, UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        Tenant? tenant = await db.Tenants
            .Include(t => t.SubscriptionPlan)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tenant is null)
        {
            return Error.NotFound("tenant.not_found", $"Tenant {id} does not exist.");
        }

        CodebookEntryRef? currency = await codebook.GetByIdAsync(
            CodebookTypes.Currencies, request.DefaultCurrencyId, cancellationToken);
        if (currency is null || !currency.IsActive)
        {
            return Error.Validation("tenant.unknown_currency",
                $"Currency {request.DefaultCurrencyId} does not exist or is inactive.");
        }

        tenant.Name = request.Name;
        tenant.DefaultCurrencyId = request.DefaultCurrencyId;
        tenant.ContactEmail = request.ContactEmail;
        tenant.ContactPhone = request.ContactPhone;
        tenant.TaxId = request.TaxId;
        tenant.CompanyRegistrationNumber = request.CompanyRegistrationNumber;
        tenant.IsVatRegistered = request.IsVatRegistered;
        tenant.BankAccountNumber = request.BankAccountNumber;
        tenant.DefaultLaborRate = request.DefaultLaborRate;
        tenant.AddressLine1 = request.AddressLine1;
        tenant.AddressLine2 = request.AddressLine2;
        tenant.City = request.City;
        tenant.PostalCode = request.PostalCode;
        tenant.Country = request.Country;
        tenant.ArrivalReminderLeadDays = request.ArrivalReminderLeadDays;

        await db.SaveChangesAsync(cancellationToken);

        return TenantResponse.From(tenant);
    }
}
