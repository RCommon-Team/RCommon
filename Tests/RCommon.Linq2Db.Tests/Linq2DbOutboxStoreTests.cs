using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence;
using RCommon.Persistence.Linq2Db.Outbox;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Linq2Db.Tests;

public class Linq2DbOutboxStoreTests
{
    [Fact]
    public void Constructor_ThrowsOnNullDataStoreFactory()
    {
        var act = () => new Linq2DbOutboxStore(
            null!,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullDefaultDataStoreOptions()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var act = () => new Linq2DbOutboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions()),
            Options.Create(new OutboxOptions()));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_SucceedsWithValidParameters()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var store = new Linq2DbOutboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()));

        store.Should().NotBeNull();
    }
}
