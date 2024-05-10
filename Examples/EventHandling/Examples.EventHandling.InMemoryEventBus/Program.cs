﻿using Examples.EventHandling.InMemoryEventBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System.Diagnostics;
using System.Reflection;

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
                        .WithEventHandling<InMemoryEventBusBuilder>(eventHandling =>
                        {
                            eventHandling.AddProducer<PublishWithEventBusEventProducer>();

                            // You can add subscribers this way which is pretty straight forward but verbose
                            //eventHandling.AddSubscriber<TestEvent, TestEventHandler>();

                            // Or this way which uses a little magic but is simple
                            eventHandling.AddSubscribers((typeof(Program).GetTypeInfo().Assembly));
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

