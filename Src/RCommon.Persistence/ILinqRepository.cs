using RCommon.BusinessEntities;
using RCommon.Collections;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public interface ILinqRepository<TEntity>: IQueryable<TEntity>, IReadOnlyRepository<TEntity>, IWriteOnlyRepository<TEntity>, 
        IEagerLoadableQueryable<TEntity>
        where TEntity : IBusinessEntity
    {
        IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);
        IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);

        Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int? pageIndex, int pageSize = 0,
            CancellationToken token = default);
        Task<IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default);

        IEagerLoadableQueryable<TEntity> Include(Expression<Func<TEntity, object>> path);
    }
}
