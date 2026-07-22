namespace RCommon.EventHandling
{
    /// <summary>
    /// A fluent handle returned by <c>Publish&lt;TEvent&gt;()</c> that allows post-registration
    /// routing configuration such as marking the event as durable via an outbox.
    /// </summary>
    public interface IEventRouteHandle
    {
        /// <summary>
        /// Marks the event type as durable and routes it through the outbox associated with
        /// <paramref name="dataStoreName"/>.
        /// </summary>
        /// <param name="dataStoreName">
        /// The name of the target outbox datastore. Must not be <c>null</c>, empty, or whitespace.
        /// </param>
        /// <returns>This handle instance to allow further chaining.</returns>
        IEventRouteHandle UseOutbox(string dataStoreName);
    }
}
