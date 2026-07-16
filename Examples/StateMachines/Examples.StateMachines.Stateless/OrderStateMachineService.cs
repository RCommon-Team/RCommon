using RCommon.StateMachines;

namespace Examples.StateMachines.Stateless;

public enum OrderState
{
    Pending,
    Approved,
    Shipped,
    Cancelled
}

public enum OrderTrigger
{
    Approve,
    Ship,
    Cancel
}

public class ShipmentDetails
{
    public string Carrier { get; set; } = string.Empty;
}

public class OrderStateMachineService
{
    private readonly IStateMachineConfigurator<OrderState, OrderTrigger> _configurator;
    private bool _hasStock = true;

    public OrderStateMachineService(IStateMachineConfigurator<OrderState, OrderTrigger> configurator)
    {
        _configurator = configurator;

        _configurator.ForState(OrderState.Pending)
            .Permit(OrderTrigger.Approve, OrderState.Approved)
            .Permit(OrderTrigger.Cancel, OrderState.Cancelled);

        _configurator.ForState(OrderState.Approved)
            .PermitIf(OrderTrigger.Ship, OrderState.Shipped, () => _hasStock)
            .Permit(OrderTrigger.Cancel, OrderState.Cancelled)
            .OnEntry(async ct =>
            {
                await Task.Delay(1, ct);
                Console.WriteLine("  [entry] Notifying warehouse of approved order");
            })
            .OnExit(async ct =>
            {
                await Task.Delay(1, ct);
                Console.WriteLine("  [exit] Leaving Approved");
            });

        _configurator.ForState(OrderState.Shipped)
            .OnEntry(async ct =>
            {
                await Task.Delay(1, ct);
                Console.WriteLine("  [entry] Sending shipping confirmation");
            });
    }

    public void SetStockAvailability(bool hasStock) => _hasStock = hasStock;

    public IStateMachine<OrderState, OrderTrigger> BuildFor(OrderState currentState)
        => _configurator.Build(currentState);
}
