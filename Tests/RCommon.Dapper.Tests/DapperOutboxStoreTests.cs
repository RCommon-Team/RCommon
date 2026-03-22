using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence;
using RCommon.Persistence.Dapper.Outbox;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Sql;
using System.Data;
using System.Data.Common;
using Xunit;

namespace RCommon.Dapper.Tests;

public class DapperOutboxStoreTests
{
    [Fact]
    public void Constructor_ThrowsOnNullDataStoreFactory()
    {
        var act = () => new DapperOutboxStore(
            null!,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullDefaultDataStoreOptions()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var act = () => new DapperOutboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions()),
            Options.Create(new OutboxOptions()));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_SucceedsWithValidParameters()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var store = new DapperOutboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()));

        store.Should().NotBeNull();
    }
}
