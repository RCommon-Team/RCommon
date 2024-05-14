using System;
using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.Persistence.Transactions
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        bool AutoComplete { get; }
        IsolationLevel IsolationLevel { get; set; }
        UnitOfWorkState State { get; }
        Guid TransactionId { get; }
        TransactionMode TransactionMode { get; set; }

        Task CommitAsync();
    }
}
