
using Examples.Mediator.MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.EventHandling.Producers;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
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
                        .WithMediator<MediatRBuilder>(mediator =>
                        {
                            mediator.AddSubscriber<TestObject, TestNotificationHandler>();
                            mediator.Configure(config =>
                            {
                                config.RegisterServicesFromAssemblies((typeof(Program).GetTypeInfo().Assembly));
                            });
                        });

                    Console.WriteLine(services.GenerateServiceDescriptorsString());

                }).Build();

    Console.WriteLine("Example Starting");
    var mediatorService = host.Services.GetService<IMediatorService>();
    var notification = new TestObject(DateTime.Now, Guid.NewGuid());
    await mediatorService.Publish(notification);

    Console.WriteLine("Example Complete");
    Console.ReadLine();         
}
catch (Exception ex)
{   
    Console.WriteLine(ex.ToString());
    
}

