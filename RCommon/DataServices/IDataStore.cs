using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.DataServices
{
    public interface IDataStore : IDisposable
    {

        void CommitTransaction();
    }

    public interface IDataStore<TDataSourceType> : IDataStore, IDisposable
        where TDataSourceType : class
    {
        TDataSourceType DataContext { get; }
    }
}
