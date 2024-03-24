using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public class InMemoryEventTracker : IEventTracker
    {
        private readonly ICollection<IBusinessEntity> _businessEntities = new List<IBusinessEntity>();
        private readonly IEventRouter _eventRouter;

        public InMemoryEventTracker(IEventRouter eventRouter)
        {
            this._eventRouter = eventRouter;
        }

        public void AddEntity(IBusinessEntity entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, $"Entity of type {entity.GetType().AssemblyQualifiedName} cannot be null");

            if (entity.AllowEventTracking)
            {
                _businessEntities.Add(entity);
            }
            
        }

        public ICollection<IBusinessEntity> TrackedEntities { get => _businessEntities; }

        public bool PublishLocalEvents()
        {
            foreach (var entity in this._businessEntities)
            {
                var entityGraph = entity.TraverseGraphFor<IBusinessEntity>();
                entityGraph.ForEach(graphEntity =>
                    _eventRouter.RouteEvents(graphEntity.LocalEvents));
            }
            return true;
            
        }
    }
}
