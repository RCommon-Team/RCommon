using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RCommon.Persistence
{
    public interface IEagerFetchingRepository<TEntity> : INamedDataSource
    {

        IEagerFetchingRepository<TEntity> Include(Action<EagerFetchingStrategy<TEntity>> strategyActions);

        IEagerFetchingRepository<TEntity> Include(Expression<Func<TEntity, object>> path);

    }
}
