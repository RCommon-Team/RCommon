using RCommon.DataServices;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Domain.Repositories
{
    public abstract class SqlMapperRepositoryBase<TEntity> : DisposableResource, IAsyncCrudRepository<TEntity>, INamedDataSource
        where TEntity : class
    {

        

        public string DataStoreName { get; set; }
        public abstract IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);
        public abstract IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);
        public abstract Task AddAsync(TEntity entity);
        public abstract Task DeleteAsync(TEntity entity);
        public abstract Task UpdateAsync(TEntity entity);
        public abstract Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification);
        public abstract Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression);
        public abstract Task<TEntity> FindAsync(object primaryKey);
        public abstract Task<int> GetCountAsync(ISpecification<TEntity> selectSpec);
        public abstract Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression);
        public abstract Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression);
        public abstract Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification);
        public abstract Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression);
        public abstract Task<bool> AnyAsync(ISpecification<TEntity> specification);

    }

}
