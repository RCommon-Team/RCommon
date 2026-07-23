using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Wolverine;
using Xunit;

namespace RCommon.Wolverine.Outbox.Tests;

public class WolverineUseBrokerOutboxTests
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }

    [Fact]
    public void UseBrokerOutbox_throws_NotSupported_pointing_to_UseRCommonOutbox()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = new RCommonBuilder(services);

        Action act = () => builder.WithEventHandling<WolverineEventHandlingBuilder>(e =>
            e.UseBrokerOutbox<TestDbContext>(o => o.OnDataStore("Orders").UsePostgres()));

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*UseRCommonOutbox*");
    }
}
