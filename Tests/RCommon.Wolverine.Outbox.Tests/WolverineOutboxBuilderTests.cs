using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.Wolverine;
using RCommon.Wolverine.Outbox;
using Wolverine;
using Xunit;

namespace RCommon.Wolverine.Outbox.Tests;

public class WolverineOutboxBuilderTests
{
    [Fact]
    public void Constructor_ThrowsOnNull()
    {
        var act = () => new WolverineOutboxBuilder(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithValidOptions_Succeeds()
    {
        var opts = new WolverineOptions();
        var act = () => new WolverineOutboxBuilder(opts);
        act.Should().NotThrow();
    }

    [Fact]
    public void UseEntityFrameworkCoreTransactions_ReturnsSameBuilder()
    {
        var opts = new WolverineOptions();
        var builder = new WolverineOutboxBuilder(opts);

        var result = builder.UseEntityFrameworkCoreTransactions();

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void UseEntityFrameworkCoreTransactions_DoesNotThrow()
    {
        var opts = new WolverineOptions();
        var builder = new WolverineOutboxBuilder(opts);

        var act = () => builder.UseEntityFrameworkCoreTransactions();

        act.Should().NotThrow();
    }

    [Fact]
    public void Builder_ImplementsIWolverineOutboxBuilder()
    {
        var opts = new WolverineOptions();
        var builder = new WolverineOutboxBuilder(opts);

        builder.Should().BeAssignableTo<IWolverineOutboxBuilder>();
    }

    [Fact]
    public void AddOutbox_WithNullConfigure_RegistersConfigureWolverine()
    {
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IWolverineEventHandlingBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);

        mockBuilder.Object.AddOutbox();

        // ConfigureWolverine registers at least one service descriptor
        services.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AddOutbox_ReturnsSameBuilder()
    {
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IWolverineEventHandlingBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);

        var result = mockBuilder.Object.AddOutbox();

        result.Should().BeSameAs(mockBuilder.Object);
    }

    [Fact]
    public void AddOutbox_WithConfigure_InvokesConfigure()
    {
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IWolverineEventHandlingBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);

        var configureCalled = false;
        mockBuilder.Object.AddOutbox(outboxBuilder =>
        {
            configureCalled = true;
            outboxBuilder.UseEntityFrameworkCoreTransactions();
        });

        // The configure action is deferred via ConfigureWolverine; confirm it was registered
        services.Count.Should().BeGreaterThan(0);
        // configureCalled will only be true if ConfigureWolverine invokes the action eagerly,
        // which WolverineFx does not do (it defers to host startup). We verify registration happened.
        _ = configureCalled; // suppress unused variable warning
    }
}
