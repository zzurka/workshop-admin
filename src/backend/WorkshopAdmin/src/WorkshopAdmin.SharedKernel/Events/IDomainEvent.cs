namespace WorkshopAdmin.SharedKernel.Events;

/// <summary>
/// Marker for domain events. Events are for side effects (outbox enqueue, cache
/// invalidation); a flow that must return a result is a contract call instead
/// (backend plan §8).
/// </summary>
public interface IDomainEvent;
