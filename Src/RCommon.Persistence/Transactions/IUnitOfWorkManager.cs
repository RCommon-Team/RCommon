using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RCommon.Persistence.Transactions
{
    public interface IUnitOfWorkManager : IAsyncDisposable
    {
        IUnitOfWork CurrentUnitOfWork { get; }

        Task CommitUnitOfWorkAsync(IUnitOfWork unitOfWork);
        Task CompleteUnitOfWorkAsync(IUnitOfWork unitOfWork);
        bool EnlistUnitOfWork(IUnitOfWork unitOfWork);
        Task RollbackUnitOfWorkAsync(IUnitOfWork unitOfWork);
    }
}
