using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RCommon.EventHandling.Routing
{
    /// <summary>
    /// Default implementation of <see cref="IEventRoutingRegistry"/> backed by a
    /// <see cref="ConcurrentDictionary{TKey,TValue}"/> that maps event CLR types to their target
    /// outbox datastore names.
    /// </summary>
    /// <remarks>
    /// This should be registered as a singleton instance (not a type-based registration) so that
    /// configuration-time calls to <see cref="MarkDurable"/> persist into runtime and the instance
    /// can be retrieved via <c>descriptor.ImplementationInstance</c> during pipeline setup.
    /// </remarks>
    public class EventRoutingRegistry : IEventRoutingRegistry
    {
        // Maps event CLR type -> outbox datastore name
        private readonly ConcurrentDictionary<Type, string> _durableEvents = new();

        // Per-builder-type config-time state used to implement order-independent builder defaults.
        // Key: builder CLR type. Value: mutable state bag for that builder's Publish/UseRCommonOutbox calls.
        private readonly ConcurrentDictionary<Type, BuilderOutboxState> _builderState = new();

        /// <inheritdoc />
        public void MarkDurable(Type eventType, string dataStoreName)
        {
            if (eventType is null)
                throw new ArgumentNullException(nameof(eventType));
            if (dataStoreName is null)
                throw new ArgumentNullException(nameof(dataStoreName));
            if (string.IsNullOrWhiteSpace(dataStoreName))
                throw new ArgumentException("Data store name must not be empty or whitespace.", nameof(dataStoreName));

            // Overwrite any previous registration for this event type (last wins)
            _durableEvents[eventType] = dataStoreName;
        }

        /// <inheritdoc />
        public bool IsDurable(Type eventType) => _durableEvents.ContainsKey(eventType);

        /// <inheritdoc />
        public bool TryGetOutboxStore(Type eventType, out string? dataStoreName)
        {
            if (_durableEvents.TryGetValue(eventType, out var store))
            {
                dataStoreName = store;
                return true;
            }

            dataStoreName = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<string> DurableStoreNames =>
            _durableEvents.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // -------------------------------------------------------------------
        // Internal config-time helpers for builder-level default outbox support
        // -------------------------------------------------------------------

        /// <summary>
        /// Returns (creating if necessary) the per-builder-type config-time state bag.
        /// Internal: consumed by the builder-agnostic <see cref="EventRouteRegistrationExtensions"/>
        /// helpers (and via them by every event-handling builder's <c>Publish</c>/<c>UseRCommonOutbox</c>)
        /// and by <see cref="EventRouteHandle"/> during the DI configuration phase.
        /// </summary>
        internal BuilderOutboxState GetOrCreateBuilderState(Type builderType)
            => _builderState.GetOrAdd(builderType, _ => new BuilderOutboxState());
    }

    /// <summary>
    /// Mutable config-time state that tracks the builder-level outbox default and per-event
    /// explicit overrides for a single builder type.
    /// </summary>
    internal sealed class BuilderOutboxState
    {
        private readonly object _lock = new();

        // Builder-level default store name (null = not set).
        private string? _defaultStoreName;

        // All event types that were Publish<T>()'d on this builder type.
        private readonly HashSet<Type> _publishedEventTypes = new();

        // Event types that received an explicit per-event .UseOutbox() call.
        private readonly HashSet<Type> _explicitEventTypes = new();

        /// <summary>
        /// The builder-level default outbox store name, or <c>null</c> if not set.
        /// </summary>
        internal string? DefaultStoreName
        {
            get { lock (_lock) return _defaultStoreName; }
        }

        /// <summary>
        /// Records that an event type was published on this builder.
        /// Returns the current default store name (null if none set yet).
        /// </summary>
        internal string? RecordPublished(Type eventType)
        {
            lock (_lock)
            {
                _publishedEventTypes.Add(eventType);
                return _defaultStoreName;
            }
        }

        /// <summary>
        /// Records that an event type received an explicit per-event .UseOutbox() call.
        /// Explicit events are never overwritten by a builder-level default.
        /// </summary>
        internal void RecordExplicit(Type eventType)
        {
            lock (_lock) { _explicitEventTypes.Add(eventType); }
        }

        /// <summary>
        /// Returns true if the event type was marked with an explicit per-event .UseOutbox().
        /// </summary>
        internal bool IsExplicit(Type eventType)
        {
            lock (_lock) { return _explicitEventTypes.Contains(eventType); }
        }

        /// <summary>
        /// Sets the builder-level default store name and returns all already-published event types
        /// that are NOT in the explicit set (so they can be retroactively marked durable).
        /// </summary>
        internal IReadOnlyList<Type> SetDefault(string defaultStoreName)
        {
            lock (_lock)
            {
                _defaultStoreName = defaultStoreName;
                // Return published types not yet explicitly set
                return _publishedEventTypes
                    .Where(t => !_explicitEventTypes.Contains(t))
                    .ToList();
            }
        }
    }
}
