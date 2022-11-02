using System;
using System.Collections.Concurrent;

namespace RCommon.DataServices.Transactions
{
    public interface IUnitOfWorkManager
    {
        IUnitOfWork CurrentUnitOfWork { get; }
        ConcurrentDictionary<Guid, IUnitOfWork> EnlistedTransactions { get; }

        bool EnlistUnitOfWork(IUnitOfWork unitOfWorkScope);
    }
}