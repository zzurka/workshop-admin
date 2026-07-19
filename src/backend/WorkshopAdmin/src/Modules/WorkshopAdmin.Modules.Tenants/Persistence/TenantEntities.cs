using WorkshopAdmin.SharedKernel.Persistence;

namespace WorkshopAdmin.Modules.Tenants.Persistence;

/// <summary>One workshop / organization using the system (tenant.tenants).</summary>
internal sealed class Tenant : AuditableEntity
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public short DefaultCurrencyId { get; set; }
    public string? TaxId { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public bool IsVatRegistered { get; set; }
    public string? BankAccountNumber { get; set; }
    public decimal? DefaultLaborRate { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public short? ArrivalReminderLeadDays { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public SubscriptionPlan? SubscriptionPlan { get; set; }
}

/// <summary>Billable product with pricing, cadence, limits and feature flags (tenant.subscription_plans).</summary>
internal sealed class SubscriptionPlan : AuditableEntity
{
    public string Code { get; set; } = "";
    public Dictionary<string, string> Label { get; set; } = [];
    public Dictionary<string, string>? Description { get; set; }
    public decimal Price { get; set; }
    public short CurrencyId { get; set; }
    public short BillingPeriodId { get; set; }
    public short TrialDays { get; set; }
    public int? MaxUsers { get; set; }
    public int? MaxVehicles { get; set; }
    public int? MaxWorkOrdersPerMonth { get; set; }
    public int? MaxStorageMb { get; set; }

    /// <summary>Raw JSON object of feature flags, mapped to the jsonb column as-is.</summary>
    public string Features { get; set; } = "{}";

    public bool IsPublic { get; set; } = true;
    public short SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>One subscription period in a tenant's history (tenant.tenant_subscriptions).
/// At most one open period (valid_to IS NULL) per tenant, DB-enforced.</summary>
internal sealed class TenantSubscription : AuditableEntity
{
    public Guid TenantId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public DateOnly? TrialUntil { get; set; }
    public string? Notes { get; set; }

    public Tenant? Tenant { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }
}
