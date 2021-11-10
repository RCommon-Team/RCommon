using System;

namespace RCommon.DataServices.Transactions
{
    public interface IUnitOfWorkScopeFactory
    {
        IUnitOfWorkScope Create();
        IUnitOfWorkScope Create(TransactionMode mode);
        IUnitOfWorkScope Create(TransactionMode mode, Action<IUnitOfWorkScope> customize);
    }
}