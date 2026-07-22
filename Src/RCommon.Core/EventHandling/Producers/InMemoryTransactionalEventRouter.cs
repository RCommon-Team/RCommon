using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.EventHandling;
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
        private readonly int _maxGenerations;
        private readonly ConcurrentQueue<(ISerializableEvent Event, int Generation)> _queue;
        private volatile bool _draining;
        private int _currentGeneration;

        /// <summary>
        /// Initializes a new instance of <see cref="InMemoryTransactionalEventRouter"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve <see cref="IEventProducer"/> instances.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="subscriptionManager">The manager that tracks event-to-producer subscriptions for filtering.</param>
        /// <param name="eventHandlingOptions">Options controlling the cascade generation limit for the pre-commit drain.</param>
        public InMemoryTransactionalEventRouter(IServiceProvider serviceProvider, ILogger<InMemoryTransactionalEventRouter> logger,
            EventSubscriptionManager subscriptionManager, IOptions<EventHandlingOptions> eventHandlingOptions)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
            if (eventHandlingOptions == null) throw new ArgumentNullException(nameof(eventHandlingOptions));
            _maxGenerations = eventHandlingOptions.Value.MaxDispatchGenerations;
            _queue = new ConcurrentQueue<(ISerializableEvent Event, int Generation)>();
        }

        /// <inheritdoc />
        public async Task RouteEventsAsync(IEnumerable<ISerializableEvent> transactionalEvents, CancellationToken cancellationToken = default)
        {
            try
            {
                Guard.IsNotNull(transactionalEvents, nameof(transactionalEvents));

                if (transactionalEvents.Any())
                {
                    // Seperate Async events from Sync Events
                    var syncEvents = transactionalEvents.Where(x => x is ISyncEvent);
                    var asyncEvents = transactionalEvents.Where(x => x is IAsyncEvent);
                    var remainingEvents = transactionalEvents.Where(x => x is not IAsyncEvent && x is not ISyncEvent);
                    var eventProducers = _serviceProvider.GetServices<IEventProducer>().ToList();

                    if (!eventProducers.Any())
                    {
                        _logger.LogWarning(
                            "{Router} has {Count} transactional event(s) to route but no IEventProducer is " +
                            "registered -- these events will not be delivered to any subscriber.",
                            this.GetGenericTypeName(), transactionalEvents.Count());
                    }
                    else
                    {
                        _logger.LogInformation(
                            "{Router} is routing {Count} transactional events to event producers.",
                            this.GetGenericTypeName(), transactionalEvents.Count());
                    }

                    if (syncEvents.Any())
                    {
                        // Produce the Synchronized Events first
                        _logger.LogInformation($"{this.GetGenericTypeName()} is routing {syncEvents.Count().ToString()} synchronized transactional events.");
                        await this.ProduceSyncEvents(syncEvents, eventProducers, cancellationToken).ConfigureAwait(false);
                    }

                    if (asyncEvents.Any())
                    {
                        // Produce the Async Events
                        _logger.LogInformation($"{this.GetGenericTypeName()} is routing {asyncEvents.Count().ToString()} asynchronous transactional events.");
                        await this.ProduceAsyncEvents(asyncEvents, eventProducers, cancellationToken).ConfigureAwait(false);
                    }

                    if (remainingEvents.Any()) // Could be ISerializable events left over that are not marked as ISyncEvent or IAsyncEvent
                    {
                        // Send as synchronized by default
                        _logger.LogInformation($"No sync/async events found. {this.GetGenericTypeName()} is routing {remainingEvents.Count().ToString()} serializable events as synchronized transactional events by default.");
                        await this.ProduceSyncEvents(remainingEvents, eventProducers, cancellationToken).ConfigureAwait(false);
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
        private async Task ProduceAsyncEvents(IEnumerable<ISerializableEvent> asyncEvents, IEnumerable<IEventProducer> eventProducers, CancellationToken cancellationToken = default)
        {
            var eventTaskList = new List<Task>();
            foreach (var @event in asyncEvents)
            {
                var filteredProducers = _subscriptionManager.GetProducersForEvent(eventProducers, @event.GetType());
                foreach (var producer in filteredProducers)
                {
                    eventTaskList.Add(producer.ProduceEventAsync(@event, cancellationToken));
                }
            }
            await Task.WhenAll(eventTaskList).ConfigureAwait(false);
        }

        /// <summary>
        /// Dispatches sync events to their filtered producers sequentially, awaiting each before proceeding.
        /// </summary>
        /// <param name="syncEvents">The synchronous events to produce.</param>
        /// <param name="eventProducers">All registered event producers (will be filtered per event).</param>
        private async Task ProduceSyncEvents(IEnumerable<ISerializableEvent> syncEvents, IEnumerable<IEventProducer> eventProducers, CancellationToken cancellationToken = default)
        {
            foreach (var @event in syncEvents)
            {
                _logger.LogDebug($"{this.GetGenericTypeName()} is routing event: {@event}");
                var filteredProducers = _subscriptionManager.GetProducersForEvent(eventProducers, @event.GetType());
                foreach (var producer in filteredProducers)
                {
                    await producer.ProduceEventAsync(@event, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Drains all stored transactional events through their producers using a single FIFO queue
        /// that preserves raise order and tracks cascade "generations". Events raised by a handler while
        /// draining are enqueued at the next generation and processed in the same pass; an unbounded
        /// cascade trips the <see cref="EventHandlingOptions.MaxDispatchGenerations"/> cycle-breaker.
        /// </summary>
        /// <returns>Completed Task</returns>
        /// <remarks>
        /// The drain always dequeues the head before dispatching, guaranteeing forward progress. A
        /// contiguous run of same-generation async events is dispatched concurrently; sync (and untagged)
        /// events are dispatched one at a time in order. This preserves the interleaved raise-order that
        /// the batch <see cref="RouteEventsAsync(IEnumerable{ISerializableEvent}, CancellationToken)"/>
        /// overload does not.
        /// </remarks>
        public async Task RouteEventsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _draining = true;

                var eventProducers = _serviceProvider.GetServices<IEventProducer>().ToList();

                if (!eventProducers.Any() && _queue.TryPeek(out _))
                {
                    _logger.LogWarning(
                        "{Router} has transactional event(s) to route but no IEventProducer is " +
                        "registered -- these events will not be delivered to any subscriber.",
                        this.GetGenericTypeName());
                }

                while (_queue.TryPeek(out var head))
                {
                    _currentGeneration = head.Generation;

                    // Always dequeue the head first -- guarantees forward progress; the loop can never
                    // spin on a peeked-but-not-consumed head.
                    _queue.TryDequeue(out var headItem);

                    if (headItem.Event is IAsyncEvent)
                    {
                        // Extend the run with subsequent same-generation async events, dispatched concurrently.
                        var run = new List<ISerializableEvent> { headItem.Event };
                        while (_queue.TryPeek(out var next)
                               && next.Generation == headItem.Generation
                               && next.Event is IAsyncEvent)
                        {
                            _queue.TryDequeue(out var runItem);
                            run.Add(runItem.Event);
                        }

                        await this.ProduceAsyncEvents(run, eventProducers, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // ISyncEvent or untagged -> dispatch the single head synchronously, in order.
                        await this.ProduceSyncEvents(new[] { headItem.Event }, eventProducers, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (EventProductionException ex)
            {
                _logger.LogError(ex, "An error occured while producing events through the EventRouter: {0}.", this.GetGenericTypeName());
                throw;
            }
            catch (Exception ex) when (ex is not DispatchGenerationLimitException)
            {
                var message = "An error occured while producing events through the EventRouter: {0}";
                _logger.LogError(ex, message, this.GetGenericTypeName());
                throw new EventProductionException(message,
                    ex,
                    new object[] { this.GetGenericTypeName() });
            }
            finally
            {
                _draining = false;
            }
        }

        /// <inheritdoc />
        public void AddTransactionalEvent(ISerializableEvent serializableEvent)
        {
            Guard.IsNotNull(serializableEvent, nameof(serializableEvent));

            var generation = _draining ? _currentGeneration + 1 : 0;
            if (generation > _maxGenerations)
            {
                throw new DispatchGenerationLimitException(_maxGenerations);
            }

            _queue.Enqueue((serializableEvent, generation));
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
