using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.Models.Events;

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
        private readonly EventSubscriptionManager _subscriptionManager;
        private ConcurrentQueue<ISerializableEvent> _storedTransactionalEvents;

        /// <summary>
        /// Initializes a new instance of <see cref="InMemoryTransactionalEventRouter"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve <see cref="IEventProducer"/> instances.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="subscriptionManager">The manager that tracks event-to-producer subscriptions for filtering.</param>
        public InMemoryTransactionalEventRouter(IServiceProvider serviceProvider, ILogger<InMemoryTransactionalEventRouter> logger,
            EventSubscriptionManager subscriptionManager)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            _storedTransactionalEvents = new ConcurrentQueue<ISerializableEvent>();
        }

        /// <inheritdoc />
        public async Task RouteEventsAsync(IEnumerable<ISerializableEvent> transactionalEvents)
        {
            try
            {
                Guard.IsNotNull(transactionalEvents, nameof(transactionalEvents));

                if (transactionalEvents.Any())
                {
                    _logger.LogInformation($"{this.GetGenericTypeName()} is routing {transactionalEvents.Count().ToString()} transactional events to event producers.");

                    // Seperate Async events from Sync Events
                    var syncEvents = transactionalEvents.Where(x => x is ISyncEvent);
                    var asyncEvents = transactionalEvents.Where(x => x is IAsyncEvent);
                    var remainingEvents = transactionalEvents.Where(x => x is not IAsyncEvent && x is not ISyncEvent);
                    var eventProducers = _serviceProvider.GetServices<IEventProducer>();

                    if (syncEvents.Any())
                    {
                        // Produce the Synchronized Events first
                        _logger.LogInformation($"{this.GetGenericTypeName()} is routing {syncEvents.Count().ToString()} synchronized transactional events.");
                        await this.ProduceSyncEvents(syncEvents, eventProducers).ConfigureAwait(false);
                    }

                    if (asyncEvents.Any())
                    {
                        // Produce the Async Events
                        _logger.LogInformation($"{this.GetGenericTypeName()} is routing {asyncEvents.Count().ToString()} asynchronous transactional events.");
                        await this.ProduceAsyncEvents(asyncEvents, eventProducers).ConfigureAwait(false);
                    }
                    
                    if (remainingEvents.Any()) // Could be ISerializable events left over that are not marked as ISyncEvent or IAsyncEvent
                    {
                        // Send as synchronized by default
                        _logger.LogInformation($"No sync/async events found. {this.GetGenericTypeName()} is routing {remainingEvents.Count().ToString()} serializable events as synchronized transactional events by default.");
                        await this.ProduceSyncEvents(remainingEvents, eventProducers).ConfigureAwait(false);
                    }

                }
            }
            catch(EventProductionException ex)
            {
                _logger.LogError(ex, "An error occured while producing events through the EventRouter: {0}.", this.GetGenericTypeName());
                throw;
            }
            catch (Exception ex)
            {
                var message = "An error occured while producing events through the EventRouter: {0}";
                _logger.LogError(ex, message, this.GetGenericTypeName());
                throw new EventProductionException(message,
                    ex,
                    new object[] { this.GetGenericTypeName() });
            }
        }

        /// <summary>
        /// Dispatches async events to their filtered producers concurrently using <see cref="Task.WhenAll"/>.
        /// </summary>
        /// <param name="asyncEvents">The async events to produce.</param>
        /// <param name="eventProducers">All registered event producers (will be filtered per event).</param>
        private async Task ProduceAsyncEvents(IEnumerable<ISerializableEvent> asyncEvents, IEnumerable<IEventProducer> eventProducers)
        {
            var eventTaskList = new List<Task>();
            foreach (var @event in asyncEvents)
            {
                var filteredProducers = _subscriptionManager.GetProducersForEvent(eventProducers, @event.GetType());
                foreach (var producer in filteredProducers)
                {
                    eventTaskList.Add(producer.ProduceEventAsync(@event));
                }
            }
            await Task.WhenAll(eventTaskList);
        }

        /// <summary>
        /// Dispatches sync events to their filtered producers sequentially, awaiting each before proceeding.
        /// </summary>
        /// <param name="syncEvents">The synchronous events to produce.</param>
        /// <param name="eventProducers">All registered event producers (will be filtered per event).</param>
        private async Task ProduceSyncEvents(IEnumerable<ISerializableEvent> syncEvents, IEnumerable<IEventProducer> eventProducers)
        {
            foreach (var @event in syncEvents)
            {
                _logger.LogDebug($"{this.GetGenericTypeName()} is routing event: {@event}");
                var filteredProducers = _subscriptionManager.GetProducersForEvent(eventProducers, @event.GetType());
                foreach (var producer in filteredProducers)
                {
                    await producer.ProduceEventAsync(@event).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Routes all transactional events. This will loop until we have removed all the events from the concurrent queue. 
        /// </summary>
        /// <returns>Completed Task</returns>
        /// <remarks>This should help us avoid race conditions e.g. a subscriber/event handler adds new events while we are processing the current list</remarks>
        public async Task RouteEventsAsync()
        {
            
            while (_storedTransactionalEvents.Any()) 
            {
                var currentEvents = new List<ISerializableEvent>();
                _storedTransactionalEvents.ForEach(x => currentEvents.Add(x));
                await this.RouteEventsAsync(currentEvents).ConfigureAwait(false);
                RemoveEvents(currentEvents);
            }
        }

        /// <summary>
        /// Removes a batch of events from the concurrent queue with retry logic.
        /// Each event is attempted up to 4 times before throwing an <see cref="EventProductionException"/>.
        /// </summary>
        /// <param name="events">The events to remove from the queue.</param>
        /// <exception cref="EventProductionException">Thrown if an event cannot be dequeued after 4 attempts.</exception>
        private void RemoveEvents(IEnumerable<ISerializableEvent> events)
        {
            foreach (var @event in events)
            {
                var item = @event;

                // Retry dequeue up to 4 times to handle ConcurrentQueue contention
                for (int i = 1; i <= 4; i++) // Try 4 times
                {
                    if (!RemoveEvent(item))
                    {
                        i++;
                    }
                    else
                    {
                        break;
                    }

                    if (i == 4)
                    {
                        throw new EventProductionException($"Could not Dequeue event {item}");
                    }
                }

            }
        }

        /// <summary>
        /// Attempts to dequeue a single event from the concurrent queue.
        /// </summary>
        /// <param name="event">The event to dequeue (used as output parameter for the dequeued item).</param>
        /// <returns><c>true</c> if the dequeue was successful; otherwise <c>false</c>.</returns>
        private bool RemoveEvent(ISerializableEvent @event)
        {
            bool success = _storedTransactionalEvents.TryDequeue(out ISerializableEvent? _);
            return success;
        }

        /// <inheritdoc />
        public void AddTransactionalEvent(ISerializableEvent serializableEvent)
        {
            Guard.IsNotNull(serializableEvent, nameof(serializableEvent));
            _storedTransactionalEvents.Enqueue(serializableEvent);
        }

        /// <inheritdoc />
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
