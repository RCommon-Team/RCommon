using RCommon.Collections;
using RCommon.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching.Crud
{
    public interface ICachingLinqRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {
        Task<IPaginatedList<TEntity>> FindAsync(object cacheKey, Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0, CancellationToken token = default);
        Task<IPaginatedList<TEntity>> FindAsync(object cacheKey, IPagedSpecification<TEntity> specification, CancellationToken token = default);
        Task<ICollection<TEntity>> FindAsync(object cacheKey, ISpecification<TEntity> specification, CancellationToken token = default);
        Task<ICollection<TEntity>> FindAsync(object cacheKey, Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
    }
}
