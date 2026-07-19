using WorkshopAdmin.Modules.Tenants.Persistence;

namespace WorkshopAdmin.Modules.Tenants.Features.Tenants;

internal sealed record TenantListItem(
    Guid Id, string Name, string Slug, string? ContactEmail, string? ContactPhone, bool IsActive);

internal sealed record TenantPlanRef(Guid Id, string Code, Dictionary<string, string> Label);

internal sealed record TenantResponse(
    Guid Id,
    string Name,
    string Slug,
    string? ContactEmail,
    string? ContactPhone,
    short DefaultCurrencyId,
    string? TaxId,
    string? CompanyRegistrationNumber,
    bool IsVatRegistered,
    string? BankAccountNumber,
    decimal? DefaultLaborRate,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? PostalCode,
    string? Country,
    short? ArrivalReminderLeadDays,
    bool IsActive,
    DateTimeOffset CreatedAt,
    TenantPlanRef CurrentPlan)
{
    public static TenantResponse From(Tenant tenant)
    {
        SubscriptionPlan plan = tenant.SubscriptionPlan
            ?? throw new InvalidOperationException("Tenant loaded without its subscription plan.");

        return new TenantResponse(
            tenant.Id, tenant.Name, tenant.Slug, tenant.ContactEmail, tenant.ContactPhone,
            tenant.DefaultCurrencyId, tenant.TaxId, tenant.CompanyRegistrationNumber,
            tenant.IsVatRegistered, tenant.BankAccountNumber, tenant.DefaultLaborRate,
            tenant.AddressLine1, tenant.AddressLine2, tenant.City, tenant.PostalCode, tenant.Country,
            tenant.ArrivalReminderLeadDays, tenant.IsActive, tenant.CreatedAt,
            new TenantPlanRef(plan.Id, plan.Code, plan.Label));
    }
}
