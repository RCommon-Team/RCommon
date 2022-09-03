using RCommon.BusinessEntities;
using RCommon.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.NHibernate
{
    public interface INHRepository<TEntity> : IReadOnlyRepository<TEntity>, IWriteOnlyRepository<TEntity>,
        IGraphRepository<TEntity>, IEagerFetchingRepository<TEntity>, ILinqRepository<TEntity>
        where TEntity : IBusinessEntity
    {
        
    }
}
