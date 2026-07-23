using Examples.EventHandling.MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.EventHandling.Producers;
using RCommon.MediatR;

// Recipe 5: DDD + in-process mediator (MediatR).
//
// A domain event raised by an aggregate is dispatched IN-PROCESS through MediatR to an
// ISubscriber<T> handler. MediatR is in-process only -- there is no broker and no transport.
//
// WithEventHandling<MediatREventHandlingBuilder> self-registers MediatR (it calls
// services.AddMediatR(...) internally), so there is no manual AddMediatR here. The wiring needs
// BOTH verbs: Publish<OrderPlacedEvent>() registers PublishWithMediatREventProducer (the in-process
// producer), and AddSubscriber<OrderPlacedEvent, OrderPlacedEventHandler>() bridges the RCommon
// subscriber to a MediatR notification handler. AddSubscriber alone does not register the producer.
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddRCommon()
            .WithSimpleGuidGenerator()
            .WithEventHandling<MediatREventHandlingBuilder>(events =>
            {
                events.Publish<OrderPlacedEvent>();
                events.AddSubscriber<OrderPlacedEvent, OrderPlacedEventHandler>();
            });
    })
    .Build();

Console.WriteLine("Example Starting");

// The aggregate raises the domain event via the DDD API.
var order = new Order { CustomerName = "Ada Lovelace", Total = 249.99m };
order.Place();
var domainEvent = (OrderPlacedEvent)order.LocalEvents.Single();

using (var scope = host.Services.CreateScope())
{
    // The MediatR builder registers a single IEventProducer: the publish producer. Producing with
    // the concrete event type lets it wrap the event as MediatRNotification<OrderPlacedEvent>, which
    // MediatR routes in-process to OrderPlacedEventHandler.
    var producer = scope.ServiceProvider.GetServices<IEventProducer>().Single();
    await producer.ProduceEventAsync(domainEvent);
}

Console.WriteLine($"Subscriber invocations so far: {OrderPlacedEventHandler.HandledCount}");
Console.WriteLine("Example Complete");
