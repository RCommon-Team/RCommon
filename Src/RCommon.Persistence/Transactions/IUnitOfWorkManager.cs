using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RCommon.Persistence.Transactions
{
    public interface IUnitOfWorkManager : IAsyncDisposable
    {
        [Obsolete("Please use UnitOfWorkManager.CurrentUnitOfWorkTransactionId. This will be removed in a future version.")]
        IUnitOfWork CurrentUnitOfWork { get; }

        Guid CurrentUnitOfWorkTransactionId { get; }

        bool IsUnitOfWorkActive { get; }

        Task CommitUnitOfWorkAsync(IUnitOfWork unitOfWork);
        Task CompleteUnitOfWorkAsync(IUnitOfWork unitOfWork);
        bool EnlistUnitOfWork(IUnitOfWork unitOfWork);
        Task RollbackUnitOfWorkAsync(IUnitOfWork unitOfWork);
    }
}
