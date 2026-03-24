using Microsoft.Extensions.DependencyInjection;
using RCommon.Stateless;
using RCommon.StateMachines;

namespace RCommon;

/// <summary>
/// Provides extension methods on <see cref="IRCommonBuilder"/> for registering the Stateless
/// state machine adapter into the RCommon configuration pipeline.
/// </summary>
public static class StatelessBuilderExtensions
{
    /// <summary>
    /// Registers the Stateless library as the <see cref="IStateMachineConfigurator{TState, TTrigger}"/>
    /// implementation, enabling state machine support throughout the application.
    /// </summary>
    /// <param name="builder">The RCommon builder instance.</param>
    /// <returns>The <see cref="IRCommonBuilder"/> for further chaining.</returns>
    public static IRCommonBuilder WithStatelessStateMachine(this IRCommonBuilder builder)
    {
        builder.Services.AddTransient(typeof(IStateMachineConfigurator<,>), typeof(StatelessConfigurator<,>));
        return builder;
    }
}
