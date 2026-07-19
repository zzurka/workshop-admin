namespace WorkshopAdmin.Application.Features.Tenant.List;

public sealed record TenantListItem(
    Guid Id,
    string Name,
    string Slug,
    string? ContactEmail,
    string SubscriptionPlanCode,
    bool IsActive,
    DateTime CreatedAt);
