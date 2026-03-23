using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence;
using RCommon.Persistence.Linq2Db.Inbox;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Linq2Db.Tests;

public class Linq2DbInboxStoreTests
{
    [Fact]
    public void Constructor_NullDataStoreFactory_ThrowsArgumentNullException()
    {
        var act = () => new Linq2DbInboxStore(
            null!,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()));

        act.Should().Throw<ArgumentNullException>().WithParameterName("dataStoreFactory");
    }

    [Fact]
    public void Constructor_NullDefaultDataStoreOptions_ThrowsArgumentNullException()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var act = () => new Linq2DbInboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions()),
            Options.Create(new OutboxOptions()));

        act.Should().Throw<ArgumentNullException>().WithParameterName("defaultDataStoreOptions");
    }

    [Fact]
    public void Constructor_SucceedsWithValidParameters()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var store = new Linq2DbInboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()));

        store.Should().NotBeNull();
    }
}
