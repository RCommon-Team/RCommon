


using Examples.EventHandling.MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.EventHandling.Producers;
using RCommon.MediatR;
using RCommon.MediatR.Producers;
using System.Diagnostics;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

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
                        .WithEventHandling<MediatREventHandlingBuilder>(eventHandling =>
                        {
                            
                            eventHandling.AddProducer<PublishWithMediatREventProducer>();
                            //eventHandling.AddProducer<SendWithMediatREventProducer>();
                            eventHandling.AddSubscriber<TestEvent, TestEventHandler>();
                        });

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

