using System;

namespace RCommon.DataServices.Transactions
{
    public interface IUnitOfWorkManager
    {
        IUnitOfWorkTransactionManager CurrentTransactionManager { get; }
        IUnitOfWork CurrentUnitOfWork { get; }

        void SetTransactionManagerProvider(Func<IUnitOfWorkTransactionManager> transactionManager);
    }
}