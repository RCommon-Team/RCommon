using MediatR;
using RCommon.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.BusinessEntities
{
    public class EventTracker : IEventTracker
    {
        private readonly ICollection<IBusinessEntity> _businessEntities = new List<IBusinessEntity>();

        public EventTracker()
        {
            
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
    }
}
