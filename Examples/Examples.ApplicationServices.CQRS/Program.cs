using Examples.ApplicationServices.CQRS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.ExecutionResults;
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
                        .WithCQRS<CqrsBuilder>(builder =>
                        {
                            builder.AddQueryHandler<TestQueryHandler, TestQuery, TestDto>();
                            builder.AddCommandHandler<TestCommandHandler, TestCommand, IExecutionResult>();
                        });

                    services.AddTransient<ITestApplicationService, TestApplicationService>();

                }).Build();

    Console.WriteLine("Example Starting");

    var appService = host.Services.GetRequiredService<ITestApplicationService>();
    var commandResult = await appService.ExecuteTestCommand(new TestCommand());
    var queryResult = await appService.ExecuteTestQuery(new TestQuery());

    Console.WriteLine(commandResult.ToString());
    Console.WriteLine(queryResult.Message);

    Console.WriteLine("Example Complete");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());

}

