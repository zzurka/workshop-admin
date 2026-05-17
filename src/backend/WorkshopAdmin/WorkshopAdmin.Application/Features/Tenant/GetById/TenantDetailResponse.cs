namespace WorkshopAdmin.Application.Features.Tenant.GetById;

public sealed record TenantDetailResponse(
    Guid Id,
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
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
