using RCommon.BusinessEntities;
using RCommon.DataServices;
using RCommon.DataServices.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistance
{
    public interface ISqlMapperRepository<TEntity> : IWriteOnlyRepository<TEntity> , INamedDataSource
        where TEntity : IBusinessEntity
    {
        public string TableName { get; set; }
        Task<ICollection<TEntity>> FindAsync(string sql, IList<Parameter> dbParams, CommandType commandType = CommandType.Text);

        Task<TEntity> FindSingleOrDefaultAsync(string sql, IList<Parameter> dbParams, CommandType commandType = CommandType.Text);
    }
}
