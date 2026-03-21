using Microsoft.Extensions.DependencyInjection;
using RCommon.MassTransit.StateMachines;
using RCommon.StateMachines;

namespace RCommon;

/// <summary>
/// Extension methods for registering the MassTransit state machine adapter
/// with the RCommon builder pipeline.
/// </summary>
public static class MassTransitStateMachineBuilderExtensions
{
    /// <summary>
    /// Registers the MassTransit dictionary-based state machine as the implementation
    /// for <see cref="IStateMachineConfigurator{TState, TTrigger}"/>.
    /// </summary>
    /// <param name="builder">The RCommon builder to register services against.</param>
    /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
    public static IRCommonBuilder WithMassTransitStateMachine(this IRCommonBuilder builder)
    {
        builder.Services.AddTransient(typeof(IStateMachineConfigurator<,>), typeof(MassTransitStateMachineConfigurator<,>));
        return builder;
    }
}
