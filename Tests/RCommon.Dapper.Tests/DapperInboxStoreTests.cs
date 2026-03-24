using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence;
using RCommon.Persistence.Dapper.Inbox;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Dapper.Tests;

public class DapperInboxStoreTests
{
    [Fact]
    public void Constructor_NullDataStoreFactory_ThrowsArgumentNullException()
    {
        var act = () => new DapperInboxStore(
            null!,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()));
        act.Should().Throw<ArgumentNullException>().WithParameterName("dataStoreFactory");
    }

    [Fact]
    public void Constructor_NullDefaultDataStoreOptions_ThrowsArgumentNullException()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var act = () => new DapperInboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions()),
            Options.Create(new OutboxOptions()));
        act.Should().Throw<ArgumentNullException>().WithParameterName("defaultDataStoreOptions");
    }

    [Fact]
    public void Constructor_SucceedsWithValidParameters()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var store = new DapperInboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()));
        store.Should().NotBeNull();
    }
}
