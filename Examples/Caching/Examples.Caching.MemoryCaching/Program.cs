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

                            
                        });
                    
                    services.AddTransient<ITestApplicationService, TestApplicationService>();
                    
                }).Build();

    Console.WriteLine("Example Starting");
    var appService = host.Services.GetRequiredService<ITestApplicationService>();
    await appService.SetCache();
    await appService.GetCache();

    Console.WriteLine("");
    Console.WriteLine("");

    Console.WriteLine("Example Complete");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());

}

