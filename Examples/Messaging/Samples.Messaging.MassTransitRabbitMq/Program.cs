using Examples.Messaging.MassTransit;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.MassTransit;
using RCommon.MassTransit.Producers;
using System.Diagnostics;

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
                    // Configure RCommon
                    services.AddRCommon()
                        .WithEventHandling<MassTransitEventHandlingBuilder>(eventHandling =>
                        {
                            eventHandling.UsingInMemory((context, cfg) =>
                            {
                                cfg.ConfigureEndpoints(context);
                            });

                            eventHandling.AddProducer<PublishWithMassTransitEventProducer>();
                            eventHandling.AddSubscriber<TestEvent, TestEventHandler>();
                        });

                }).Build();

    Console.WriteLine("Example Starting");
    var eventProducers = host.Services.GetServices<IEventProducer>();
    var testEvent = new TestEvent(DateTime.Now, Guid.NewGuid());

    foreach (var producer in eventProducers)
    {
        Console.WriteLine($"Producer: {producer}");
        await producer.ProduceEventAsync(testEvent);
    }

    Console.WriteLine("Example Complete");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());

}

