using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RCommon.EventHandling.Producers
{
    /// <summary>
    /// Tracks which event types are subscribed to which producer types. This ensures that when the
    /// <see cref="IEventRouter"/> routes events, each <see cref="IEventProducer"/> only receives events
    /// that were explicitly subscribed through its associated event handling builder.
    /// </summary>
    /// <remarks>This should be registered as a singleton so that configuration-time registrations
    /// persist into runtime.</remarks>
    public class EventSubscriptionManager
    {
        // Maps builder concrete type -> set of producer types registered on that builder
        private readonly ConcurrentDictionary<Type, HashSet<Type>> _builderProducerMap = new();

        // Maps event type -> set of producer types that should handle that event
        private readonly ConcurrentDictionary<Type, HashSet<Type>> _eventProducerMap = new();

        // Tracks which builder types have had AddSubscription called on them at least once,
        // independent of whether any producer was registered for that builder at the time.
        private readonly ConcurrentDictionary<Type, byte> _buildersWithSubscriptions = new();

        /// <summary>
        /// Records that a producer type was registered through a specific builder type.
        /// Called during <c>AddProducer</c> configuration.
        /// </summary>
        public void AddProducerForBuilder(Type builderType, Type producerType)
        {
            var producers = _builderProducerMap.GetOrAdd(builderType, _ => new HashSet<Type>());
            lock (producers)
            {
                producers.Add(producerType);
            }
        }

        /// <summary>
        /// Returns true if the given producer type has already been registered through the given builder type.
        /// </summary>
        public bool HasProducerForBuilder(Type builderType, Type producerType)
        {
            if (_builderProducerMap.TryGetValue(builderType, out var producers))
            {
                lock (producers)
                {
                    return producers.Contains(producerType);
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if at least one producer has been registered through the given builder type,
        /// regardless of which producer type. Used by the startup diagnostics check to detect a builder
        /// with recorded subscriptions but zero registered producers -- a misconfiguration under which
        /// those subscribers are never invoked.
        /// </summary>
        public bool HasProducerForBuilder(Type builderType)
        {
            if (_builderProducerMap.TryGetValue(builderType, out var producers))
            {
                lock (producers)
                {
                    return producers.Count > 0;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the set of builder types that have at least one recorded event subscription
        /// (i.e. <see cref="AddSubscription"/> has been called for that builder type at least once).
        /// </summary>
        public IReadOnlyCollection<Type> GetBuilderTypesWithSubscriptions()
        {
            return _buildersWithSubscriptions.Keys.ToList();
        }

        /// <summary>
        /// Records that an event type should be handled by all producers registered on a specific builder.
        /// Called during <c>AddSubscriber</c> configuration.
        /// </summary>
        public void AddSubscription(Type builderType, Type eventType)
        {
            _buildersWithSubscriptions.TryAdd(builderType, 0);

            if (_builderProducerMap.TryGetValue(builderType, out var producerTypes))
            {
                var eventProducers = _eventProducerMap.GetOrAdd(eventType, _ => new HashSet<Type>());
                lock (producerTypes)
                {
                    lock (eventProducers)
                    {
                        foreach (var producerType in producerTypes)
                        {
                            eventProducers.Add(producerType);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Filters the given producers to only those that should handle the specified event type.
        /// If no subscriptions have been registered for the event type, returns all producers
        /// (backward-compatible fallback).
        /// </summary>
        public IEnumerable<IEventProducer> GetProducersForEvent(
            IEnumerable<IEventProducer> allProducers,
            Type eventType)
        {
            if (_eventProducerMap.TryGetValue(eventType, out var allowedProducerTypes))
            {
                lock (allowedProducerTypes)
                {
                    var snapshot = allowedProducerTypes.ToHashSet();
                    return allProducers.Where(p => snapshot.Contains(p.GetType()));
                }
            }

            // No subscriptions registered for this event type - fall back to all producers
            return allProducers;
        }

        /// <summary>
        /// Returns true if the given producer type should handle the given event type.
        /// If no subscriptions exist at all, or the event type has no explicit subscriptions,
        /// returns true (backward-compatible fallback).
        /// </summary>
        public bool ShouldProduceEvent(Type producerType, Type eventType)
        {
            if (_eventProducerMap.TryGetValue(eventType, out var allowedProducerTypes))
            {
                lock (allowedProducerTypes)
                {
                    return allowedProducerTypes.Contains(producerType);
                }
            }

            // No subscriptions registered for this event type - allow all producers
            return true;
        }

        /// <summary>
        /// Returns true if any subscriptions have been configured at all.
        /// </summary>
        public bool HasSubscriptions => !_eventProducerMap.IsEmpty;

        /// <summary>
        /// Clears all subscriptions. Primarily intended for testing scenarios.
        /// </summary>
        public void ClearSubscriptions()
        {
            _builderProducerMap.Clear();
            _eventProducerMap.Clear();
            _buildersWithSubscriptions.Clear();
        }
    }
}
