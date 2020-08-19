using RCommon.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Samples.ConsoleApp.Domain.Repositories
{
    public interface  IEncapsulatedRepository<TEntity>
    {
        TEntity Add(TEntity entity);
        Task AddAsync(TEntity entity);
        void Attach(TEntity entity);
        void Delete(TEntity entity);
        Task DeleteAsync(TEntity entity);
        IEagerFetchingRepository<TEntity> EagerlyWith(Action<EagerFetchingStrategy<TEntity>> strategyActions);
        IEagerFetchingRepository<TEntity> EagerlyWith(Expression<Func<TEntity, object>> path);
        ICollection<TEntity> Find(Expression<Func<TEntity, bool>> expression);
        ICollection<TEntity> Find(ISpecification<TEntity> specification);
        TEntity Find(object primaryKey);
        Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression);
        Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification);
        Task<TEntity> FindAsync(object primaryKey);
        TEntity FindSingleOrDefault(Expression<Func<TEntity, bool>> expression);
        TEntity FindSingleOrDefault(ISpecification<TEntity> specification);
        Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression);
        Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification);
        int GetCount(Expression<Func<TEntity, bool>> expression);
        int GetCount(ISpecification<TEntity> selectSpec);
        Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression);
        Task<int> GetCountAsync(ISpecification<TEntity> selectSpec);
        void Update(TEntity entity);
        Task UpdateAsync(TEntity entity);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression);
        Task<bool> AnyAsync(ISpecification<TEntity> selectSpec);
    }
}
