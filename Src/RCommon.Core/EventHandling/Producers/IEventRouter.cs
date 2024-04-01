using System.Collections.Generic;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Producers
{
    public interface IEventRouter
    {
        /// <summary>
        /// Adds a serializable event to the transactional event store so that it can be published when the consumer is ready.
        /// </summary>
        /// <param name="serializableEvent"></param>
        void AddTransactionalEvent(ISerializableEvent serializableEvent);

        /// <summary>
        /// Adds a collection of serializable events to the transactional event store so that it can be published when the consumer is ready.
        /// </summary>
        /// <param name="serializableEvents"></param>
        void AddTransactionalEvents(IEnumerable<ISerializableEvent> serializableEvents);

        /// <summary>
        /// Wires up all of the event producers for all stored transactional events and then executes each <see cref="IEventProducer"/>
        /// for <see cref="ISyncEvent"/> events and <see cref="IAsyncEvent"/> events.
        /// </summary>
        /// <returns>Async Task Result</returns>
        Task RouteEventsAsync();

        /// <summary>
        /// Wires up all of the event producers for all events passed into parameters and then executes each <see cref="IEventProducer"/>
        /// for <see cref="ISyncEvent"/> events and <see cref="IAsyncEvent"/> events. 
        /// </summary>
        /// <param name="transactionalEvents">Events that needs to be published or sent through the <see cref="IEventProducer"/> 
        /// producers that are registered.</param>
        /// <returns>Async Task Result</returns>
        /// <remarks>This will not send stored transactional events, only the events sent through the parameter.</remarks>
        Task RouteEventsAsync(IEnumerable<ISerializableEvent> transactionalEvents);
    }
}