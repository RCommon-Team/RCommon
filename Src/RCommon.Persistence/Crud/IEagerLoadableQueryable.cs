﻿using RCommon.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Crud
{
    public interface IEagerLoadableQueryable<TEntity> : IQueryable<TEntity>, IReadOnlyRepository<TEntity>
        where TEntity : IBusinessEntity
    {
        IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path);
    }
}
