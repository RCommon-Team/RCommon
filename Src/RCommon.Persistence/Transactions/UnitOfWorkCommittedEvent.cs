using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Transactions
{
    public record UnitOfWorkCommittedEvent : ISyncEvent
    {
        public UnitOfWorkCommittedEvent(Guid transactionId)
        {
            TransactionId = transactionId;
        }

        public Guid TransactionId { get; }
    }
}
