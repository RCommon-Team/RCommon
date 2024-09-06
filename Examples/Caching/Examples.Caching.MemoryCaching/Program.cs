using Examples.Caching.MemoryCaching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Caching;
using RCommon.Json;
using RCommon.JsonNet;
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
                        .WithJsonSerialization<JsonNetBuilder>() // Distributed memory caching requires serialization
                        .WithMemoryCaching<InMemoryCachingBuilder>(cache =>
                        {
                            cache.Configure(x =>
                            {
                                x.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
                            });
                            cache.CacheDynamicallyCompiledExpressions();
                        })
                        .WithDistributedCaching<DistributedMemoryCacheBuilder>(cache =>
                        {
                            cache.Configure(x =>
                            {
                                x.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
                            });
                        });

                    services.AddTransient<ITestApplicationService, TestApplicationService>();
                    
                }).Build();

    Console.WriteLine("Example Starting");
    var appService = host.Services.GetRequiredService<ITestApplicationService>();

    // In Memory Cache
    appService.SetMemoryCache("test-key", new TestDto("test data 1"));
    var testData1 = appService.GetMemoryCache("test-key");

    // In Memory Distributed Cache
    appService.SetDistributedMemoryCache("test-key", typeof(TestDto), new TestDto("test data 2"));
    var testData2 = appService.GetDistributedMemoryCache("test-key");

    Console.WriteLine(testData1.Message);
    Console.WriteLine(testData2.Message);

    Console.WriteLine("Example Complete");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());

}

