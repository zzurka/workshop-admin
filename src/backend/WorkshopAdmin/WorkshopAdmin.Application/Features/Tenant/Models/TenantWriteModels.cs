namespace WorkshopAdmin.Application.Features.Tenant.Models;

/// <summary>Resolved values for inserting a tenant.tenants row.</summary>
public sealed record TenantInsert(
    string Name,
    string Slug,
    string? ContactEmail,
    string? ContactPhone,
    Guid SubscriptionPlanId,
    short DefaultCurrencyId,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? PostalCode,
    string? Country);

/// <summary>Resolved values for updating a tenant.tenants row (slug is immutable).</summary>
public sealed record TenantUpdate(
    string Name,
    string? ContactEmail,
    string? ContactPhone,
    Guid SubscriptionPlanId,
    short DefaultCurrencyId,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? PostalCode,
    string? Country);
