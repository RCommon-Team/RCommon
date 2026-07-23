using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RCommon.IntegrationTests.Fixtures;
using Xunit;

namespace RCommon.IntegrationTests;

[Trait("Category", "Integration")]   // REQUIRED: excludes this from the fast (no-container) CI job
[Collection(PostgreSqlCollection.Name)]
public class HarnessSmokeTests
{
    private readonly PostgreSqlFixture _pg;
    public HarnessSmokeTests(PostgreSqlFixture pg) => _pg = pg;

    private sealed class Widget { public int Id { get; set; } public string Name { get; set; } = ""; }

    private sealed class SmokeContext(DbContextOptions<SmokeContext> o) : DbContext(o)
    {
        public DbSet<Widget> Widgets => Set<Widget>();
    }

    [Fact]
    public async Task Postgres_container_round_trips_a_row()
    {
        var options = new DbContextOptionsBuilder<SmokeContext>()
            .UseNpgsql(_pg.ConnectionString).Options;

        await using var ctx = new SmokeContext(options);
        await ctx.Database.EnsureCreatedAsync();

        ctx.Widgets.Add(new Widget { Name = "hello" });
        await ctx.SaveChangesAsync();

        var count = await ctx.Widgets.CountAsync(w => w.Name == "hello");
        count.Should().Be(1);
    }
}
