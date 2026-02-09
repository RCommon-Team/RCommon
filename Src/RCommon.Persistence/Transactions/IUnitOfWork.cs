using System;
using System.Threading.Tasks;
using System.Transactions;

namespace RCommon.Persistence.Transactions
{
    /// <summary>
    /// Defines a unit of work that manages a transaction scope around one or more persistence operations.
    /// </summary>
    /// <remarks>
    /// Disposing a unit of work without calling <see cref="Commit"/> will result in a rollback.
    /// Use <see cref="IUnitOfWorkFactory"/> to create instances with specific transaction settings.
    /// </remarks>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the unit of work will automatically commit on disposal
        /// if no explicit commit or rollback was attempted.
        /// </summary>
        bool AutoComplete { get; }

        /// <summary>
        /// Gets or sets the <see cref="IsolationLevel"/> for the underlying transaction.
        /// </summary>
        IsolationLevel IsolationLevel { get; set; }

        /// <summary>
        /// Gets the current <see cref="UnitOfWorkState"/> of this unit of work.
        /// </summary>
        UnitOfWorkState State { get; }

        /// <summary>
        /// Gets the unique identifier for this unit of work transaction.
        /// </summary>
        Guid TransactionId { get; }

        /// <summary>
        /// Gets or sets the <see cref="TransactionMode"/> that determines how this unit of work
        /// participates in ambient transactions.
        /// </summary>
        TransactionMode TransactionMode { get; set; }

        /// <summary>
        /// Commits the unit of work, completing the underlying transaction scope.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the unit of work has already been disposed.</exception>
        /// <exception cref="UnitOfWorkException">Thrown if the unit of work has already been completed.</exception>
        void Commit();
    }
}
