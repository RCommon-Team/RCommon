using System;
using System.Reflection;
using FluentAssertions;
using RCommon.Entities;
using RCommon.Persistence.Crud;
using Xunit;

namespace RCommon.Persistence.Tests;

public class IAggregateRepositoryTests
{
    [Fact]
    public void Interface_Has_IAggregateRoot_Constraint_On_TAggregate()
    {
        var type = typeof(IAggregateRepository<,>);
        var genericArgs = type.GetGenericArguments();
        var tAggregate = genericArgs[0];
        var constraints = tAggregate.GetGenericParameterConstraints();

        constraints.Should().Contain(t => t.IsGenericType
            && t.GetGenericTypeDefinition() == typeof(IAggregateRoot<>),
            "TAggregate must be constrained to IAggregateRoot<TKey>");
    }

    [Fact]
    public void Interface_Has_IEquatable_Constraint_On_TKey()
    {
        var type = typeof(IAggregateRepository<,>);
        var genericArgs = type.GetGenericArguments();
        var tKey = genericArgs[1];
        var constraints = tKey.GetGenericParameterConstraints();

        constraints.Should().Contain(t => t.IsGenericType
            && t.GetGenericTypeDefinition() == typeof(IEquatable<>),
            "TKey must be constrained to IEquatable<TKey>");
    }

    [Fact]
    public void Interface_Inherits_INamedDataSource()
    {
        var type = typeof(IAggregateRepository<,>);
        type.GetInterfaces().Should().Contain(typeof(INamedDataSource));
    }

    [Fact]
    public void Interface_Does_Not_Inherit_ILinqRepository()
    {
        var type = typeof(IAggregateRepository<,>);
        var interfaces = type.GetInterfaces();
        interfaces.Should().NotContain(i => i.Name.Contains("ILinqRepository"));
        interfaces.Should().NotContain(i => i.Name.Contains("IGraphRepository"));
        interfaces.Should().NotContain(i => i.Name.Contains("IReadOnlyRepository"));
    }
}
