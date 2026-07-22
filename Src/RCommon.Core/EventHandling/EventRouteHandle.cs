using System;
using RCommon.EventHandling.Routing;

namespace RCommon.EventHandling
{
    /// <summary>
    /// Default implementation of <see cref="IEventRouteHandle"/> that records routing configuration
    /// for a specific event type into the <see cref="IEventRoutingRegistry"/>.
    /// </summary>
    public class EventRouteHandle : IEventRouteHandle
    {
        private readonly Type _eventType;
        private readonly IEventRoutingRegistry? _registry;

        /// <summary>
        /// Initializes a new <see cref="EventRouteHandle"/> for <paramref name="eventType"/>.
        /// </summary>
        /// <param name="eventType">The CLR type of the event being configured.</param>
        /// <param name="registry">
        /// The routing registry to record durability into. May be <c>null</c> if the core event
        /// handling infrastructure was not configured (unusual; <see cref="UseOutbox"/> becomes a no-op).
        /// </param>
        public EventRouteHandle(Type eventType, IEventRoutingRegistry? registry)
        {
            _eventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            _registry = registry;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Delegates null/blank-store guards to <see cref="IEventRoutingRegistry.MarkDurable"/>.
        /// </remarks>
        public IEventRouteHandle UseOutbox(string dataStoreName)
        {
            _registry?.MarkDurable(_eventType, dataStoreName);
            return this;
        }
    }
}
