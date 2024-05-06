using Examples.ApplicationServices.CQRS;
using Examples.ApplicationServices.CQRS.Validators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.ExecutionResults;
using RCommon.FluentValidation;
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
                        .WithCQRS<CqrsBuilder>(cqrs =>
                        {
                            cqrs.AddQueryHandler<TestQueryHandler, TestQuery, TestDto>();
                            cqrs.AddCommandHandler<TestCommandHandler, TestCommand, IExecutionResult>();
                        })
                        .WithValidation<FluentValidationBuilder>(validation =>
                        {
                            validation.AddValidatorsFromAssemblyContaining(typeof(TestCommand));

                            validation.UseWithCqrs(options =>
                            {
                                options.ValidateCommands = true;
                                options.ValidateQueries = true;
                            });
                        });
                    Console.WriteLine(services.GenerateServiceDescriptorsString());
                    services.AddTransient<ITestApplicationService, TestApplicationService>();
                    
                }).Build();

    Console.WriteLine("Example Starting");
    var appService = host.Services.GetRequiredService<ITestApplicationService>();
    var commandResult = await appService.ExecuteTestCommand(new TestCommand("test"));
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

