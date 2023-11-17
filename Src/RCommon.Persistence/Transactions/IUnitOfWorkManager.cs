using System;
using System.Collections.Concurrent;

namespace RCommon.Persistence.Transactions
{
    public interface IUnitOfWorkManager
    {
        IUnitOfWork CurrentUnitOfWork { get; }
        ConcurrentDictionary<Guid, IUnitOfWork> EnlistedTransactions { get; }

        bool EnlistUnitOfWork(IUnitOfWork unitOfWorkScope);
    }
}