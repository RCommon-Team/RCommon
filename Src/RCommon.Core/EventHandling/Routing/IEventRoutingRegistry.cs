using System;
using System.Collections.Generic;

namespace RCommon.EventHandling.Routing
{
    /// <summary>
    /// Tracks durability metadata for event types: whether a given event type is routed through
    /// RCommon's per-datastore outbox, and which datastore name it targets.
    /// </summary>
    /// <remarks>
    /// Registered as a singleton instance so that configuration-time registrations
    /// (e.g., <c>Publish&lt;T&gt;().UseOutbox("store")</c>) persist into runtime and can be
    /// read via <c>descriptor.ImplementationInstance</c> during later pipeline setup.
    /// </remarks>
    public interface IEventRoutingRegistry
    {
        /// <summary>
        /// Records that <paramref name="eventType"/> is durable and should be routed through
        /// the outbox associated with <paramref name="dataStoreName"/>. If the event type was
        /// previously registered, the registration is overwritten (last registration wins).
        /// </summary>
        /// <param name="eventType">The CLR type of the event. Must not be <c>null</c>.</param>
        /// <param name="dataStoreName">
        /// The name of the target outbox datastore. Must not be <c>null</c>, empty, or whitespace.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="eventType"/> or <paramref name="dataStoreName"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="dataStoreName"/> is empty or whitespace.
        /// </exception>
        void MarkDurable(Type eventType, string dataStoreName);

        /// <summary>
        /// Returns <c>true</c> if <paramref name="eventType"/> has been marked as durable.
        /// </summary>
        bool IsDurable(Type eventType);

        /// <summary>
        /// Attempts to retrieve the outbox datastore name registered for <paramref name="eventType"/>.
        /// </summary>
        /// <param name="eventType">The CLR type of the event to look up.</param>
        /// <param name="dataStoreName">
        /// When this method returns <c>true</c>, contains the registered store name;
        /// otherwise <c>null</c>.
        /// </param>
        /// <returns><c>true</c> if the event type is registered as durable; otherwise <c>false</c>.</returns>
        bool TryGetOutboxStore(Type eventType, out string? dataStoreName);

        /// <summary>
        /// Returns the distinct set of datastore names that have at least one durable event type registered
        /// (case-insensitive distinct).
        /// </summary>
        IReadOnlyCollection<string> DurableStoreNames { get; }
    }
}
