﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public class EntityChangedEvent<TEntity> : EntityEvent<TEntity>
    {
        public EntityChangedEvent(TEntity entity) : base(entity)
        {
        }
    }
}
