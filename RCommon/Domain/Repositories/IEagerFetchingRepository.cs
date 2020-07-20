using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RCommon.Domain.Repositories
{
    public interface IEagerFetchingRepository<TEntity, TDataStore> : IAsyncCrudRepository<TEntity, TDataStore>
    {

        IEagerFetchingRepository<TEntity, TDataStore> EagerlyWith(Action<EagerFetchingStrategy<TEntity>> strategyActions);

        IEagerFetchingRepository<TEntity, TDataStore> EagerlyWith(Expression<Func<TEntity, object>> path);

    }
}
