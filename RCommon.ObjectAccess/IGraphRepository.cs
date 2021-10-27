using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistance
{
    public interface IGraphRepository<TEntity>
    {
        Task AttachAsync(TEntity entity);

        Task DetachAsync(TEntity entity);

        public bool Tracking { get; set; }
    }
}
