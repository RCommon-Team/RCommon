﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public class EntityCreatedEvent<TEntity> : EntityChangedEvent<TEntity>
    {
        public EntityCreatedEvent(TEntity entity) : base(entity)
        {
        }
    }
}
