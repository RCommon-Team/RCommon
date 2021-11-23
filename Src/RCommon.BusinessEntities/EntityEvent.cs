using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.BusinessEntities
{
    public class EntityEvent<TEntity> : ILocalEvent
    {
        public EntityEvent(TEntity entity)
        {
            Entity = entity;
        }

        public TEntity Entity { get; }
    }
}
