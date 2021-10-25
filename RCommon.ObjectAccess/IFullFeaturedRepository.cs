
using RCommon.BusinessEntities;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess
{
    public interface IFullFeaturedRepository<TEntity> : ILinqMapperRepository<TEntity>, IEagerFetchingRepository<TEntity>, IGraphRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {
    }
}
