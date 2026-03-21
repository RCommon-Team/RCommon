using System;
using FluentAssertions;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using Xunit;

namespace RCommon.Persistence.Tests;

public class IReadModelRepositoryTests
{
    [Fact]
    public void Interface_Has_IReadModel_Constraint()
    {
        var type = typeof(IReadModelRepository<>);
        var tReadModel = type.GetGenericArguments()[0];
        var constraints = tReadModel.GetGenericParameterConstraints();

        constraints.Should().Contain(typeof(IReadModel));
    }

    [Fact]
    public void Interface_Inherits_INamedDataSource()
    {
        var type = typeof(IReadModelRepository<>);
        type.GetInterfaces().Should().Contain(typeof(INamedDataSource));
    }

    [Fact]
    public void Interface_Has_Class_Constraint()
    {
        var type = typeof(IReadModelRepository<>);
        var tReadModel = type.GetGenericArguments()[0];
        var attrs = tReadModel.GenericParameterAttributes;

        attrs.HasFlag(System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint)
            .Should().BeTrue();
    }
}
