using System.Threading.Tasks;

namespace RCommon.EventHandling.Subscribers
{
    /// <summary>
    /// Defines a handler for distributed events that accepts dynamically-typed event data,
    /// allowing handling of events without compile-time knowledge of their concrete type.
    /// </summary>
    public interface IDynamicDistributedEventHandler
    {
        /// <summary>
        /// Handles a distributed event with dynamically-typed event data.
        /// </summary>
        /// <param name="eventData">The event data as a dynamic object.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous handling operation.</returns>
        Task Handle(dynamic eventData);
    }
}
