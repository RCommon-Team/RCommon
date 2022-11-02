using System;
using System.Transactions;

namespace RCommon.DataServices.Transactions
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork Create();
        IUnitOfWork Create(TransactionMode mode);
        IUnitOfWork Create(TransactionMode transactionMode, IsolationLevel isolationLevel);
    }
}
