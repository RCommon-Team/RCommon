


using Examples.EventHandling.MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.Mediator;
using RCommon.Mediator.Producers;
using RCommon.MediatR;
using System.Diagnostics;

try
{
    //var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
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
                        .WithEventHandling<MediatREventHandlingConfiguration>(eventHandling =>
                        {
                            eventHandling.AddEvent<TestEvent>();
                            eventHandling.AddProducer<PublishByMediatorEventProducer>();
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
    Debug.WriteLine(ex.ToString());
}

