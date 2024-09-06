using RCommon.Entities;
using RCommon.Persistence.Crud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching.Crud
{
    public interface ICachingSqlMapperRepository<TEntity> : ISqlMapperRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {
        Task<ICollection<TEntity>> FindAsync(object cacheKey, ISpecification<TEntity> specification, CancellationToken token = default);
        Task<ICollection<TEntity>> FindAsync(object cacheKey, Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
    }
}
