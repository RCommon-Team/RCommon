using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.Persistence.Transactions
{
    public interface IUnitOfWorkFactory
    {
        Task<IUnitOfWork> CreateAsync();
        Task<IUnitOfWork> CreateAsync(TransactionMode transactionMode);
        Task<IUnitOfWork> CreateAsync(TransactionMode transactionMode, IsolationLevel isolationLevel);
    }
}