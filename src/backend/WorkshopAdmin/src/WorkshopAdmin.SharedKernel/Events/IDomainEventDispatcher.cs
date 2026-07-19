namespace WorkshopAdmin.SharedKernel.Events;

public interface IDomainEventDispatcher
{
    /// <summary>Invokes every registered handler for the event, sequentially, pre-commit.</summary>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
