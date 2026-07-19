using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace WorkshopAdmin.SharedKernel.Events;

/// <summary>
/// In-process dispatcher: resolves all <see cref="IDomainEventHandler{TEvent}"/> for the
/// runtime event type from the current scope and awaits them one by one. Exceptions
/// propagate to the caller — a failing handler fails the whole transaction, by design.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, (Type HandlerType, MethodInfo HandleMethod)> Cache = new();

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        (Type handlerType, MethodInfo handleMethod) = Cache.GetOrAdd(domainEvent.GetType(), static eventType =>
        {
            Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            return (handlerType, handlerType.GetMethod(nameof(IDomainEventHandler<>.HandleAsync))!);
        });

        foreach (object? handler in serviceProvider.GetServices(handlerType))
        {
            if (handler is not null)
            {
                await (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
