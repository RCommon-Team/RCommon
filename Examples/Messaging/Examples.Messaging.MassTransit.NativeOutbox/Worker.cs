using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon.Persistence.Transactions;

namespace Examples.Messaging.MassTransit.NativeOutbox;

/// <summary>
/// Demonstrates recipe 2b end to end: inside an RCommon UnitOfWork, add a business <see cref="Order"/> row
/// and publish an <see cref="OrderConfirmed"/> integration event through MassTransit's <c>IPublishEndpoint</c>.
/// Because <c>UseBrokerOutbox&lt;AppDbContext&gt;</c> configured <c>UseBusOutbox()</c>, that publish does NOT
/// hit the broker; it stages a MassTransit <c>OutboxMessage</c> row during the DbContext's SaveChanges, which
/// enlists in the UnitOfWork's ambient TransactionScope. Business row and outbox row commit atomically.
/// Requires a reachable Postgres database (see Program.cs); the deterministic proof lives in the integration test.
/// </summary>
public sealed class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(IServiceProvider serviceProvider, IHostApplicationLifetime lifetime)
    {
        _serviceProvider = serviceProvider;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Recipe 2b (MassTransit native broker outbox) example starting");

        using (var scope = _serviceProvider.CreateScope())
        {
            var sp = scope.ServiceProvider;

            // Ensure the schema (Orders + MassTransit outbox tables) exists for the demo.
            var ctx = sp.GetRequiredService<AppDbContext>();
            await ctx.Database.EnsureCreatedAsync(stoppingToken);

            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var publish = sp.GetRequiredService<IPublishEndpoint>();

            var order = new Order { CustomerName = "Ada Lovelace", Total = 249.99m };

            // The bus is deliberately not started in this demo path either — bus-outbox STAGING happens via
            // the scoped IPublishEndpoint + the DbContext SavingChanges interceptor during SaveChanges. It
            // does not require the bus to be running.
            using var uow = uowFactory.Create();
            ctx.Orders.Add(order);
            await publish.Publish(new OrderConfirmed(Guid.NewGuid(), order.CustomerName, order.Total), stoppingToken);
            await ctx.SaveChangesAsync(stoppingToken); // stages the MassTransit OutboxMessage row
            await uow.CommitAsync();

            Console.WriteLine(
                $"Order {order.Id} confirmed; MassTransit OutboxMessage staged atomically in the UnitOfWork transaction.");
        }

        Console.WriteLine("Recipe 2b example complete");
        _lifetime.StopApplication();
    }
}
