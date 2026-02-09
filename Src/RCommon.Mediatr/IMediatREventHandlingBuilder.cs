using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;

namespace RCommon.MediatR
{
    /// <summary>
    /// Builder interface for configuring MediatR-based event handling within the RCommon framework.
    /// Extends <see cref="IEventHandlingBuilder"/> to provide MediatR-specific event subscription capabilities.
    /// </summary>
    public interface IMediatREventHandlingBuilder : IEventHandlingBuilder
    {

    }
}
