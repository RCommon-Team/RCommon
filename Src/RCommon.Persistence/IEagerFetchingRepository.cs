using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RCommon.Persistence
{
    public interface IEagerFetchingRepository<TEntity>
    {

        IEagerFetchingRepository<TEntity> EagerlyWith(Action<EagerFetchingStrategy<TEntity>> strategyActions);

        IEagerFetchingRepository<TEntity> EagerlyWith(Expression<Func<TEntity, object>> path);

    }
}
