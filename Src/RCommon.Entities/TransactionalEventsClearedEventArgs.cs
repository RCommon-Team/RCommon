using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    /// <summary>
    /// Provides data for the event raised when all transactional (local) events
    /// are cleared from a <see cref="IBusinessEntity"/>.
    /// </summary>
    /// <seealso cref="TransactionalEventsChangedEventArgs"/>
    public class TransactionalEventsClearedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TransactionalEventsClearedEventArgs"/>.
        /// </summary>
        /// <param name="entity">The entity whose transactional events were cleared.</param>
        public TransactionalEventsClearedEventArgs(IBusinessEntity entity)
        {
            Entity = entity;
        }

        /// <summary>
        /// Gets the entity whose transactional events were cleared.
        /// </summary>
        public IBusinessEntity Entity { get; }
    }
}
