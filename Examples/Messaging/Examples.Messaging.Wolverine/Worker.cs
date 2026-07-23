using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Transactions;

namespace Examples.Messaging.Wolverine;

/// <summary>
/// Demonstrates recipe 2a end to end: create + confirm an <see cref="Order"/>, then commit a UnitOfWork.
/// The commit stages the durable <see cref="OrderConfirmed"/> event into RCommon's __OutboxMessages
/// table atomically with the business row; the outbox poller relays it to the Wolverine producer
/// post-commit. Requires a reachable Postgres database (see Program.cs).
/// </summary>
public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Recipe 2a (Wolverine) example starting");

        using (var scope = _serviceProvider.CreateScope())
        {
            var sp = scope.ServiceProvider;

            // Ensure the schema (Orders + __OutboxMessages) exists for the demo.
            var ctx = sp.GetRequiredService<AppDbContext>();
            await ctx.Database.EnsureCreatedAsync(stoppingToken);

            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var repo = sp.GetRequiredService<IAggregateRepository<Order, Guid>>();
            repo.DataStoreName = "Orders";

            var order = new Order { CustomerName = "Ada Lovelace", Total = 249.99m };
            order.Confirm(); // raises OrderConfirmed

            using var uow = uowFactory.Create();
            await repo.AddAsync(order, stoppingToken);
            await uow.CommitAsync();

            Console.WriteLine($"Order {order.Id} confirmed and OrderConfirmed staged in RCommon's outbox.");
        }

        Console.WriteLine("Recipe 2a example complete");
        _lifetime.StopApplication();
    }
}
