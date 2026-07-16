using Examples.StateMachines.Stateless;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddRCommon()
            .WithStatelessStateMachine();

        services.AddSingleton<OrderStateMachineService>();
    })
    .Build();

Console.WriteLine("Example Starting");
var orderStateService = host.Services.GetRequiredService<OrderStateMachineService>();

Console.WriteLine("--- Happy path: Pending -> Approved -> Shipped ---");
var machine = orderStateService.BuildFor(OrderState.Pending);
Console.WriteLine($"Current state: {machine.CurrentState}");
Console.WriteLine($"Permitted triggers: {string.Join(", ", machine.PermittedTriggers)}");

await machine.FireAsync(OrderTrigger.Approve);
Console.WriteLine($"Current state: {machine.CurrentState}");

await machine.FireAsync(OrderTrigger.Ship, new ShipmentDetails { Carrier = "FedEx" });
Console.WriteLine($"Current state: {machine.CurrentState}");

Console.WriteLine();
Console.WriteLine("--- Guarded transition: Ship is blocked when out of stock ---");
orderStateService.SetStockAvailability(false);
var blockedMachine = orderStateService.BuildFor(OrderState.Approved);
Console.WriteLine($"Can fire Ship: {blockedMachine.CanFire(OrderTrigger.Ship)}");

// Unlike SagaOrchestrator (which checks CanFire internally before firing), calling FireAsync
// directly on a disallowed trigger throws -- always check CanFire first when driving a machine
// by hand.
if (blockedMachine.CanFire(OrderTrigger.Ship))
{
    await blockedMachine.FireAsync(OrderTrigger.Ship);
}
Console.WriteLine($"Current state (unchanged): {blockedMachine.CurrentState}");

await blockedMachine.FireAsync(OrderTrigger.Cancel);
Console.WriteLine($"Current state after cancel: {blockedMachine.CurrentState}");

Console.WriteLine("Example Complete");
