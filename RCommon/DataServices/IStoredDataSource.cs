using System;

namespace RCommon.DataServices
{
    public interface IStoredDataSource
    {
        IDataStore DataStore { get; }
        Guid TransactionId { get; }
    }
}