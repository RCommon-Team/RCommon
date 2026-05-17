using Examples.Bootstrapping.MultiModule.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.EventHandling.Producers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        IServiceModule[] modules =
        {
            new OrderingModule(),
            new InventoryModule(),
            new NotificationsModule(),
        };

        Console.WriteLine($"IsRCommonInitialized before any module: {services.IsRCommonInitialized()}");

        foreach (var module in modules)
        {
            module.Configure(services);
            Console.WriteLine($"  {module.GetType().Name} configured; IsRCommonInitialized={services.IsRCommonInitialized()}");
        }
    })
    .Build();

await host.StartAsync();

var sp = host.Services;
var producerCount = sp.GetServices<IEventProducer>().Count();
Console.WriteLine($"\nDistinct IEventProducer instances resolved: {producerCount}");
Console.WriteLine("Expected: 1 (AuditProducer was registered by Ordering and Notifications but dedup keeps it to one).\n");

var builder = sp.GetRequiredService<IRCommonBuilder>();
var diagnostics = builder.GetBootstrapDiagnostics();
if (!string.IsNullOrEmpty(diagnostics))
{
    Console.WriteLine("Bootstrap diagnostics report:");
    Console.WriteLine(diagnostics);
}
else
{
    Console.WriteLine("Bootstrap diagnostics: no soft duplicates detected.");
}

await host.StopAsync();
