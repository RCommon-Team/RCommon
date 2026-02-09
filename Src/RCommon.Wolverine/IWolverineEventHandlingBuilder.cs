using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;

namespace RCommon.Wolverine
{
    /// <summary>
    /// Builder interface for configuring Wolverine-based event handling within the RCommon framework.
    /// Extends <see cref="IEventHandlingBuilder"/> to provide Wolverine-specific event subscription capabilities.
    /// </summary>
    public interface IWolverineEventHandlingBuilder : IEventHandlingBuilder
    {
    }
}
