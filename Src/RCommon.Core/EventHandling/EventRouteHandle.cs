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
        private readonly BuilderOutboxState? _builderState;

        /// <summary>
        /// Initializes a new <see cref="EventRouteHandle"/> for <paramref name="eventType"/>.
        /// </summary>
        /// <param name="eventType">The CLR type of the event being configured.</param>
        /// <param name="registry">
        /// The routing registry to record durability into. May be <c>null</c> if the core event
        /// handling infrastructure was not configured (unusual; <see cref="UseOutbox"/> becomes a no-op).
        /// </param>
        public EventRouteHandle(Type eventType, IEventRoutingRegistry? registry)
            : this(eventType, registry, builderState: null) { }

        /// <summary>
        /// Initializes a new <see cref="EventRouteHandle"/> with per-builder-type state so that
        /// <see cref="UseOutbox"/> can record its explicitness and prevent a later
        /// <c>UseRCommonOutbox</c> from overwriting the choice.
        /// </summary>
        /// <param name="eventType">The CLR type of the event being configured.</param>
        /// <param name="registry">The routing registry. May be <c>null</c>.</param>
        /// <param name="builderState">
        /// The config-time state bag for the builder that called <c>Publish&lt;T&gt;</c>.
        /// May be <c>null</c> when the builder has no <see cref="EventRoutingRegistry"/> available.
        /// </param>
        internal EventRouteHandle(Type eventType, IEventRoutingRegistry? registry, BuilderOutboxState? builderState)
        {
            _eventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            _registry = registry;
            _builderState = builderState;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Delegates null/blank-store guards to <see cref="IEventRoutingRegistry.MarkDurable"/>.
        /// Also records this event as explicitly configured so a later <c>UseRCommonOutbox</c>
        /// builder-level default will not overwrite this store choice.
        /// </remarks>
        public IEventRouteHandle UseOutbox(string dataStoreName)
        {
            // Record explicitness first so that UseRCommonOutbox (if called after this)
            // knows not to overwrite this event's store.
            _builderState?.RecordExplicit(_eventType);
            _registry?.MarkDurable(_eventType, dataStoreName);
            return this;
        }
    }
}
