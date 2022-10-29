using System;
using System.Transactions;

namespace RCommon.DataServices.Transactions
{
    public interface IUnitOfWorkScopeFactory
    {
        IUnitOfWorkScope Create();
        IUnitOfWorkScope Create(TransactionMode mode);
        IUnitOfWorkScope Create(TransactionMode transactionMode, IsolationLevel isolationLevel);
    }
}
