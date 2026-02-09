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
    /// <summary>
    /// Decorator around <see cref="IGraphRepository{TEntity}"/> (as an <see cref="ILinqRepository{TEntity}"/>
    /// implementation) that adds cache-aware query overloads.
    /// Non-cached operations are delegated directly to the underlying repository.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by this repository.</typeparam>
    /// <remarks>
    /// Cached overloads use <see cref="ICacheService.GetOrCreateAsync{TData}"/> to retrieve results
    /// from cache or fall through to the inner repository and store the result.
    /// The <see cref="ICacheService"/> is resolved via <see cref="ICommonFactory{TEnum, TService}"/>
    /// using <see cref="PersistenceCachingStrategy.Default"/>.
    /// </remarks>
    public class CachingLinqRepository<TEntity> : ICachingLinqRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {
        private readonly IGraphRepository<TEntity> _repository;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingLinqRepository{TEntity}"/> class.
        /// </summary>
        /// <param name="repository">The inner graph repository to delegate operations to.</param>
        /// <param name="cacheFactory">Factory used to resolve the <see cref="ICacheService"/> for the default persistence caching strategy.</param>
        public CachingLinqRepository(IGraphRepository<TEntity> repository, ICommonFactory<PersistenceCachingStrategy, ICacheService> cacheFactory)
        {
            _repository = repository;
            _cacheService = cacheFactory.Create(PersistenceCachingStrategy.Default);
        }

        /// <inheritdoc />
        public Type ElementType => _repository.ElementType;

        /// <inheritdoc />
        public Expression Expression => _repository.Expression;

        /// <inheritdoc />
        public IQueryProvider Provider => _repository.Provider;

        /// <inheritdoc />
        public string DataStoreName { get => _repository.DataStoreName; set => _repository.DataStoreName = value; }

        /// <inheritdoc />
        public async Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            await _repository.AddAsync(entity, token);
        }

        /// <inheritdoc />
        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await _repository.AnyAsync(expression, token);
        }

        /// <inheritdoc />
        public async Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await _repository.AnyAsync(specification, token);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            await _repository.DeleteAsync(entity, token);
        }

        /// <inheritdoc />
        public async Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default)
        {
            return await _repository.FindAsync(primaryKey, token);
        }

        /// <inheritdoc />
        public IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification)
        {
            return _repository.FindQuery(specification);
        }

        /// <inheritdoc />
        public IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression)
        {
            return _repository.FindQuery(expression);
        }

        /// <inheritdoc />
        public IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0)
        {
            return _repository.FindQuery(expression, orderByExpression, orderByAscending, pageNumber, pageSize);
        }

        /// <inheritdoc />
        public IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression, bool orderByAscending)
        {
            return _repository.FindQuery(expression, orderByExpression, orderByAscending);
        }

        /// <inheritdoc />
        public IQueryable<TEntity> FindQuery(IPagedSpecification<TEntity> specification)
        {
            return _repository.FindQuery(specification);
        }

        /// <inheritdoc />
        public async Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await _repository.FindSingleOrDefaultAsync(expression, token);
        }

        /// <inheritdoc />
        public async Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await _repository.FindSingleOrDefaultAsync(specification, token);
        }

        /// <inheritdoc />
        public async Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default)
        {
            return await _repository.GetCountAsync(selectSpec, token);
        }

        /// <inheritdoc />
        public async Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await _repository.GetCountAsync(expression, token);
        }

        /// <inheritdoc />
        public IEnumerator<TEntity> GetEnumerator()
        {
            return _repository.GetEnumerator();
        }

        /// <inheritdoc />
        public IEagerLoadableQueryable<TEntity> Include(Expression<Func<TEntity, object>> path)
        {
            return _repository.Include(path);
        }

        /// <inheritdoc />
        public IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        {
            return _repository.ThenInclude<TPreviousProperty, TProperty>(path);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            await _repository.UpdateAsync(entity, token);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _repository.GetEnumerator();
        }

        /// <inheritdoc />
        public async Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity,
            object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0, CancellationToken token = default)
        {
            return await _repository.FindAsync(expression, orderByExpression, orderByAscending, pageNumber, pageSize, token);
        }

        /// <inheritdoc />
        public async Task<IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default)
        {
            return await _repository.FindAsync(specification, token);
        }

        /// <inheritdoc />
        public async Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await _repository.FindAsync(specification, token);
        }

        /// <inheritdoc />
        public async Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await _repository.FindAsync(expression, token);
        }

        /// <inheritdoc />
        public async Task<int> DeleteManyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await _repository.DeleteManyAsync(specification, token);
        }

        /// <inheritdoc />
        public async Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await _repository.DeleteManyAsync(expression, token);
        }

        // Cached items — these overloads check the cache first and fall through to the inner repository on a miss.

        /// <inheritdoc />
        public async Task<IPaginatedList<TEntity>> FindAsync(object cacheKey, Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity,
            object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0, CancellationToken token = default)
        {
            var data = await _cacheService.GetOrCreateAsync(cacheKey,
                async () => await _repository.FindAsync(expression, orderByExpression, orderByAscending, pageNumber, pageSize, token));
            return await data;
        }

        /// <inheritdoc />
        public async Task<IPaginatedList<TEntity>> FindAsync(object cacheKey, IPagedSpecification<TEntity> specification, CancellationToken token = default)
        {
            var data = await _cacheService.GetOrCreateAsync(cacheKey,
                async () => await _repository.FindAsync(specification, token));
            return await data;
        }

        /// <inheritdoc />
        public async Task<ICollection<TEntity>> FindAsync(object cacheKey, ISpecification<TEntity> specification, CancellationToken token = default)
        {
            var data = await _cacheService.GetOrCreateAsync(cacheKey,
                async () => await _repository.FindAsync(specification, token));
            return await data;
        }

        /// <inheritdoc />
        public async Task<ICollection<TEntity>> FindAsync(object cacheKey, Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            var data = await _cacheService.GetOrCreateAsync(cacheKey,
                async () => await _repository.FindAsync(expression, token));
            return await data;
        }

        /// <summary>
        /// Adds a range of entities by delegating to the underlying repository.
        /// </summary>
        /// <param name="entities">Collection of entities to persist.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            await _repository.AddRangeAsync(entities, token);
        }
    }
}
