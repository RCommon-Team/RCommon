using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess.EFCore
{
    public interface IEFCoreRepository<TEntity> where TEntity : class
    {
        bool Tracking { get; set; }

        TEntity Add(TEntity entity);
        Task AddAsync(TEntity entity);
        void Attach(TEntity entity);
        IQueryable<TEntity> CreateQuery();
        void Delete(TEntity entity);
        Task DeleteAsync(TEntity entity);
        ICollection<TEntity> Find(Expression<Func<TEntity, bool>> expression);
        ICollection<TEntity> Find(ISpecification<TEntity> specification);
        TEntity Find(object primaryKey);
        Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression);
        Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification);
        Task<TEntity> FindAsync(object primaryKey);
        IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);
        IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);
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
    }
}