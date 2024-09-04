using RCommon.Collections;
using RCommon.Entities;
using RCommon.Persistence.Crud;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching.Crud
{
    public class CachingLinqRepository<TEntity> : ILinqRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {
        public Type ElementType => throw new NotImplementedException();

        public Expression Expression => throw new NotImplementedException();

        public IQueryProvider Provider => throw new NotImplementedException();

        public string DataStoreName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0)
        {
            throw new NotImplementedException();
        }

        public IQueryable<TEntity> FindQuery(IPagedSpecification<TEntity> specification)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public IEagerLoadableQueryable<TEntity> Include(Expression<Func<TEntity, object>> path)
        {
            throw new NotImplementedException();
        }

        public IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
