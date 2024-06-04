using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using Microsoft.Extensions.Options;
using RCommon.Core.Threading;
using RCommon.Entities;
using RCommon.Persistence;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Linq2Db
{
    public class RCommonDataConnection : DataConnection, IDataStore
    {

        public RCommonDataConnection(DataOptions linq2DbOptions)
            :base(linq2DbOptions)
        {
            
        }

     

        public DbConnection GetDbConnection()
        {
            return this.Connection;
        }
    }
}
