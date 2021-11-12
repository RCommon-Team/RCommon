using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.DataServices
{
    public interface IDataStore : IDisposable, IAsyncDisposable
    { 

        Task PersistChangesAsync(CancellationToken cancellationToken);

        IDbConnection GetDbConnection();
    }

}
