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
    private readonly Mock<ILockStatementProvider> _lockProviderMock = new();

    public Linq2DbOutboxStoreTests()
    {
        _lockProviderMock.Setup(l => l.ProviderName).Returns("SqlServer");
    }

    [Fact]
    public void Constructor_ThrowsOnNullDataStoreFactory()
    {
        var act = () => new Linq2DbOutboxStore(
            null!,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()),
            _lockProviderMock.Object);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ThrowsOnNullDefaultDataStoreOptions()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var act = () => new Linq2DbOutboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions()),
            Options.Create(new OutboxOptions()),
            _lockProviderMock.Object);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_SucceedsWithValidParameters()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var store = new Linq2DbOutboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()),
            _lockProviderMock.Object);

        store.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullLockStatementProvider_ThrowsArgumentNullException()
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        var act = () => new Linq2DbOutboxStore(
            factoryMock.Object,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }),
            Options.Create(new OutboxOptions()),
            null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("lockStatementProvider");
    }
}
