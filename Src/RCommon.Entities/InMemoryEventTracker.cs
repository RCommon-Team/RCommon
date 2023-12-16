using RCommon.Entities;
using RCommon.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public class InMemoryEventTracker : ILocalEventTracker
    {
        private readonly ICollection<IBusinessEntity> _businessEntities = new List<IBusinessEntity>();
        private readonly IMediatorService _mediatorService;

        public InMemoryEventTracker(IMediatorService mediatorService)
        {
            _mediatorService = mediatorService;
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
                    graphEntity.LocalEvents.ForEach(localEvent =>
                        this._mediatorService.Publish(localEvent)));
            }
            return true;
            
        }
    }
}
