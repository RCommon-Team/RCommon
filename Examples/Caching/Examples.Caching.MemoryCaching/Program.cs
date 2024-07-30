using Examples.Caching.MemoryCaching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Caching;
using RCommon.MemoryCache;
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
                        .WithCaching<MemoryCachingBuilder>(cache =>
                        {

                            // Or this way which uses a little magic but is simple
                            cqrs.AddCommandHandlers((typeof(Program).GetTypeInfo().Assembly));
                            cqrs.AddQueryHandlers((typeof(Program).GetTypeInfo().Assembly));
                        });
                    
                    services.AddTransient<ITestApplicationService, TestApplicationService>();
                    
                }).Build();

    Console.WriteLine("Example Starting");
    var appService = host.Services.GetRequiredService<ITestApplicationService>();
    var commandResult = await appService(new TestCommand("test"));
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

