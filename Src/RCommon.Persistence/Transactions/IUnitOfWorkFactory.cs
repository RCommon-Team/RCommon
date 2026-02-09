using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.Persistence.Transactions
{
    /// <summary>
    /// Factory for creating <see cref="IUnitOfWork"/> instances with configurable transaction settings.
    /// </summary>
    /// <seealso cref="UnitOfWorkFactory"/>
    public interface IUnitOfWorkFactory
    {
        /// <summary>
        /// Creates a new <see cref="IUnitOfWork"/> with default transaction settings.
        /// </summary>
        /// <returns>A new <see cref="IUnitOfWork"/> instance.</returns>
        IUnitOfWork Create();

        /// <summary>
        /// Creates a new <see cref="IUnitOfWork"/> with the specified transaction mode.
        /// </summary>
        /// <param name="transactionMode">The <see cref="TransactionMode"/> to use.</param>
        /// <returns>A new <see cref="IUnitOfWork"/> instance.</returns>
        IUnitOfWork Create(TransactionMode transactionMode);

        /// <summary>
        /// Creates a new <see cref="IUnitOfWork"/> with the specified transaction mode and isolation level.
        /// </summary>
        /// <param name="transactionMode">The <see cref="TransactionMode"/> to use.</param>
        /// <param name="isolationLevel">The <see cref="IsolationLevel"/> for the transaction.</param>
        /// <returns>A new <see cref="IUnitOfWork"/> instance.</returns>
        IUnitOfWork Create(TransactionMode transactionMode, IsolationLevel isolationLevel);
    }
}
