using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;

namespace RCommon.MassTransit
{
    public interface IMassTransitEventHandlingBuilder : IEventHandlingBuilder, IBusRegistrationConfigurator
    {
        
    }
}
