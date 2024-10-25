
using Examples.Caching.PersistenceCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Caching;
using RCommon.MemoryCache;
using RCommon.Persistence;
using RCommon.Persistence.Caching;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.Transactions;
using RCommon.TestBase;
using RCommon.TestBase.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

try
{ 

var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    ConfigurationContainer.Configuration = builder.Build();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConfiguration>(ConfigurationContainer.Configuration);

                    // Configure RCommon
                    services.AddRCommon()
                        .WithPersistence<EFCorePerisistenceBuilder>(ef => // Repository/ORM configuration. We could easily swap out to Linq2Db without impact to domain service up through the stack
                        {
                            // Add all the DbContexts here
                            ef.AddDbContext<TestDbContext>("TestDbContext", ef =>
                            {
                                ef.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=RCommon_TestDatabase;Trusted_Connection=True;MultipleActiveResultSets=true");
                            });
                            ef.SetDefaultDataStore(dataStore =>
                            {
                                dataStore.DefaultDataStoreName = "TestDbContext";
                            });
                            ef.EnablePersistenceCaching(); // This gives us access to the caching repository interfaces/implementations
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

    Console.WriteLine("Seeding Data");
    var repo = new TestRepository(host.Services);
    repo.Prepare_Can_Find_Async_With_Expression();

    Console.WriteLine("Example Starting");
    var appService = host.Services.GetRequiredService<ITestApplicationService>();

    Console.WriteLine("Hitting the database w/ a query");
    var customers = await appService.GetCustomers("my-test-key");

    Console.WriteLine("Hitting the cache");
    customers = await appService.GetCustomers("my-test-key");

    Console.WriteLine("Example Complete");
    repo.CleanUpSeedData();
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());

}

