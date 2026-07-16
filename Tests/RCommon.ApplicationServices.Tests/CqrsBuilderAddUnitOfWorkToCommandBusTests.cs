using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.Commands;
using RCommon.ApplicationServices.Queries;
using RCommon.ApplicationServices.Validation;
using RCommon.Caching;
using RCommon.Persistence.Transactions;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

/// <summary>
/// DI-wiring coverage for ICqrsBuilder.AddUnitOfWorkToCommandBus() -- see
/// docs/specs/cqrs/native-command-bus-transactions.md. Confirms the opt-in registration only
/// swaps ICommandBus, leaves IQueryBus untouched, and stays opt-in (regression guard).
/// </summary>
public class CqrsBuilderAddUnitOfWorkToCommandBusTests
{
    private static ServiceCollection BuildServicesWithUnitOfWorkFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        services.AddSingleton(mockUnitOfWorkFactory.Object);

        // CommandBus/QueryBus constructor dependencies not otherwise wired by CqrsBuilder.
        services.AddSingleton(new Mock<IValidationService>().Object);
        services.AddSingleton(Options.Create(new CqrsValidationOptions()));
        services.AddSingleton(Options.Create(new CachingOptions { CachingEnabled = false }));

        return services;
    }

    [Fact]
    public void WithoutAddUnitOfWorkToCommandBus_ICommandBusResolvesToPlainCommandBus()
    {
        // Arrange -- regression guard: opt-in stays opt-in
        var services = BuildServicesWithUnitOfWorkFactory();
        var rcommonBuilder = new RCommonBuilder(services);
        rcommonBuilder.WithCQRS<CqrsBuilder>(cqrs => { });

        // Act
        var provider = services.BuildServiceProvider();
        var commandBus = provider.GetRequiredService<ICommandBus>();

        // Assert
        commandBus.Should().BeOfType<CommandBus>();
    }

    [Fact]
    public void AddUnitOfWorkToCommandBus_ICommandBusResolvesToUnitOfWorkCommandBus()
    {
        // Arrange
        var services = BuildServicesWithUnitOfWorkFactory();
        var rcommonBuilder = new RCommonBuilder(services);
        rcommonBuilder.WithCQRS<CqrsBuilder>(cqrs => cqrs.AddUnitOfWorkToCommandBus());

        // Act
        var provider = services.BuildServiceProvider();
        var commandBus = provider.GetRequiredService<ICommandBus>();

        // Assert
        commandBus.Should().BeOfType<UnitOfWorkCommandBus>();
    }

    [Fact]
    public void AddUnitOfWorkToCommandBus_DoesNotAffectIQueryBusRegistration()
    {
        // Arrange -- queries are read-only by CQRS convention and must not be wrapped
        var services = BuildServicesWithUnitOfWorkFactory();
        var rcommonBuilder = new RCommonBuilder(services);
        rcommonBuilder.WithCQRS<CqrsBuilder>(cqrs => cqrs.AddUnitOfWorkToCommandBus());

        // Act
        var provider = services.BuildServiceProvider();
        var queryBus = provider.GetRequiredService<IQueryBus>();

        // Assert
        queryBus.Should().BeOfType<QueryBus>();
    }
}
