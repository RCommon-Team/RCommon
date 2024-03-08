using Examples.Messaging.Wolverine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.Wolverine.Producers;
using System.Diagnostics;
using Wolverine;

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseWolverine(options =>
        {
            options.LocalQueue("test");
        })
        .ConfigureAppConfiguration((context, builder) =>
        {

            ConfigurationContainer.Configuration = builder
                .Build();
        })
        .ConfigureServices(services =>
        {
            // Configure RCommon
            services.AddRCommon()
                .WithEventHandling<InMemoryEventBusBuilder>(eventHandling =>
                {
                    eventHandling.AddProducer<PublishWithWolverineEventProducer>();
                    eventHandling.AddSubscriber<TestEvent, TestEventHandler>();
                });

        }).Build();

    await host.StartAsync();

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

