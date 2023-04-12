using RCommon.BusinessEntities;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public interface IGraphRepository<TEntity> : ILinqRepository<TEntity>, IEagerLoadableQueryable<TEntity>
        where TEntity : class, IBusinessEntity
    {

        public bool Tracking { get; set; }
    }
}
