
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
                            mediator.AddNotification<TestNotification, TestNotificationHandler>();
                            mediator.AddRequest<TestRequest, TestRequestHandler>();
                            mediator.AddRequest<TestRequestWithResponse, TestResponse, TestRequestHandlerWithResponse>();
                            
                            // Additional configurations can be set like below
                            mediator.Configure(config =>
                            {
                                config.RegisterServicesFromAssemblies((typeof(Program).GetTypeInfo().Assembly));
                            });
                        });

                }).Build();

    Console.WriteLine("Example Starting");
    var mediatorService = host.Services.GetService<IMediatorService>();
    var notification = new TestNotification(DateTime.Now, Guid.NewGuid());
    var request = new TestRequest(DateTime.Now, Guid.NewGuid());
    var requestWithResponse = new TestRequestWithResponse(DateTime.Now, Guid.NewGuid());

    await mediatorService.Publish(notification); // For multiple handlers
    await mediatorService.Send(request); // For a single endpoint

    var response = await mediatorService.Send<TestRequestWithResponse, TestResponse>(requestWithResponse); // For a single endpoint with a response
    Console.WriteLine("Response: {0}", response.Message);

    Console.WriteLine("Example Complete");
    Console.ReadLine();         
}
catch (Exception ex)
{   
    Console.WriteLine(ex.ToString());
    
}

