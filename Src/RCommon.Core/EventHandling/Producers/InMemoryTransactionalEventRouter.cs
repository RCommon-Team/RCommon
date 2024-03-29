using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RCommon.EventHandling.Producers
{
    /// <summary>
    /// Responsible for storing transactional events and wiring up all <see cref="ISerializableEvent">local events</see> to 
    /// their appropriate <see cref="IEventProducer"/>
    /// </summary>
    /// <remarks>This should be a scoped dependency.</remarks>
    public class InMemoryTransactionalEventRouter : IEventRouter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InMemoryTransactionalEventRouter> _logger;
        private List<ISerializableEvent> _storedTransactionalEvents;

        public InMemoryTransactionalEventRouter(IServiceProvider serviceProvider, ILogger<InMemoryTransactionalEventRouter> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _storedTransactionalEvents = new List<ISerializableEvent>();
        }

        public async Task RouteEventsAsync(IEnumerable<ISerializableEvent> transactionalEvents)
        {
            try
            {
                Guard.IsNotNull(transactionalEvents, nameof(transactionalEvents));

                // Seperate Async events from Sync Events
                var syncEvents = transactionalEvents.Where(x => x is ISyncEvent);
                var asyncEvents = transactionalEvents.Where(x => x is IAsyncEvent);
                var eventProducers = _serviceProvider.GetServices<IEventProducer>();

                // Produce the Synchronized Events first
                foreach (var localEvent in syncEvents)
                {
                    foreach (var producer in eventProducers)
                    {
                        await producer.ProduceEventAsync(localEvent);
                    }
                }

                // Produce the Async Events
                foreach (var localEvent in asyncEvents)
                {
                    foreach (var producer in eventProducers)
                    {
                        await producer.ProduceEventAsync(localEvent).ConfigureAwait(false);
                    }
                }

            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "An error occured while producing events through the EventRouter.");
                throw;
            }
        }

        public async Task RouteEventsAsync()
        {
            await this.RouteEventsAsync(this._storedTransactionalEvents);
            this._storedTransactionalEvents.Clear();
        }

        public void AddTransactionalEvent(ISerializableEvent serializableEvent)
        {
            Guard.IsNotNull(serializableEvent, nameof(serializableEvent));
            _storedTransactionalEvents.Add(serializableEvent);
        }

        public void AddTransactionalEvents(IEnumerable<ISerializableEvent> serializableEvents)
        {
            Guard.IsNotNull(serializableEvents, nameof(serializableEvents));

            foreach (var serializableEvent in serializableEvents)
            {
                this.AddTransactionalEvent(serializableEvent);
            }
        }
    }
}
