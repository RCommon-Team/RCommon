
using RCommon.BusinessEntities;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public interface IFullFeaturedRepository<TEntity> : ILinqRepository<TEntity>, IEagerFetchingRepository<TEntity>, IGraphRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {
    }
}
