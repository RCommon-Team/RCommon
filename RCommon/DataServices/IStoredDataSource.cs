using System;

namespace RCommon.DataServices
{
    public interface IStoredDataSource
    {
        string DataStoreName { get; set; }
        Guid TransactionId { get; }
        Type Type { get; }
    }
}