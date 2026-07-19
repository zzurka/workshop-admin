using WorkshopAdmin.SharedKernel.Events;

namespace WorkshopAdmin.Modules.Tenants.Contracts;

/// <summary>
/// Raised when a tenant moves to a different subscription plan. Handlers run in the
/// same transaction, before commit (backend plan §8.3). No subscribers yet —
/// Notifications enqueues a confirmation email from F3.
/// </summary>
public sealed record TenantSubscriptionChanged(
    Guid TenantId, Guid PreviousPlanId, Guid NewPlanId, DateOnly ValidFrom) : IDomainEvent;
