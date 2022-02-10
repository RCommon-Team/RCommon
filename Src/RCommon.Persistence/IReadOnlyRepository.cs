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
    public interface IReadOnlyRepository<TEntity> : IQueryable<TEntity>, INamedDataSource
    {

        IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);
        IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);

        Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default);
        Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default);

        Task<int> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default);
        Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int? pageIndex, int pageSize = 0,
            CancellationToken token = default);
        Task<IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default);
    }
}
