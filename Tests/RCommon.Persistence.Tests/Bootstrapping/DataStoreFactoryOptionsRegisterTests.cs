using System;
using System.Data.Common;
using System.Threading.Tasks;
using FluentAssertions;
using RCommon.Persistence;
using Xunit;

namespace RCommon.Persistence.Tests.Bootstrapping;

public class DataStoreFactoryOptionsRegisterTests
{
    [Fact]
    public void Register_SameName_SameBase_SameConcrete_IsIdempotent()
    {
        var options = new DataStoreFactoryOptions();

        options.Register<FakeBase, FakeConcreteA>("DataStoreA");
        Action secondCall = () => options.Register<FakeBase, FakeConcreteA>("DataStoreA");

        secondCall.Should().NotThrow();
        options.Values.Should().HaveCount(1);
    }

    [Fact]
    public void Register_SameName_SameBase_DifferentConcrete_Throws()
    {
        var options = new DataStoreFactoryOptions();
        options.Register<FakeBase, FakeConcreteA>("DataStoreA");

        Action act = () => options.Register<FakeBase, FakeConcreteB>("DataStoreA");

        act.Should().Throw<UnsupportedDataStoreException>()
            .WithMessage("*DataStoreA*FakeConcreteA*FakeConcreteB*");
    }

    [Fact]
    public void Register_DifferentNames_RegistersBoth()
    {
        var options = new DataStoreFactoryOptions();

        options.Register<FakeBase, FakeConcreteA>("DataStoreA");
        options.Register<FakeBase, FakeConcreteB>("DataStoreB");

        options.Values.Should().HaveCount(2);
    }

    // DataStoreValue's constructor validates concreteType.BaseType == baseType (CLR base class,
    // not implemented interface), so fakes must inherit through a concrete abstract class.
    public abstract class FakeBase : IDataStore
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public DbConnection GetDbConnection() => throw new NotSupportedException("Test fake");
    }
    public class FakeConcreteA : FakeBase { }
    public class FakeConcreteB : FakeBase { }
}
