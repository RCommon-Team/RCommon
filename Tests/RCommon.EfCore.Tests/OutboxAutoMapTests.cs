using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.EfCore.Tests;

/// <summary>
/// A plain RCommonDbContext subclass whose developer-authored OnModelCreating does NOT
/// manually map the outbox. It relies entirely on the base-class auto-mapping.
/// </summary>
public class AutoMapDbContext : RCommonDbContext
{
    public AutoMapDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
        });
    }
}

/// <summary>
/// A RCommonDbContext subclass whose developer-authored OnModelCreating ALSO maps the outbox
/// manually. Combined with auto-mapping this must not throw (idempotency).
/// </summary>
public class ManualAndAutoMapDbContext : RCommonDbContext
{
    public ManualAndAutoMapDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddOutboxMessages();
    }
}

public class OutboxAutoMapTests
{
    [Fact]
    public void OutboxOwningDatastore_AutoMaps_OutboxMessage()
    {
        // Datastore "A" is registered as an outbox owner.
        var options = new DbContextOptionsBuilder<AutoMapDbContext>()
            .UseSqlite("DataSource=:memory:")
            .UseOutboxDataStore("A", RegistryContaining("A"))
            .Options;

        using var context = new AutoMapDbContext(options);

        context.Model.FindEntityType(typeof(OutboxMessage)).Should().NotBeNull(
            "datastore 'A' owns an outbox so OutboxMessage must be auto-mapped");
    }

    [Fact]
    public void NonOutboxDatastore_DoesNotMap_OutboxMessage()
    {
        // Datastore "C" is NOT in the registry.
        var options = new DbContextOptionsBuilder<AutoMapDbContext>()
            .UseSqlite("DataSource=:memory:")
            .UseOutboxDataStore("C", RegistryContaining("A"))
            .Options;

        using var context = new AutoMapDbContext(options);

        context.Model.FindEntityType(typeof(OutboxMessage)).Should().BeNull(
            "datastore 'C' does not own an outbox so OutboxMessage must not be mapped");
    }

    [Fact]
    public void ContextWithNoOutboxMarker_DoesNotMap_OutboxMessage()
    {
        // No outbox marker at all (e.g. persistence configured without any outbox).
        var options = new DbContextOptionsBuilder<AutoMapDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new AutoMapDbContext(options);

        context.Model.FindEntityType(typeof(OutboxMessage)).Should().BeNull();
    }

    [Fact]
    public void ManualAndAutoMap_IsIdempotent_AndBuildsWithoutError()
    {
        // Datastore "A" owns an outbox AND the developer manually maps it too.
        var options = new DbContextOptionsBuilder<ManualAndAutoMapDbContext>()
            .UseSqlite("DataSource=:memory:")
            .UseOutboxDataStore("A", RegistryContaining("A"))
            .Options;

        using var context = new ManualAndAutoMapDbContext(options);

        // Building the model (accessing .Model) must not throw despite double mapping.
        var act = () => context.Model.FindEntityType(typeof(OutboxMessage));
        act.Should().NotThrow();
        context.Model.FindEntityType(typeof(OutboxMessage)).Should().NotBeNull();
    }

    private static IOutboxDataStoreRegistry RegistryContaining(params string[] names)
        => new FakeOutboxDataStoreRegistry(names);

    private sealed class FakeOutboxDataStoreRegistry : IOutboxDataStoreRegistry
    {
        public FakeOutboxDataStoreRegistry(IReadOnlyCollection<string> registrations)
            => Registrations = registrations;

        public IReadOnlyCollection<string> Registrations { get; }
    }
}
