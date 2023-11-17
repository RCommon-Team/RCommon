using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public class EntityUpdatedEvent<TEntity> : EntityChangedEvent<TEntity>
    {
        public EntityUpdatedEvent(TEntity entity) : base(entity)
        {
        }
    }
}
