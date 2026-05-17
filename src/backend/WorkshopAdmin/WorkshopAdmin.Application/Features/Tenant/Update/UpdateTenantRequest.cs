namespace WorkshopAdmin.Application.Features.Tenant.Update;

/// <summary>Edits mutable tenant details. Slug is immutable and not included.</summary>
public sealed record UpdateTenantRequest(
    string Name,
    string? ContactEmail,
    string? ContactPhone,
    string SubscriptionPlanCode,
    string CurrencyCode,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? PostalCode,
    string? Country);
