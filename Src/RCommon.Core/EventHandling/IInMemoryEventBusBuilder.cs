using Microsoft.Extensions.DependencyInjection;

namespace RCommon.EventHandling
{
    /// <summary>
    /// Marker interface for the in-memory event bus builder. Extends <see cref="IEventHandlingBuilder"/>
    /// to provide a distinct builder type for configuring in-memory event handling with
    /// <see cref="InMemoryEventBus"/>.
    /// </summary>
    /// <seealso cref="InMemoryEventBusBuilder"/>
    public interface IInMemoryEventBusBuilder : IEventHandlingBuilder
    {

    }
}
