using Microsoft.Extensions.DependencyInjection;
using WorkshopAdmin.SharedKernel.Events;
using Xunit;

namespace WorkshopAdmin.UnitTests.SharedKernel;

public class DomainEventDispatcherTests
{
    private sealed record OrderCompleted(Guid OrderId) : IDomainEvent;

    private sealed record SomethingElse : IDomainEvent;

    private sealed class Recorder
    {
        public List<string> Calls { get; } = [];
    }

    private sealed class FirstHandler(Recorder recorder) : IDomainEventHandler<OrderCompleted>
    {
        public Task HandleAsync(OrderCompleted domainEvent, CancellationToken cancellationToken)
        {
            recorder.Calls.Add($"first:{domainEvent.OrderId}");
            return Task.CompletedTask;
        }
    }

    private sealed class SecondHandler(Recorder recorder) : IDomainEventHandler<OrderCompleted>
    {
        public Task HandleAsync(OrderCompleted domainEvent, CancellationToken cancellationToken)
        {
            recorder.Calls.Add($"second:{domainEvent.OrderId}");
            return Task.CompletedTask;
        }
    }

    private sealed class UnrelatedHandler(Recorder recorder) : IDomainEventHandler<SomethingElse>
    {
        public Task HandleAsync(SomethingElse domainEvent, CancellationToken cancellationToken)
        {
            recorder.Calls.Add("unrelated");
            return Task.CompletedTask;
        }
    }

    private static (DomainEventDispatcher Dispatcher, Recorder Recorder) Build()
    {
        ServiceCollection services = new();
        services.AddSingleton<Recorder>();
        services.AddScoped<IDomainEventHandler<OrderCompleted>, FirstHandler>();
        services.AddScoped<IDomainEventHandler<OrderCompleted>, SecondHandler>();
        services.AddScoped<IDomainEventHandler<SomethingElse>, UnrelatedHandler>();
        ServiceProvider provider = services.BuildServiceProvider();

        return (new DomainEventDispatcher(provider), provider.GetRequiredService<Recorder>());
    }

    [Fact]
    public async Task Dispatch_InvokesAllHandlersOfTheEventType_InOrder()
    {
        (DomainEventDispatcher dispatcher, Recorder recorder) = Build();
        Guid orderId = Guid.NewGuid();

        await dispatcher.DispatchAsync(new OrderCompleted(orderId));

        Assert.Equal([$"first:{orderId}", $"second:{orderId}"], recorder.Calls);
    }

    [Fact]
    public async Task Dispatch_DoesNotInvokeHandlersOfOtherEvents()
    {
        (DomainEventDispatcher dispatcher, Recorder recorder) = Build();

        await dispatcher.DispatchAsync(new OrderCompleted(Guid.NewGuid()));

        Assert.DoesNotContain("unrelated", recorder.Calls);
    }

    [Fact]
    public async Task Dispatch_EventWithNoHandlers_IsANoOp()
    {
        DomainEventDispatcher dispatcher = new(new ServiceCollection().BuildServiceProvider());

        await dispatcher.DispatchAsync(new OrderCompleted(Guid.NewGuid()));
    }
}
