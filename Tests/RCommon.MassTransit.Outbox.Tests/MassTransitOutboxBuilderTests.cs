using FluentAssertions;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Moq;
using RCommon.MassTransit.Outbox;
using Xunit;

namespace RCommon.MassTransit.Outbox.Tests;

public class MassTransitOutboxBuilderTests
{
    /// <summary>
    /// UseSqlServer and UsePostgres are extension methods on IEntityFrameworkOutboxConfigurator.
    /// They cannot be verified via Moq directly, so we verify that they set the LockStatementProvider
    /// property on the underlying configurator, which is the actual contract being fulfilled.
    /// </summary>
    [Fact]
    public void UseSqlServer_SetsLockStatementProviderOnConfigurator()
    {
        var configuratorMock = new Mock<IEntityFrameworkOutboxConfigurator>();
        var builder = new MassTransitOutboxBuilder(configuratorMock.Object);

        var result = builder.UseSqlServer();

        result.Should().BeSameAs(builder);
        // UseSqlServer() is an extension method that sets LockStatementProvider to SqlServerLockStatementProvider
        configuratorMock.VerifySet(c => c.LockStatementProvider = It.IsAny<ILockStatementProvider>(), Times.Once);
    }

    [Fact]
    public void UsePostgres_SetsLockStatementProviderOnConfigurator()
    {
        var configuratorMock = new Mock<IEntityFrameworkOutboxConfigurator>();
        var builder = new MassTransitOutboxBuilder(configuratorMock.Object);

        var result = builder.UsePostgres();

        result.Should().BeSameAs(builder);
        // UsePostgres() is an extension method that sets LockStatementProvider to PostgresLockStatementProvider
        configuratorMock.VerifySet(c => c.LockStatementProvider = It.IsAny<ILockStatementProvider>(), Times.Once);
    }

    [Fact]
    public void UseBusOutbox_DelegatesToConfigurator()
    {
        var configuratorMock = new Mock<IEntityFrameworkOutboxConfigurator>();
        var builder = new MassTransitOutboxBuilder(configuratorMock.Object);

        var result = builder.UseBusOutbox();

        result.Should().BeSameAs(builder);
        configuratorMock.Verify(c => c.UseBusOutbox(It.IsAny<Action<IBusOutboxConfigurator>>()), Times.Once);
    }

    [Fact]
    public void Constructor_ThrowsOnNull()
    {
        var act = () => new MassTransitOutboxBuilder(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
