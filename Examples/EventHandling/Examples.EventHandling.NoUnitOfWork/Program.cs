using Examples.EventHandling.NoUnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;

// Recipe 4: "No UnitOfWork (direct publish), the escape hatch".
//
// Use RCommon eventing WITHOUT any UnitOfWork/persistence ceremony. The IEventBus is a framework
// singleton independent of any UoW -- just resolve it and publish.

Console.WriteLine("Example Starting");

using var provider = NoUnitOfWorkExample.BuildDirectPublishProvider();

// No UnitOfWork, no IUnitOfWorkFactory, no persistence -- just the event bus singleton.
var bus = provider.GetRequiredService<IEventBus>();
await bus.PublishAsync(new NotificationRequested(Guid.NewGuid(), "Welcome aboard"));

Console.WriteLine($"Handler invocations so far: {NotificationRequestedHandler.HandledCount}");
Console.WriteLine("Example Complete");
