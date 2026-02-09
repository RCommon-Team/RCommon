using System.Threading.Tasks;

namespace RCommon.EventHandling
{
    /// <summary>
    /// Defines an in-process event bus for publishing events and subscribing event handlers.
    /// </summary>
    /// <seealso cref="InMemoryEventBus"/>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes an event to all registered subscribers of <typeparamref name="TEvent"/>.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to publish.</typeparam>
        /// <param name="event">The event instance to publish.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task PublishAsync<TEvent>(TEvent @event);

        /// <summary>
        /// Subscribes a specific event handler to a specific event type.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to subscribe to.</typeparam>
        /// <typeparam name="TEventHandler">The handler type that implements <see cref="Subscribers.ISubscriber{TEvent}"/>.</typeparam>
        /// <returns>The <see cref="IEventBus"/> instance for method chaining.</returns>
        IEventBus Subscribe<TEvent, TEventHandler>()
            where TEvent : class
            where TEventHandler : class, Subscribers.ISubscriber<TEvent>;

        /// <summary>
        /// Automatically subscribes <typeparamref name="TEventHandler"/> to all event types it handles
        /// by discovering all <see cref="Subscribers.ISubscriber{TEvent}"/> interface implementations.
        /// </summary>
        /// <typeparam name="TEventHandler">The handler type whose <see cref="Subscribers.ISubscriber{TEvent}"/> implementations are auto-discovered.</typeparam>
        /// <returns>The <see cref="IEventBus"/> instance for method chaining.</returns>
        IEventBus SubscribeAllHandledEvents<TEventHandler>() where TEventHandler : class;
    }
}