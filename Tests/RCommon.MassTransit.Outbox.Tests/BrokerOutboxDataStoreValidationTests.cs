using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RCommon.Persistence;
using Xunit;

namespace RCommon.MassTransit.Outbox.Tests;

/// <summary>
/// Covers the fail-loud startup validation that rejects a UseBrokerOutbox binding whose named
/// datastore either (a) isn't registered in DataStoreFactoryOptions or (b) is registered with a
/// different concrete DbContext type than the one declared in UseBrokerOutbox&lt;TDbContext&gt;.
/// </summary>
public class BrokerOutboxDataStoreValidationTests
{
    // Two minimal EF DbContexts used as type tokens — no real DB required.
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }

    public class OtherDbContext : DbContext
    {
        public OtherDbContext(DbContextOptions<OtherDbContext> options) : base(options) { }
    }

    private static BrokerOutboxDataStoreValidationHostedService BuildValidator(
        DataStoreFactoryOptions dsOptions,
        MassTransitBrokerOutboxRegistrationOptions brokerOptions)
    {
        return new BrokerOutboxDataStoreValidationHostedService(
            Options.Create(brokerOptions),
            Options.Create(dsOptions));
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_When_Name_And_ConcreteType_Match()
    {
        // Arrange: datastore "Orders" registered with TestDbContext; broker binding also uses TestDbContext.
        var dsOptions = new DataStoreFactoryOptions();
        dsOptions.Values.Add(new DataStoreValue("Orders", typeof(DbContext), typeof(TestDbContext)));

        var brokerOptions = new MassTransitBrokerOutboxRegistrationOptions();
        brokerOptions.Register("Orders", typeof(TestDbContext));

        var sut = BuildValidator(dsOptions, brokerOptions);

        // Act & Assert
        Func<Task> act = () => sut.StartAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_Throws_When_ConcreteType_Does_Not_Match()
    {
        // Arrange: datastore "Orders" registered with OtherDbContext; broker binding declares TestDbContext.
        var dsOptions = new DataStoreFactoryOptions();
        dsOptions.Values.Add(new DataStoreValue("Orders", typeof(DbContext), typeof(OtherDbContext)));

        var brokerOptions = new MassTransitBrokerOutboxRegistrationOptions();
        brokerOptions.Register("Orders", typeof(TestDbContext));

        var sut = BuildValidator(dsOptions, brokerOptions);

        // Act
        Func<Task> act = () => sut.StartAsync(CancellationToken.None);

        // Assert: exception names the datastore and both types.
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*Orders*");
        ex.Which.Message.Should().ContainAny(nameof(TestDbContext), nameof(OtherDbContext));
    }

    [Fact]
    public async Task StartAsync_Throws_When_DataStore_Name_Not_Registered()
    {
        // Arrange: DataStoreFactoryOptions has no registrations; broker binding names "Orders".
        var dsOptions = new DataStoreFactoryOptions();

        var brokerOptions = new MassTransitBrokerOutboxRegistrationOptions();
        brokerOptions.Register("Orders", typeof(TestDbContext));

        var sut = BuildValidator(dsOptions, brokerOptions);

        // Act
        Func<Task> act = () => sut.StartAsync(CancellationToken.None);

        // Assert: exception names the missing datastore.
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Orders*");
    }
}
