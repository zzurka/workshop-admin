namespace WorkshopAdmin.SharedKernel.Events;

/// <summary>
/// Handles a domain event. Handlers run in-process, inside the publisher's request
/// scope — they share the request's <c>IDbSession</c> transaction, so their writes
/// commit or roll back atomically with the source change (outbox pattern).
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
