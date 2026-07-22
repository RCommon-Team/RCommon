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
    }
}
