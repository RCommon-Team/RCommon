using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using Xunit;

namespace RCommon.EfCore.Tests.Bootstrapping;

/// <summary>
/// End-to-end coverage for the bootstrapping addendum in
/// docs/specs/bootstrapping/bootstrapping.md ("Default Data Store Inference & Improved Failure
/// Messaging"): a single registered data store is auto-inferred as the default, ambiguity across
/// multiple registered stores is left alone (not an error), an explicit SetDefaultDataStore(...)
/// call is never overridden, and the eventual DataStoreNotFoundException carries a useful message.
/// </summary>
public class DefaultDataStoreInferenceTests
{
    [Fact]
    public void SingleDataStoreRegistered_NoExplicitDefault_IsAutoInferred()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon().WithPersistence<EFCorePersistenceBuilder>(ef =>
            ef.AddDbContext<InferenceDbContext>("OnlyStore", o => o.UseInMemoryDatabase("Infer-" + Guid.NewGuid())));

        using var provider = services.BuildServiceProvider();

        // Act
        var defaultOptions = provider.GetRequiredService<IOptions<DefaultDataStoreOptions>>().Value;

        // Assert
        defaultOptions.DefaultDataStoreName.Should().Be("OnlyStore");
    }

    [Fact]
    public void SingleDataStoreRegistered_NoExplicitDefault_RepositoryResolvesWithoutSettingDataStoreName()
    {
        // Arrange -- the original bug report scenario: single-database app, SetDefaultDataStore
        // never called, repository used without setting DataStoreName explicitly.
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon().WithPersistence<EFCorePersistenceBuilder>(ef =>
            ef.AddDbContext<InferenceDbContext>("OnlyStore", o => o.UseInMemoryDatabase("Infer-" + Guid.NewGuid())));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var repository = scope.ServiceProvider.GetRequiredService<IGraphRepository<InferenceEntity>>();

        // Assert -- resolving the repository's underlying data store must not throw
        Action act = () => repository.FindAsync(e => true).GetAwaiter().GetResult();
        act.Should().NotThrow<DataStoreNotFoundException>();
    }

    [Fact]
    public void TwoDataStoresRegistered_NoExplicitDefault_EachRepositorySetsDataStoreNameExplicitly_NoException()
    {
        // Arrange -- regression guard: this is a documented, legitimate pattern
        // (website/docs/persistence/repository-pattern.mdx) and must never throw.
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon().WithPersistence<EFCorePersistenceBuilder>(ef =>
        {
            ef.AddDbContext<InferenceDbContext>("StoreA", o => o.UseInMemoryDatabase("A-" + Guid.NewGuid()));
            ef.AddDbContext<SecondInferenceDbContext>("StoreB", o => o.UseInMemoryDatabase("B-" + Guid.NewGuid()));
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var repository = scope.ServiceProvider.GetRequiredService<IGraphRepository<InferenceEntity>>();
        repository.DataStoreName = "StoreA";

        Action act = () => repository.FindAsync(e => true).GetAwaiter().GetResult();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TwoDataStoresRegistered_NoExplicitDefault_RepositoryUsedWithoutDataStoreName_ThrowsWithHelpfulMessage()
    {
        // Arrange -- the genuine misconfiguration case: ambiguous, and never resolved
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon().WithPersistence<EFCorePersistenceBuilder>(ef =>
        {
            ef.AddDbContext<InferenceDbContext>("StoreA", o => o.UseInMemoryDatabase("A-" + Guid.NewGuid()));
            ef.AddDbContext<SecondInferenceDbContext>("StoreB", o => o.UseInMemoryDatabase("B-" + Guid.NewGuid()));
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var repository = scope.ServiceProvider.GetRequiredService<IGraphRepository<InferenceEntity>>();

        Action act = () => repository.FindAsync(e => true).GetAwaiter().GetResult();

        // Assert
        act.Should().Throw<DataStoreNotFoundException>()
            .WithMessage("*SetDefaultDataStore*")
            .WithMessage("*StoreA*")
            .WithMessage("*StoreB*");
    }

    [Fact]
    public void ExplicitSetDefaultDataStore_IsNeverOverriddenByAutoInfer()
    {
        // Arrange -- explicit default set even though only one store is registered; must remain
        // exactly what the consumer set, not silently reassigned by the inference logic.
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon().WithPersistence<EFCorePersistenceBuilder>(ef =>
        {
            ef.AddDbContext<InferenceDbContext>("OnlyStore", o => o.UseInMemoryDatabase("Infer-" + Guid.NewGuid()));
            ef.SetDefaultDataStore(opts => opts.DefaultDataStoreName = "OnlyStore");
        });

        using var provider = services.BuildServiceProvider();

        // Act
        var defaultOptions = provider.GetRequiredService<IOptions<DefaultDataStoreOptions>>().Value;

        // Assert
        defaultOptions.DefaultDataStoreName.Should().Be("OnlyStore");
    }

    public class InferenceEntity : RCommon.Entities.BusinessEntity<Guid>
    {
    }

    public class InferenceDbContext : RCommonDbContext
    {
        public InferenceDbContext(DbContextOptions<InferenceDbContext> options) : base(options) { }

        public DbSet<InferenceEntity> Entities => Set<InferenceEntity>();
    }

    public class SecondInferenceDbContext : RCommonDbContext
    {
        public SecondInferenceDbContext(DbContextOptions<SecondInferenceDbContext> options) : base(options) { }
    }
}
