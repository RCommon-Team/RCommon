using RCommon.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Repositories
{
    public interface IEagerLoadableQueryable<TEntity> : IQueryable<TEntity>, IReadOnlyRepository<TEntity>
        where TEntity : IBusinessEntity
    {
        IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path);
    }
}
