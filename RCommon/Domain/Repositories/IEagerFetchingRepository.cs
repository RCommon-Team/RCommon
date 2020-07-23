using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RCommon.Domain.Repositories
{
    public interface IEagerFetchingRepository<TEntity> : IAsyncCrudRepository<TEntity>
    {

        IEagerFetchingRepository<TEntity> EagerlyWith(Action<EagerFetchingStrategy<TEntity>> strategyActions);

        IEagerFetchingRepository<TEntity> EagerlyWith(Expression<Func<TEntity, object>> path);

    }
}
