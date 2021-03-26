using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Domain.Repositories
{
    public interface IAsyncReadOnlyRepository<TEntity>
    {

        IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);
        IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);

        Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification);
        Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression);

        Task<TEntity> FindAsync(object primaryKey);

        Task<int> GetCountAsync(ISpecification<TEntity> selectSpec);
        Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression);

        Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression);

        Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression);

        Task<bool> AnyAsync(ISpecification<TEntity> specification);
    }
}
