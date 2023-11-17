using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public class EntityDeletedEvent<TEntity> : EntityChangedEvent<TEntity>
    {
        public EntityDeletedEvent(TEntity entity) : base(entity)
        {
        }
    }
}
