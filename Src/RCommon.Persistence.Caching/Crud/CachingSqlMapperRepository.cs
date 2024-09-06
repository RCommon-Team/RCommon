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
    public class CachingSqlMapperRepository<TEntity> : ISqlMapperRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {
        private readonly IGraphRepository<TEntity> _repository;
        private readonly ICacheService _cacheService;

        public CachingSqlMapperRepository(IGraphRepository<TEntity> repository, ICommonFactory<PersistenceCachingStrategy, ICacheService> cacheFactory)
        {
            _repository = repository;
            _cacheService = cacheFactory.Create(PersistenceCachingStrategy.Default);
        }

        public string TableName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string DataStoreName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
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

        public Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<TEntity>> FindAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> FindSingleOrDefaultAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
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

        public Task<long> GetCountAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
