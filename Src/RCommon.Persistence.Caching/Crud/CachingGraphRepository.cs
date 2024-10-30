using RCommon.Caching;
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
    public class CachingGraphRepository<TEntity> : ICachingGraphRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {
        private readonly IGraphRepository<TEntity> _repository;
        private readonly ICacheService _cacheService;

        public CachingGraphRepository(IGraphRepository<TEntity> repository, ICommonFactory<PersistenceCachingStrategy, ICacheService> cacheFactory)
        {
            _repository = repository;
            _cacheService = cacheFactory.Create(PersistenceCachingStrategy.Default);
        }

        public bool Tracking { get => _repository.Tracking; set => _repository.Tracking = value; }

        public Type ElementType => _repository.ElementType;

        public Expression Expression => _repository.Expression;

        public IQueryProvider Provider => _repository.Provider;

        public string DataStoreName { get => _repository.DataStoreName; set => _repository.DataStoreName = value; }

        public async Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            await _repository.AddAsync(entity, token);
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await _repository.AnyAsync(expression, token);
        }

        public async Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await _repository.AnyAsync(specification, token);
        }

        public async Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            await _repository.DeleteAsync(entity, token);
        }

        public async Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default)
        {
            return await _repository.FindAsync(primaryKey, token);
        }

        public IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification)
        {
            return _repository.FindQuery(specification);
        }

        public IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression)
        {
            return _repository.FindQuery(expression);
        }

        public IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0)
        {
            return _repository.FindQuery(expression, orderByExpression, orderByAscending, pageNumber, pageSize);
        }

        public IQueryable<TEntity> FindQuery(IPagedSpecification<TEntity> specification)
        {
            return _repository.FindQuery(specification);
        }

        public async Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await _repository.FindSingleOrDefaultAsync(expression, token);
        }

        public async Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await _repository.FindSingleOrDefaultAsync(specification, token);
        }

        public async Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default)
        {
            return await _repository.GetCountAsync(selectSpec, token);
        }

        public async Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await _repository.GetCountAsync(expression, token);
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return _repository.GetEnumerator();
        }

        public IEagerLoadableQueryable<TEntity> Include(Expression<Func<TEntity, object>> path)
        {
            return _repository.Include(path);
        }

        public IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        {
            return _repository.ThenInclude<TPreviousProperty, TProperty>(path);
        }

        public async Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            await _repository.UpdateAsync(entity, token);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _repository.GetEnumerator();
        }

        public async Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity,
            object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0, CancellationToken token = default)
        {
            return await _repository.FindAsync(expression, orderByExpression, orderByAscending, pageNumber, pageSize, token);
        }

        public async Task<IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default)
        {
            return await _repository.FindAsync(specification, token);
        }

        public async Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await _repository.FindAsync(specification, token);
        }

        public async Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await _repository.FindAsync(expression, token);
        }

        // Cached items

        public async Task<IPaginatedList<TEntity>> FindAsync(object cacheKey, Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity,
            object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0, CancellationToken token = default)
        {
            var data = await _cacheService.GetOrCreateAsync(cacheKey, 
                async () => await _repository.FindAsync(expression, orderByExpression, orderByAscending, pageNumber, pageSize, token));
            return await data;
        }

        public async Task<IPaginatedList<TEntity>> FindAsync(object cacheKey, IPagedSpecification<TEntity> specification, CancellationToken token = default)
        {
            var data = await _cacheService.GetOrCreateAsync(cacheKey,
                async () => await _repository.FindAsync(specification, token));
            return await data;
        }

        public async Task<ICollection<TEntity>> FindAsync(object cacheKey, ISpecification<TEntity> specification, CancellationToken token = default)
        {
            var data = await _cacheService.GetOrCreateAsync(cacheKey,
                 async () => await _repository.FindAsync(specification, token));
            return await data;
        }

        public async Task<ICollection<TEntity>> FindAsync(object cacheKey, Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            var data = await _cacheService.GetOrCreateAsync(cacheKey,
                async () => await _repository.FindAsync(expression, token));
            return await data;
        }
    }
}
