using RCommon.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.NHibernate
{
    public interface INHRepository<TEntity> where TEntity : class
    {
        Task AddAsync(TEntity entity, CancellationToken token = default);
        Task AttachAsync(TEntity entity, CancellationToken token = default);
        Task DeleteAsync(TEntity entity, CancellationToken token = default);
        Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default);
        Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default);
        IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);
        IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);
        Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default);
        Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        Task<int> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default);
        Task UpdateAsync(TEntity entity, CancellationToken token = default);
        Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int? pageIndex, int pageSize = 0,
            CancellationToken token = default);
        Task<IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default);
    }
}
