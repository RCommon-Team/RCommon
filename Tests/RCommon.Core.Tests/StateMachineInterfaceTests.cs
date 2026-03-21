using System;
using System.Linq;
using FluentAssertions;
using RCommon.StateMachines;
using Xunit;

namespace RCommon.Core.Tests;

public class StateMachineInterfaceTests
{
    [Fact]
    public void IStateMachine_Has_Struct_And_Enum_Constraints()
    {
        var type = typeof(IStateMachine<,>);
        var tState = type.GetGenericArguments()[0];
        var tTrigger = type.GetGenericArguments()[1];

        tState.GenericParameterAttributes.HasFlag(
            System.Reflection.GenericParameterAttributes.NotNullableValueTypeConstraint)
            .Should().BeTrue("TState must be struct");
        tState.GetGenericParameterConstraints().Should().Contain(typeof(Enum));

        tTrigger.GenericParameterAttributes.HasFlag(
            System.Reflection.GenericParameterAttributes.NotNullableValueTypeConstraint)
            .Should().BeTrue("TTrigger must be struct");
        tTrigger.GetGenericParameterConstraints().Should().Contain(typeof(Enum));
    }

    [Fact]
    public void IStateMachine_Has_Required_Members()
    {
        var type = typeof(IStateMachine<,>);
        type.GetProperty("CurrentState").Should().NotBeNull();
        type.GetProperty("PermittedTriggers").Should().NotBeNull();
        type.GetMethod("CanFire").Should().NotBeNull();
        type.GetMethods().Where(m => m.Name == "FireAsync").Should().HaveCountGreaterThanOrEqualTo(2,
            "should have FireAsync and FireAsync<TData> overloads");
    }

    [Fact]
    public void IStateMachineConfigurator_Has_ForState_And_Build()
    {
        var type = typeof(IStateMachineConfigurator<,>);
        type.GetMethod("ForState").Should().NotBeNull();
        type.GetMethod("Build").Should().NotBeNull();
    }

    [Fact]
    public void IStateConfigurator_Has_Required_Members()
    {
        var type = typeof(IStateConfigurator<,>);
        type.GetMethod("Permit").Should().NotBeNull();
        type.GetMethod("OnEntry").Should().NotBeNull();
        type.GetMethod("OnExit").Should().NotBeNull();
        type.GetMethod("PermitIf").Should().NotBeNull();
    }
}
