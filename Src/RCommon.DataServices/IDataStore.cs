using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices
{
    public interface IDataStore : IDisposable, IAsyncDisposable
    { 

        void PersistChanges();

        IDbConnection GetDbConnection();
    }

}
