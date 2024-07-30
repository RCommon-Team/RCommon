using Examples.ApplicationServices.CQRS.Validators;
using Examples.Caching.PersistenceCaching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.ExecutionResults;
using RCommon.FluentValidation;
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
                        .WithCQRS<CqrsBuilder>(cqrs =>
                        {
                            // You can do it this way which is pretty straight forward but verbose
                            //cqrs.AddQueryHandler<TestQueryHandler, TestQuery, TestDto>();
                            //cqrs.AddCommandHandler<TestCommandHandler, TestCommand, IExecutionResult>();

                            // Or this way which uses a little magic but is simple
                            cqrs.AddCommandHandlers((typeof(Program).GetTypeInfo().Assembly));
                            cqrs.AddQueryHandlers((typeof(Program).GetTypeInfo().Assembly));
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

