using RCommon.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public interface IEntityEventTracker
    {
        /// <summary>
        /// The collection of entities that each may store a collection of events.
        /// </summary>
        ICollection<IBusinessEntity> TrackedEntities { get; }
        
        /// <summary>
        /// Adds an entity that can be tracked for any new events associated with it.
        /// </summary>
        /// <param name="entity"></param>
        void AddEntity(IBusinessEntity entity);

        /// <summary>
        /// Publishes the events associated with each entity being tracked.
        /// </summary>
        /// <returns>True if successful</returns>
        Task<bool> PublishLocalEvents();
    }
}
