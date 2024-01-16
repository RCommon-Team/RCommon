


using Examples.EventHandling.MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using RCommon.Mediator.Producers;
using RCommon.MediatR;
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
                        .WithEventHandling<InMemoryEventBusBuilder>(eventHandling =>
                        {
                            eventHandling.AddProducer<PublishWithMediatorEventProducer>();
                            eventHandling.AddSubscriber<TestEvent, TestEventHandler>();
                            //services.AddTransient<IAppNotificationHandler<TestEvent>, TestEventHandler>();
                        });

                    Console.WriteLine($"Total Services Registered:");
                    Console.WriteLine(services.GenerateServiceDescriptorsString());

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

