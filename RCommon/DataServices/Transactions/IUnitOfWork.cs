

using System;
using System.Threading.Tasks;

namespace RCommon.DataServices.Transactions
{
    /// <summary>
    /// A unit of work contract that that encapsulates the Unit of Work pattern.
    /// </summary>
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Flushes the changes made in the unit of work to the data store.
        /// </summary>
        void Flush();

        Nullable<Guid> TransactionId { get; set; }


    }
}