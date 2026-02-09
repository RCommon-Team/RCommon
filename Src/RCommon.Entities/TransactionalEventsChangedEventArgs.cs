using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    /// <summary>
    /// Provides data for events raised when a transactional (local) event is added to
    /// or removed from a <see cref="IBusinessEntity"/>.
    /// </summary>
    /// <seealso cref="TransactionalEventsClearedEventArgs"/>
    public class TransactionalEventsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TransactionalEventsChangedEventArgs"/>.
        /// </summary>
        /// <param name="entity">The entity on which the transactional event changed.</param>
        /// <param name="eventData">The serializable event that was added or removed.</param>
        public TransactionalEventsChangedEventArgs(IBusinessEntity entity, ISerializableEvent eventData)
        {
            Entity=entity;
            EventData=eventData;
        }

        /// <summary>
        /// Gets the entity on which the transactional event changed.
        /// </summary>
        public IBusinessEntity Entity { get; }

        /// <summary>
        /// Gets the serializable event that was added or removed.
        /// </summary>
        public ISerializableEvent EventData { get; }
    }
}
