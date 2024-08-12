using Examples.Caching.RedisCaching;
using RCommon.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Caching;
using RCommon.JsonNet;
using RCommon.RedisCache;
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
                        .WithJsonSerialization<JsonNetBuilder>()
                        .WithDistributedCaching<RedisCachingBuilder>(cache =>
                        {
                            cache.Configure(redis =>
                            {
                                // Redis Configuration
                            });

                        });

                    services.AddTransient<ITestApplicationService, TestApplicationService>();

                }).Build();

    Console.WriteLine("Example Starting");
    var appService = host.Services.GetRequiredService<ITestApplicationService>();

    // In Memory Distributed Cache
    appService.SetDistributedMemoryCache("test-key", typeof(TestDto), new TestDto("test data 1"));
    var testData1 = appService.GetDistributedMemoryCache("test-key");

    Console.WriteLine(testData1.Message);

    Console.WriteLine("Example Complete");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());

}


