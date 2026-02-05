using Examples.Messaging.SubscriptionIsolation;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.MassTransit;
using RCommon.MassTransit.Producers;

try
{
    var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    ConfigurationContainer.Configuration = builder
                        .Build();
                })
                .ConfigureServices(services =>
                {
                    // Configure RCommon with two event handling builders demonstrating subscription isolation.
                    //
                    // InMemoryOnlyEvent is subscribed ONLY through InMemoryEventBusBuilder,
                    // so only PublishWithEventBusEventProducer will handle it.
                    //
                    // MassTransitOnlyEvent is subscribed ONLY through MassTransitEventHandlingBuilder,
                    // so only PublishWithMassTransitEventProducer will handle it.
                    //
                    // SharedEvent is subscribed through BOTH builders,
                    // so both producers will handle it.
                    services.AddRCommon()
                        .WithEventHandling<InMemoryEventBusBuilder>(eventHandling =>
                        {
                            eventHandling.AddProducer<PublishWithEventBusEventProducer>();
                            eventHandling.AddSubscriber<InMemoryOnlyEvent, InMemoryOnlyEventHandler>();
                            eventHandling.AddSubscriber<SharedEvent, SharedEventHandler>();
                        })
                        .WithEventHandling<MassTransitEventHandlingBuilder>(eventHandling =>
                        {
                            eventHandling.UsingInMemory((context, cfg) =>
                            {
                                cfg.ConfigureEndpoints(context);
                            });

                            eventHandling.AddProducer<PublishWithMassTransitEventProducer>();
                            eventHandling.AddSubscriber<MassTransitOnlyEvent, MassTransitOnlyEventHandler>();
                            eventHandling.AddSubscriber<SharedEvent, SharedEventHandler>();
                        });

                    services.AddHostedService<Worker>();
                }).Build();

    Console.WriteLine("=== Event Subscription Isolation Example ===");
    Console.WriteLine();
    Console.WriteLine("This example demonstrates that events are only routed to the");
    Console.WriteLine("producer/handler associated with the builder that subscribed them:");
    Console.WriteLine("  - InMemoryOnlyEvent -> InMemoryEventBus only");
    Console.WriteLine("  - MassTransitOnlyEvent -> MassTransit only");
    Console.WriteLine("  - SharedEvent -> Both InMemoryEventBus and MassTransit");
    Console.WriteLine();

    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}
