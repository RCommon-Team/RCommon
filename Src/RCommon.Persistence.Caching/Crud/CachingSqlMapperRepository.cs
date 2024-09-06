using RCommon.Caching;
using RCommon.Entities;
using RCommon.Persistence.Crud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching.Crud
{
    public class CachingSqlMapperRepository<TEntity> : ISqlMapperRepository<TEntity>, ICachingSqlMapperRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {
        private readonly ISqlMapperRepository<TEntity> _repository;
        private readonly ICacheService _cacheService;

        public CachingSqlMapperRepository(ISqlMapperRepository<TEntity> repository, ICommonFactory<PersistenceCachingStrategy, ICacheService> cacheFactory)
        {
            _repository = repository;
            _cacheService = cacheFactory.Create(PersistenceCachingStrategy.Default);
        }

        public string TableName { get => _repository.TableName; set => _repository.TableName = value; }
        public string DataStoreName { get => _repository.DataStoreName; set => _repository.DataStoreName = value; }

        public async Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            await _repository.AddAsync(entity, token);
        }

        public async Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
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

        public async Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await _repository.FindAsync(specification, token);
        }

        public async Task<ICollection<TEntity>> FindAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await _repository.FindAsync(expression, token);
        }

        public async Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default)
        {
            return await _repository.FindAsync(primaryKey, token);
        }

        public async Task<TEntity> FindSingleOrDefaultAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
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

        public async Task<long> GetCountAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await _repository.GetCountAsync(expression, token);
        }

        public async Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            await _repository.UpdateAsync(entity, token);
        }

        // Cached Items

        public async Task<ICollection<TEntity>> FindAsync(object cacheKey, ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await _cacheService.GetOrCreateAsync(cacheKey,
                await _repository.FindAsync(specification, token));
        }

        public async Task<ICollection<TEntity>> FindAsync(object cacheKey, System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await _cacheService.GetOrCreateAsync(cacheKey,
                await _repository.FindAsync(expression, token));
        }
    }
}
