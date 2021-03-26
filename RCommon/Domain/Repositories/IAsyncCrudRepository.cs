using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Domain.Repositories
{
    public interface IAsyncCrudRepository<TEntity> : IAsyncReadOnlyRepository<TEntity>, IAsyncWriteOnlyRepository<TEntity>
    {

    }
}
