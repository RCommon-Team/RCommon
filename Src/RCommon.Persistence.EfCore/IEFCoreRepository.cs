using RCommon.BusinessEntities;
using RCommon.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.EFCore
{
    public interface IEFCoreRepository<TEntity> : IReadOnlyRepository<TEntity>, IWriteOnlyRepository<TEntity>, 
        IGraphRepository<TEntity>, ILinqRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {

        
        
    }
}
