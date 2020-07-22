using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices
{
    public interface IDataStore : IDisposable, IAsyncDisposable
    {

        void PersistChanges();

        Task PersistChangesAsync();
    }

    public interface IDataStore<TDataSourceType> : IDataStore, IDisposable
        where TDataSourceType : class
    {
        TDataSourceType DataContext { get; }
    }
}
