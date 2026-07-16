using Examples.Persistence.Sagas;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.Persistence.Sagas;
using Xunit;

namespace Examples.Persistence.Sagas.Tests;

public class OrderFulfillmentSagaTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddRCommon().WithStatelessStateMachine();
        services.AddScoped(typeof(ISagaStore<,>), typeof(InMemorySagaStore<,>));
        services.AddScoped<OrderFulfillmentSaga>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task HappyPath_AllFourEvents_ReachesCompletedAndPersists()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var saga = scope.ServiceProvider.GetRequiredService<OrderFulfillmentSaga>();
        var store = scope.ServiceProvider.GetRequiredService<ISagaStore<OrderSagaState, Guid>>();

        var orderId = Guid.NewGuid();
        var state = new OrderSagaState
        {
            Id = orderId,
            OrderId = orderId,
            CorrelationId = orderId.ToString(),
            StartedAt = DateTimeOffset.UtcNow
        };

        await saga.HandleAsync(new PaymentReceivedEvent(orderId, "txn-001"), state);
        await saga.HandleAsync(new InventoryConfirmedEvent(orderId), state);
        await saga.HandleAsync(new ShipmentDispatchedEvent(orderId), state);
        await saga.HandleAsync(new OrderDeliveredEvent(orderId), state);

        state.CurrentStep.Should().Be(nameof(OrderSagaStep.Completed));
        state.IsFaulted.Should().BeFalse();

        var reloaded = await store.GetByIdAsync(orderId);
        reloaded.Should().NotBeNull();
        reloaded!.CurrentStep.Should().Be(nameof(OrderSagaStep.Completed));
    }

    [Fact]
    public async Task UnmappedEvent_TriggersStepFailed_AndCompensateAsyncMarksFaulted()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var saga = scope.ServiceProvider.GetRequiredService<OrderFulfillmentSaga>();

        var orderId = Guid.NewGuid();
        var state = new OrderSagaState
        {
            Id = orderId,
            OrderId = orderId,
            CorrelationId = orderId.ToString(),
            StartedAt = DateTimeOffset.UtcNow,
            PaymentTransactionId = "txn-002"
        };

        await saga.HandleAsync(new PaymentReceivedEvent(orderId, "txn-002"), state);
        await saga.HandleAsync(new InventoryUnavailableEvent(orderId), state);

        state.CurrentStep.Should().Be(nameof(OrderSagaStep.Faulted));

        await saga.CompensateAsync(state);

        state.IsFaulted.Should().BeTrue();
        state.FaultReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DisallowedTrigger_IsSilentlyIgnored_NotThrown()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var saga = scope.ServiceProvider.GetRequiredService<OrderFulfillmentSaga>();

        var orderId = Guid.NewGuid();
        var state = new OrderSagaState
        {
            Id = orderId,
            OrderId = orderId,
            CorrelationId = orderId.ToString(),
            StartedAt = DateTimeOffset.UtcNow
        };

        // OrderDelivered is not permitted from the initial Pending state -- CanFire is false,
        // so this must be a silent no-op rather than throwing.
        await saga.HandleAsync(new OrderDeliveredEvent(orderId), state);

        state.CurrentStep.Should().BeNullOrEmpty();
    }
}
