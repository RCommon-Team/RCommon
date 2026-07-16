using Examples.Persistence.Sagas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Persistence.Sagas;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddRCommon()
            .WithStatelessStateMachine();

        // InMemorySagaStore requires no real database -- see persistence/sagas.mdx's
        // "In-memory store" section. A real deployment would use the EFCore/Dapper/Linq2Db
        // saga store instead, registered automatically by that persistence provider.
        services.AddScoped(typeof(ISagaStore<,>), typeof(InMemorySagaStore<,>));
        services.AddScoped<OrderFulfillmentSaga>();
    })
    .Build();

Console.WriteLine("Example Starting");

using (var scope = host.Services.CreateScope())
{
    var saga = scope.ServiceProvider.GetRequiredService<OrderFulfillmentSaga>();
    var store = scope.ServiceProvider.GetRequiredService<ISagaStore<OrderSagaState, Guid>>();

    var orderId = Guid.NewGuid();
    var state = new OrderSagaState
    {
        Id = orderId,
        OrderId = orderId,
        CorrelationId = orderId.ToString(),
        Amount = 249.99m,
        StartedAt = DateTimeOffset.UtcNow
    };

    Console.WriteLine("--- Happy path: full fulfillment ---");
    await saga.HandleAsync(new PaymentReceivedEvent(orderId, "txn-001"), state);
    Console.WriteLine($"Step after payment: {state.CurrentStep}");

    state.InventoryReserved = true;
    state.PaymentTransactionId = "txn-001";
    await saga.HandleAsync(new InventoryConfirmedEvent(orderId), state);
    Console.WriteLine($"Step after inventory confirmed: {state.CurrentStep}");

    await saga.HandleAsync(new ShipmentDispatchedEvent(orderId), state);
    Console.WriteLine($"Step after shipment dispatched: {state.CurrentStep}");

    await saga.HandleAsync(new OrderDeliveredEvent(orderId), state);
    Console.WriteLine($"Step after delivery: {state.CurrentStep}");

    var reloaded = await store.GetByIdAsync(orderId);
    Console.WriteLine($"Reloaded from store: step={reloaded?.CurrentStep}, faulted={reloaded?.IsFaulted}");
}

using (var scope = host.Services.CreateScope())
{
    var saga = scope.ServiceProvider.GetRequiredService<OrderFulfillmentSaga>();

    var orderId = Guid.NewGuid();
    var state = new OrderSagaState
    {
        Id = orderId,
        OrderId = orderId,
        CorrelationId = orderId.ToString(),
        Amount = 89.00m,
        StartedAt = DateTimeOffset.UtcNow,
        PaymentTransactionId = "txn-002"
    };

    Console.WriteLine("--- Compensation path: payment succeeds, then inventory is unavailable ---");
    await saga.HandleAsync(new PaymentReceivedEvent(orderId, "txn-002"), state);
    Console.WriteLine($"Step after payment: {state.CurrentStep}");

    await saga.HandleAsync(new InventoryUnavailableEvent(orderId), state); // unmapped event -> StepFailed
    Console.WriteLine($"Step after inventory failure: {state.CurrentStep}");

    await saga.CompensateAsync(state);
    Console.WriteLine($"After compensation: faulted={state.IsFaulted}, reason={state.FaultReason}");
}

Console.WriteLine("Example Complete");
