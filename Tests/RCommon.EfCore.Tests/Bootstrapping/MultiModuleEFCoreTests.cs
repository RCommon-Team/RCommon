using System;
using System.Linq;
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
/// End-to-end multi-module integration tests for <see cref="EFCorePerisistenceBuilder"/>
/// configured via <c>AddRCommon().WithPersistence&lt;EFCorePerisistenceBuilder&gt;(...)</c>.
///
/// Simulates two modules each independently calling <c>AddRCommon().WithPersistence(...)</c>
/// on the same <see cref="IServiceCollection"/> and verifies the sub-builder cache, the
/// idempotent data-store registration semantics, and the conflict detection all behave
/// as documented.
/// </summary>
public class MultiModuleEFCoreTests
{
    [Fact]
    public void TwoModules_DistinctDbContextsDistinctNames_BothResolvable()
    {
        // Arrange: two modules register their own DbContexts under distinct names
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextA>("DbA", o => o.UseInMemoryDatabase("DbA-" + Guid.NewGuid())));

        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextB>("DbB", o => o.UseInMemoryDatabase("DbB-" + Guid.NewGuid())));

        // Act
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDataStoreFactory>();

        // Use the single-generic Resolve<B>(name) overload: it correctly filters by both
        // name and base type. (The two-generic Resolve<B, C> overload has a known issue
        // where it peeks the first value in the bag without filtering -- out of scope here.)
        var dbA = factory.Resolve<RCommonDbContext>("DbA");
        var dbB = factory.Resolve<RCommonDbContext>("DbB");

        // Assert
        dbA.Should().NotBeNull();
        dbA.Should().BeOfType<TestDbContextA>();
        dbB.Should().NotBeNull();
        dbB.Should().BeOfType<TestDbContextB>();
    }

    [Fact]
    public void TwoModules_SameNameSameContext_IsIdempotent()
    {
        // Arrange: two modules each register the same DbContext under the same name
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextA>("DbA", o => o.UseInMemoryDatabase("DbA-Idem")));
        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextA>("DbA", o => o.UseInMemoryDatabase("DbA-Idem")));

        // Act: materialize the options to force the Register() validation to run
        using var provider = services.BuildServiceProvider();
        Action materialize = () =>
        {
            var opts = provider.GetRequiredService<IOptions<DataStoreFactoryOptions>>().Value;
            // Touch the values bag so the configure delegates have fully executed
            _ = opts.Values.Count;
        };

        // Assert
        materialize.Should().NotThrow();
    }

    [Fact]
    public void TwoModules_SameNameDifferentContext_Throws()
    {
        // Arrange: two modules register different DbContext types under the same name
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextA>("DbA", o => o.UseInMemoryDatabase("DbA-Conflict-A")));
        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextB>("DbA", o => o.UseInMemoryDatabase("DbA-Conflict-B")));

        // Act: the conflict is detected when the options are materialized (Configure delegates
        // run lazily inside IOptions.Value), so resolve IOptions to trigger the validation.
        using var provider = services.BuildServiceProvider();
        Action materialize = () =>
        {
            _ = provider.GetRequiredService<IOptions<DataStoreFactoryOptions>>().Value;
        };

        // Assert: the Configure delegate registered by AddDbContext propagates
        // UnsupportedDataStoreException directly out of IOptions<T>.Value materialization.
        materialize.Should().Throw<UnsupportedDataStoreException>()
            .WithMessage("*DbA*TestDbContextA*TestDbContextB*");
    }

    [Fact]
    public void TwoModules_RepositoryDescriptors_NotDuplicated()
    {
        // Arrange: two modules each call WithPersistence; the sub-builder cache should ensure
        // the EFCorePerisistenceBuilder constructor runs only once, so the repository registrations
        // it performs should each appear exactly once in the service collection.
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextA>("DbA", o => o.UseInMemoryDatabase("DbA-Dup1")));
        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextB>("DbB", o => o.UseInMemoryDatabase("DbB-Dup1")));

        // Assert: each generic repository interface registered by the sub-builder ctor should
        // appear exactly once.
        int CountOpenGeneric(Type openGenericServiceType) =>
            services.Count(d =>
                d.ServiceType.IsGenericTypeDefinition
                    ? d.ServiceType == openGenericServiceType
                    : d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == openGenericServiceType);

        CountOpenGeneric(typeof(IReadOnlyRepository<>)).Should().Be(1);
        CountOpenGeneric(typeof(IWriteOnlyRepository<>)).Should().Be(1);
        CountOpenGeneric(typeof(ILinqRepository<>)).Should().Be(1);
        CountOpenGeneric(typeof(IGraphRepository<>)).Should().Be(1);
    }

    /// <summary>
    /// Test DbContext used by module A. Must inherit directly from <see cref="RCommonDbContext"/>
    /// so that <see cref="DataStoreValue"/>'s <c>BaseType == typeof(RCommonDbContext)</c> check
    /// passes during registration.
    /// </summary>
    public class TestDbContextA : RCommonDbContext
    {
        public TestDbContextA(DbContextOptions<TestDbContextA> options) : base(options) { }
    }

    /// <summary>
    /// Test DbContext used by module B.
    /// </summary>
    public class TestDbContextB : RCommonDbContext
    {
        public TestDbContextB(DbContextOptions<TestDbContextB> options) : base(options) { }
    }
}
