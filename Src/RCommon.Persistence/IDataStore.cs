using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public interface IDataStore : IDisposable, IAsyncDisposable
    { 

        void PersistChanges();
        DbConnection GetDbConnection();
    }

}
