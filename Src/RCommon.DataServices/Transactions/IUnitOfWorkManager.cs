using System;
using System.Collections.Concurrent;

namespace RCommon.DataServices.Transactions
{
    public interface IUnitOfWorkManager
    {
        IUnitOfWorkScope CurrentUnitOfWork { get; }
        ConcurrentDictionary<Guid, IUnitOfWorkScope> EnlistedTransactions { get; }

        bool EnlistUnitOfWork(IUnitOfWorkScope unitOfWorkScope);
    }
}