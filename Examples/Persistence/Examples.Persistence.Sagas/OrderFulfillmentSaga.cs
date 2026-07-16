using RCommon.Models.Events;
using RCommon.Persistence.Sagas;
using RCommon.StateMachines;

namespace Examples.Persistence.Sagas;

public enum OrderSagaStep
{
    Pending,
    PaymentProcessed,
    InventoryReserved,
    Shipped,
    Completed,
    Faulted
}

public enum OrderSagaTrigger
{
    PaymentReceived,
    InventoryConfirmed,
    ShipmentDispatched,
    OrderDelivered,
    StepFailed
}

public class OrderFulfillmentSaga
    : SagaOrchestrator<OrderSagaState, Guid, OrderSagaStep, OrderSagaTrigger>
{
    public OrderFulfillmentSaga(
        ISagaStore<OrderSagaState, Guid> store,
        IStateMachineConfigurator<OrderSagaStep, OrderSagaTrigger> configurator)
        : base(store, configurator)
    {
    }

    protected override OrderSagaStep InitialState => OrderSagaStep.Pending;

    protected override void ConfigureStateMachine(
        IStateMachineConfigurator<OrderSagaStep, OrderSagaTrigger> configurator)
    {
        configurator.ForState(OrderSagaStep.Pending)
            .Permit(OrderSagaTrigger.PaymentReceived, OrderSagaStep.PaymentProcessed)
            .Permit(OrderSagaTrigger.StepFailed, OrderSagaStep.Faulted);

        configurator.ForState(OrderSagaStep.PaymentProcessed)
            .Permit(OrderSagaTrigger.InventoryConfirmed, OrderSagaStep.InventoryReserved)
            .Permit(OrderSagaTrigger.StepFailed, OrderSagaStep.Faulted);

        configurator.ForState(OrderSagaStep.InventoryReserved)
            .Permit(OrderSagaTrigger.ShipmentDispatched, OrderSagaStep.Shipped)
            .Permit(OrderSagaTrigger.StepFailed, OrderSagaStep.Faulted);

        configurator.ForState(OrderSagaStep.Shipped)
            .Permit(OrderSagaTrigger.OrderDelivered, OrderSagaStep.Completed);
    }

    protected override OrderSagaTrigger MapEventToTrigger<TEvent>(TEvent @event)
    {
        return @event switch
        {
            PaymentReceivedEvent => OrderSagaTrigger.PaymentReceived,
            InventoryConfirmedEvent => OrderSagaTrigger.InventoryConfirmed,
            ShipmentDispatchedEvent => OrderSagaTrigger.ShipmentDispatched,
            OrderDeliveredEvent => OrderSagaTrigger.OrderDelivered,
            _ => OrderSagaTrigger.StepFailed
        };
    }

    public override async Task CompensateAsync(OrderSagaState state, CancellationToken ct = default)
    {
        if (state.InventoryReserved)
        {
            // Release reserved inventory (no-op in this example).
        }

        if (!string.IsNullOrEmpty(state.PaymentTransactionId))
        {
            // Issue refund (no-op in this example).
        }

        state.IsFaulted = true;
        state.FaultReason = "Compensation executed";
        await Store.SaveAsync(state, ct);
    }
}
