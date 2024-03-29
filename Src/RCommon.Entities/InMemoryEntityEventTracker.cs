using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public class InMemoryEntityEventTracker : IEntityEventTracker
    {
        //TODO: Consider using ConcurrentQueue<IBusinessEntity> https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentqueue-1?view=net-8.0
        private readonly ICollection<IBusinessEntity> _businessEntities = new List<IBusinessEntity>();
        private readonly IEventRouter _eventRouter;

        public InMemoryEntityEventTracker(IEventRouter eventRouter)
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

        public void StoreTransactionalEvents()
        {
            foreach (var entity in this._businessEntities)
            {
                var entityGraph = entity.TraverseGraphFor<IBusinessEntity>();

                foreach (var graphEntity in entityGraph)
                {
                    _eventRouter.AddTransactionalEvents(graphEntity.LocalEvents);
                }
            }
            
        }
    }
}
