using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;

namespace RCommon.MassTransit
{
    /// <summary>
    /// Builder interface for configuring MassTransit-based event handling within the RCommon framework.
    /// Combines <see cref="IEventHandlingBuilder"/> for RCommon event wiring with
    /// <see cref="IBusRegistrationConfigurator"/> for MassTransit bus configuration.
    /// </summary>
    public interface IMassTransitEventHandlingBuilder : IEventHandlingBuilder, IBusRegistrationConfigurator
    {

    }
}
