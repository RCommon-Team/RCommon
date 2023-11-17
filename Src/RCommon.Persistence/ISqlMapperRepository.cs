using RCommon.BusinessEntities;
using RCommon.Persistence;
using RCommon.Persistence.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public interface ISqlMapperRepository<TEntity> : IReadOnlyRepository<TEntity>, IWriteOnlyRepository<TEntity>
        where TEntity : IBusinessEntity
    {
        public string TableName { get; set; }
        
    }
}
