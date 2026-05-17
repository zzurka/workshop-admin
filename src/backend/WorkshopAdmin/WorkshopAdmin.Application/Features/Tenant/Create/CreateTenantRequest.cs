namespace WorkshopAdmin.Application.Features.Tenant.Create;

/// <summary>
/// Creates a tenant and its first tenant_admin user atomically (one
/// transaction). Subscription plan and currency are referenced by their stable
/// codes, not ids.
/// </summary>
public sealed record CreateTenantRequest(
    string Name,
    string Slug,
    string? ContactEmail,
    string? ContactPhone,
    string SubscriptionPlanCode,
    string CurrencyCode,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? PostalCode,
    string? Country,
    InitialAdmin Admin);

/// <summary>The first tenant_admin user provisioned with the tenant.</summary>
public sealed record InitialAdmin(
    string Email,
    string FirstName,
    string LastName,
    string Password);
