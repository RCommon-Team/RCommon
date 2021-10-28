using RCommon.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RCommon.Persistence.EFCore
{
    public interface IEFCoreRepository<TEntity> where TEntity : IBusinessEntity
    {
        bool Tracking { get; set; }

        
        Task AttachAsync(TEntity entity);
        Task DetachAsync(TEntity entity);
        IQueryable<TEntity> CreateQuery();
        Task DeleteAsync(TEntity entity);
        Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression);
        Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification);
        Task<TEntity> FindAsync(object primaryKey);
        IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);
        IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);
        Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression);
        Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification);
        Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression);
        Task<int> GetCountAsync(ISpecification<TEntity> selectSpec);
        Task UpdateAsync(TEntity entity);
    }
}