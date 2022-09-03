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
    public interface IReadOnlyRepository<TEntity> : INamedDataSource
    {

        Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default);
        Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default);

        Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default);
        Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default);
    }
}
