using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public interface IGraphRepository<TEntity> : INamedDataSource
    {
        Task AttachAsync(TEntity entity, CancellationToken token = default);

        Task DetachAsync(TEntity entity, CancellationToken token = default);

        public bool Tracking { get; set; }
    }
}
