using MediatR;
using RCommon.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.BusinessEntities
{
    public class ChangeTracker : IChangeTracker
    {
        private readonly IMediator _mediator;
        private readonly ICollection<IBusinessEntity> _businessEntities = new List<IBusinessEntity>();

        public ChangeTracker(IMediator mediator)
        {
            _mediator = mediator;
        }

        public void AddEntity(IBusinessEntity entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, $"Entity of type {entity.GetType().AssemblyQualifiedName} cannot be null");

            entity.AllowChangeTracking = true;
            _businessEntities.Add(entity);
        }

        public ICollection<IBusinessEntity> TrackedEntities { get => _businessEntities; }
    }
}
