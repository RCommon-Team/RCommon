
using Examples.Caching.PersistenceCaching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using System.Diagnostics;
using System.Reflection;

try
{
    /*var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {

                    ConfigurationContainer.Configuration = builder
                        .Build();
                })
                .ConfigureServices(services =>
                {
                    // Configure RCommon
                    services.AddRCommon();
                    
                    services.AddTransient<ITestApplicationService, TestApplicationService>();
                    
                }).Build();

    Console.WriteLine("Example Starting");
    var appService = host.Services.GetRequiredService<ITestApplicationService>();*/

    Console.WriteLine("");
    Console.WriteLine("");

    Console.WriteLine("Example Complete");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());

}

