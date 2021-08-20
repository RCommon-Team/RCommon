using RCommon.BusinessEntities;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess
{
    public interface ILinqMapperRepository<TEntity>: IReadOnlyRepository<TEntity>, IWriteOnlyRepository<TEntity>, INamedDataSource
        where TEntity : IBusinessEntity
    {
    }
}
