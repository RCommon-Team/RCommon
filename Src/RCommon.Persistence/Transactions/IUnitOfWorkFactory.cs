using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.Persistence.Transactions
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork Create();
        IUnitOfWork Create(TransactionMode transactionMode);
        IUnitOfWork Create(TransactionMode transactionMode, IsolationLevel isolationLevel);
    }
}
