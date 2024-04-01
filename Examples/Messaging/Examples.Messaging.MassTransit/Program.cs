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
                    services.AddHostedService<Worker>();
                }).Build();




    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());

}

