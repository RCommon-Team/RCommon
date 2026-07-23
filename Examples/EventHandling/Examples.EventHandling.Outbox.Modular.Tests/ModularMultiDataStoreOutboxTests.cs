using Examples.EventHandling.Outbox.Modular.Billing;
using Examples.EventHandling.Outbox.Modular.Ordering;
using Examples.EventHandling.Outbox.Modular.Shipping;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.Entities;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Transactions;
using Xunit;

namespace Examples.EventHandling.Outbox.Modular.Tests;

/// <summary>
/// End-to-end proof of the modular multi-datastore outbox example, and the regression guard for the
/// customer's scenario (3 bounded-context modules, each its own datastore + native outbox, composed at
/// one root). Every datastore's durable event must persist to its OWN outbox and nowhere else. Before
/// 3.2.1 this composition silently dropped durable events for datastores whose module was registered
/// after a different module's WithPersistence call. Runs on the EF Core InMemory provider (fast lane).
/// </summary>
public class ModularMultiDataStoreOutboxTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon()
            .WithSimpleGuidGenerator()
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            // Compose modules NOT-primary-first, to prove the fixed registration is order-independent.
            .AddBillingModule(Guid.NewGuid().ToString())
            .AddShippingModule(Guid.NewGuid().ToString())
            .AddOrderingModule(Guid.NewGuid().ToString());

        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public void OutboxTracker_IsAuthoritative_UnderThreeModuleComposition()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IEntityEventTracker>()
            .Should().BeOfType<OutboxEntityEventTracker>(
                "the outbox tracker must win across a three-module composition regardless of order");
    }

    [Fact]
    public async Task EachModule_PersistsItsDurableEvent_ToItsOwnOutboxOnly()
    {
        using var provider = BuildProvider();

        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<BillingDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<ShippingDbContext>().Database.EnsureCreatedAsync();
        }

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();

            var orders = sp.GetRequiredService<IAggregateRepository<Order, Guid>>();
            orders.DataStoreName = "Ordering";
            var invoices = sp.GetRequiredService<IAggregateRepository<Invoice, Guid>>();
            invoices.DataStoreName = "Billing";
            var shipments = sp.GetRequiredService<IAggregateRepository<Shipment, Guid>>();
            shipments.DataStoreName = "Shipping";

            var order = new Order { CustomerName = "Ada Lovelace", Total = 249.99m };
            order.Place();
            var invoice = new Invoice { CustomerName = "Ada Lovelace", Amount = 249.99m };
            invoice.Raise();
            var shipment = new Shipment { Destination = "London", TrackingNumber = "TRK-1" };
            shipment.Dispatch();

            using var uow = uowFactory.Create();
            await orders.AddAsync(order);
            await invoices.AddAsync(invoice);
            await shipments.AddAsync(shipment);
            await uow.CommitAsync();
        }

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var orderingRows = await sp.GetRequiredService<OrderingDbContext>().Set<OutboxMessage>().AsNoTracking().CountAsync();
            var billingRows = await sp.GetRequiredService<BillingDbContext>().Set<OutboxMessage>().AsNoTracking().CountAsync();
            var shippingRows = await sp.GetRequiredService<ShippingDbContext>().Set<OutboxMessage>().AsNoTracking().CountAsync();

            orderingRows.Should().Be(1, "the Ordering module's durable event must persist to the Ordering outbox");
            billingRows.Should().Be(1, "the Billing module's durable event must persist to the Billing outbox");
            shippingRows.Should().Be(1, "the Shipping module's durable event must persist to the Shipping outbox");
        }
    }
}
