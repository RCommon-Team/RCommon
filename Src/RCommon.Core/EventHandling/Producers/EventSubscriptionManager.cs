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
        /// Records that an event type should be handled by all producers registered on a specific builder.
        /// Called during <c>AddSubscriber</c> configuration.
        /// </summary>
        public void AddSubscription(Type builderType, Type eventType)
        {
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
                return allProducers.Where(p => allowedProducerTypes.Contains(p.GetType()));
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
                return allowedProducerTypes.Contains(producerType);
            }

            // No subscriptions registered for this event type - allow all producers
            return true;
        }

        /// <summary>
        /// Returns true if any subscriptions have been configured at all.
        /// </summary>
        public bool HasSubscriptions => !_eventProducerMap.IsEmpty;
    }
}
