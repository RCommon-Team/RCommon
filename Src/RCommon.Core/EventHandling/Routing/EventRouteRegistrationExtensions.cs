using Microsoft.Extensions.DependencyInjection;
using System;

namespace RCommon.EventHandling.Routing
{
    /// <summary>
    /// Public, builder-agnostic helpers for recording event durability routes into the
    /// <see cref="IEventRoutingRegistry"/> during DI configuration.
    /// </summary>
    /// <remarks>
    /// These helpers encapsulate the concrete <see cref="EventRoutingRegistry"/> cast and the
    /// internal per-builder-type config-time state (<c>GetOrCreateBuilderState</c>,
    /// <c>RecordPublished</c>, <c>SetDefault</c>) so callers in other assemblies (for example the
    /// mediator fluent verbs) can record durability routes without touching internals. They take
    /// an explicit <see cref="Type"/> builder type so they are not tied to any specific builder.
    /// </remarks>
    public static class EventRouteRegistrationExtensions
    {
        /// <summary>
        /// Records that <paramref name="eventType"/> is published on the builder identified by
        /// <paramref name="builderType"/>, applies any already-set builder-level default outbox
        /// store (order-independent), and returns an <see cref="IEventRouteHandle"/> so the caller
        /// can optionally chain <see cref="IEventRouteHandle.UseOutbox"/>.
        /// </summary>
        /// <param name="services">The service collection being configured.</param>
        /// <param name="builderType">The CLR type of the builder recording this route.</param>
        /// <param name="eventType">The CLR type of the event being published.</param>
        /// <returns>
        /// An <see cref="IEventRouteHandle"/> for the event. If the routing registry is absent
        /// (core event handling not configured), the returned handle is null-safe and its
        /// <see cref="IEventRouteHandle.UseOutbox"/> is a no-op.
        /// </returns>
        public static IEventRouteHandle RecordPublishRoute(this IServiceCollection services, Type builderType, Type eventType)
        {
            // Resolve the routing registry so the caller can optionally mark durability
            var registry = services.GetRoutingRegistry();

            // Retrieve (or create) per-builder-type config-time state for builder-default support
            var concreteRegistry = registry as EventRoutingRegistry;
            var builderState = concreteRegistry?.GetOrCreateBuilderState(builderType);

            // Record this event as published on this builder; get the current default (if already set)
            var currentDefault = builderState?.RecordPublished(eventType);

            // If a builder-level default is already set and this event has not been explicitly
            // configured, apply it now (order-independent: default before recording the route)
            if (currentDefault is not null)
            {
                registry?.MarkDurable(eventType, currentDefault);
            }

            return new EventRouteHandle(eventType, registry, builderState);
        }

        /// <summary>
        /// Sets a builder-level default outbox store for all events recorded on the builder
        /// identified by <paramref name="builderType"/> that do not have an explicit per-event
        /// override, retroactively marking already-recorded non-explicit events durable.
        /// </summary>
        /// <param name="services">The service collection being configured.</param>
        /// <param name="builderType">The CLR type of the builder applying the default.</param>
        /// <param name="dataStoreName">
        /// The default outbox datastore name. Must not be <c>null</c>, empty, or whitespace.
        /// </param>
        public static void ApplyBuilderOutboxDefault(this IServiceCollection services, Type builderType, string dataStoreName)
        {
            if (dataStoreName is null)
                throw new ArgumentNullException(nameof(dataStoreName));
            if (string.IsNullOrWhiteSpace(dataStoreName))
                throw new ArgumentException("Data store name must not be empty or whitespace.", nameof(dataStoreName));

            var registry = services.GetRoutingRegistry();
            var concreteRegistry = registry as EventRoutingRegistry;
            var builderState = concreteRegistry?.GetOrCreateBuilderState(builderType);

            if (builderState is not null)
            {
                // Set the default and get back the already-recorded events that are not explicit
                var retroactiveTypes = builderState.SetDefault(dataStoreName);
                foreach (var eventType in retroactiveTypes)
                {
                    registry!.MarkDurable(eventType, dataStoreName);
                }
            }
        }
    }
}
