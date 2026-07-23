using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.MassTransit;
using Xunit;

namespace RCommon.MassTransit.Outbox.Tests;

public class UseBrokerOutboxTests
{
    // Minimal EF DbContext to serve as TDbContext. AddEntityFrameworkOutbox<T> only registers
    // services at config time; it does not require the context to be otherwise registered, and the
    // tests below never build/start MassTransit's hosted services.
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }

    private static (ServiceCollection services, RCommonBuilder builder) NewHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return (services, new RCommonBuilder(services));
    }

    [Fact]
    public void Options_OnDataStore_records_name_and_rejects_blank()
    {
        var o = new MassTransitBrokerOutboxOptions();
        o.OnDataStore("Orders");
        o.DataStoreName.Should().Be("Orders");

        Action bad = () => new MassTransitBrokerOutboxOptions().OnDataStore("  ");
        bad.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Options_provider_selection()
    {
        new MassTransitBrokerOutboxOptions().UsePostgres().Provider
            .Should().Be(BrokerOutboxProvider.Postgres);
        new MassTransitBrokerOutboxOptions().UseSqlServer().Provider
            .Should().Be(BrokerOutboxProvider.SqlServer);
    }

    [Fact]
    public void UseBrokerOutbox_throws_when_OnDataStore_is_omitted()
    {
        var (_, builder) = NewHost();
        Action act = () => builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
            e.UseBrokerOutbox<TestDbContext>(o => o.UsePostgres()));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OnDataStore*");
    }

    [Fact]
    public void UseBrokerOutbox_throws_when_no_provider_selected()
    {
        var (_, builder) = NewHost();
        Action act = () => builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
            e.UseBrokerOutbox<TestDbContext>(o => o.OnDataStore("Orders")));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*UsePostgres*UseSqlServer*");
    }

    [Fact]
    public void UseBrokerOutbox_records_datastore_to_dbcontext_mapping()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
            e.UseBrokerOutbox<TestDbContext>(o => o.OnDataStore("Orders").UsePostgres()));

        using var provider = services.BuildServiceProvider();
        var reg = provider.GetRequiredService<IOptions<MassTransitBrokerOutboxRegistrationOptions>>().Value;
        reg.Registrations.Should().ContainSingle(r =>
            r.DataStoreName == "Orders" && r.DbContextType == typeof(TestDbContext));
    }
}
